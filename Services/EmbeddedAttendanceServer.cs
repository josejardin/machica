using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;
using GymRegistrationApp.Models;

namespace GymRegistrationApp.Services;

public static class EmbeddedAttendanceServer
{
    private static TcpListener? _listener;
    private static CancellationTokenSource? _cts;
    private static bool _isRunning;
    public static int Port { get; private set; } = 5080;
    public static bool IsRunning => _isRunning;

    public static void Start(int port = 5080)
    {
        if (_isRunning) return;
        Port = port;
        try
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            Task.Run(() => ListenLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Embedded Attendance Server failed to start on port {port}: {ex.Message}");
            _isRunning = false;
        }
    }

    public static void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
    }

    public static string GetPreferredLocalUrl()
    {
        var ip = GetLocalIpAddress();
        return $"http://{ip}:{Port}/attendance";
    }

    public static string GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch { }

        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up ||
                    ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                        return ip.Address.ToString();
                }
            }
        }
        catch { }

        return "127.0.0.1";
    }

    private static async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClient(client), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (!_isRunning) break;
                Console.Error.WriteLine($"Server accept error: {ex.Message}");
            }
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream();
                stream.ReadTimeout = 6000;
                stream.WriteTimeout = 6000;

                using var ms = new MemoryStream();
                var tempBuf = new byte[4096];
                int headerEnd = -1;
                int contentLength = 0;

                while (true)
                {
                    var read = await stream.ReadAsync(tempBuf, 0, tempBuf.Length);
                    if (read <= 0) break;
                    ms.Write(tempBuf, 0, read);

                    var bytes = ms.ToArray();
                    var text = Encoding.UTF8.GetString(bytes);
                    headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd >= 0)
                    {
                        var headerPart = text.Substring(0, headerEnd);
                        foreach (var line in headerPart.Split("\r\n"))
                        {
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            {
                                int.TryParse(line.Substring(15).Trim(), out contentLength);
                            }
                        }

                        var bodyBytesReceived = bytes.Length - (headerEnd + 4);
                        if (bodyBytesReceived >= contentLength)
                        {
                            break;
                        }
                    }
                }

                var fullBytes = ms.ToArray();
                if (fullBytes.Length == 0 || headerEnd < 0) return;

                var fullText = Encoding.UTF8.GetString(fullBytes);
                var headerText = fullText.Substring(0, headerEnd);
                var body = fullText.Substring(headerEnd + 4).Trim();

                var lines = headerText.Split("\r\n");
                if (lines.Length == 0) return;

                var requestLine = lines[0].Split(' ');
                if (requestLine.Length < 2) return;

                var method = requestLine[0].ToUpperInvariant();
                var rawPath = requestLine[1];
                var path = rawPath.Split('?')[0];

                if (method == "GET" && (path == "/" || path == "/attendance" || path == "/index.html"))
                {
                    var html = RenderAttendancePage();
                    await SendResponse(stream, 200, "OK", "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
                    return;
                }

                if (method == "POST" && (path == "/api/attendance" || path == "/attendance"))
                {
                    string memberId = "", firstName = "", lastName = "", middleName = "", qrDate = "", qrToken = "";

                    if (body.StartsWith("{"))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(body);
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (prop.Name.Equals("memberId", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("member_id", StringComparison.OrdinalIgnoreCase))
                                    memberId = prop.Value.GetString() ?? "";
                                else if (prop.Name.Equals("firstName", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("first_name", StringComparison.OrdinalIgnoreCase))
                                    firstName = prop.Value.GetString() ?? "";
                                else if (prop.Name.Equals("middleName", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("middle_name", StringComparison.OrdinalIgnoreCase))
                                    middleName = prop.Value.GetString() ?? "";
                                else if (prop.Name.Equals("lastName", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("last_name", StringComparison.OrdinalIgnoreCase))
                                    lastName = prop.Value.GetString() ?? "";
                                else if (prop.Name.Equals("qrDate", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("date", StringComparison.OrdinalIgnoreCase))
                                    qrDate = prop.Value.GetString() ?? "";
                                else if (prop.Name.Equals("qrToken", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("code", StringComparison.OrdinalIgnoreCase))
                                    qrToken = prop.Value.GetString() ?? "";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"JSON parse error in attendance POST: {ex.Message}");
                        }
                    }
                    else
                    {
                        var form = HttpUtility.ParseQueryString(body);
                        memberId = form["memberId"] ?? form["member_id"] ?? "";
                        firstName = form["firstName"] ?? form["first_name"] ?? "";
                        middleName = form["middleName"] ?? form["middle_name"] ?? "";
                        lastName = form["lastName"] ?? form["last_name"] ?? "";
                        qrDate = form["qrDate"] ?? form["date"] ?? "";
                        qrToken = form["qrToken"] ?? form["token"] ?? form["code"] ?? "";
                    }

                    // Anti-fraud daily token verification
                    if (!string.IsNullOrEmpty(qrDate) || !string.IsNullOrEmpty(qrToken))
                    {
                        if (!DailyTokenService.ValidateDailyToken(qrDate, qrToken, out var fraudError))
                        {
                            var rejectJson = JsonSerializer.Serialize(new
                            {
                                success = false,
                                message = fraudError
                            });
                            await SendResponse(stream, 200, "OK", "application/json; charset=utf-8", Encoding.UTF8.GetBytes(rejectJson));
                            return;
                        }
                    }

                    var result = AttendanceService.Record(
                        new AttendanceSubmission(firstName, middleName, lastName, memberId),
                        "QR-MobileCheckIn");

                    if (result.Success)
                    {
                        DataStore.Reload();
                    }

                    var responseJson = JsonSerializer.Serialize(new
                    {
                        success = result.Success,
                        message = result.Message,
                        memberId = result.MemberId,
                        fullName = result.FullName,
                        checkInTime = result.CheckInTime?.ToString("h:mm tt") ?? ""
                    });

                    await SendResponse(stream, 200, "OK", "application/json; charset=utf-8", Encoding.UTF8.GetBytes(responseJson));
                    return;
                }

                // 404
                await SendResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("Not Found"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Client handler error: {ex.Message}");
            }
        }
    }

    private static async Task SendResponse(NetworkStream stream, int statusCode, string statusText, string contentType, byte[] body)
    {
        var headers = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                      $"Content-Type: {contentType}\r\n" +
                      $"Content-Length: {body.Length}\r\n" +
                      "Access-Control-Allow-Origin: *\r\n" +
                      "Connection: close\r\n\r\n";

        var headerBytes = Encoding.UTF8.GetBytes(headers);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
        await stream.WriteAsync(body, 0, body.Length);
        await stream.FlushAsync();
    }

    private static string RenderAttendancePage()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>UniversalGym - Member Check-In</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }
        body { background: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; min-height: 100dvh; padding: 16px; }
        .card { background: #1e293b; width: 100%; max-width: 440px; border-radius: 20px; box-shadow: 0 20px 40px rgba(0,0,0,0.4); overflow: hidden; border: 1px solid #334155; }
        .header { background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); color: #ffffff; padding: 28px 20px; text-align: center; border-bottom: 4px solid #ff8423; }
        .header h1 { font-size: 26px; font-weight: 900; letter-spacing: 1px; color: #ffffff; }
        .header p { color: #94a3b8; font-size: 13px; margin-top: 4px; }
        .badge { display: inline-block; background: rgba(255, 132, 35, 0.15); color: #ff8423; border: 1px solid rgba(255, 132, 35, 0.3); font-size: 12px; font-weight: 700; padding: 5px 12px; border-radius: 12px; margin-top: 10px; }
        .body { padding: 24px 20px; }
        .form-group { margin-bottom: 16px; }
        label { display: block; font-size: 12px; font-weight: 700; color: #94a3b8; margin-bottom: 6px; letter-spacing: 0.5px; }
        input[type=""text""] { width: 100%; padding: 13px 14px; background: #0f172a; border: 1.5px solid #334155; border-radius: 10px; font-size: 16px; color: #ffffff; outline: none; transition: all 0.2s; }
        input[type=""text""]:focus { border-color: #ff8423; box-shadow: 0 0 0 3px rgba(255, 132, 35, 0.2); }
        .btn-submit { width: 100%; background: #ff8423; color: #ffffff; border: none; padding: 15px; font-size: 16px; font-weight: 800; border-radius: 10px; cursor: pointer; transition: all 0.2s; margin-top: 8px; box-shadow: 0 4px 14px rgba(255, 132, 35, 0.3); }
        .btn-submit:hover { background: #e07018; transform: translateY(-1px); }
        .btn-submit:active { transform: scale(0.99); }
        .btn-submit:disabled { background: #475569; cursor: not-allowed; box-shadow: none; transform: none; }
        .result-box { display: none; padding: 16px; border-radius: 10px; margin-top: 18px; font-size: 14px; line-height: 1.6; }
        .result-box.success { display: block; background: rgba(16, 185, 129, 0.15); border: 1.5px solid #10b981; color: #6ee7b7; }
        .result-box.error { display: block; background: rgba(239, 68, 68, 0.15); border: 1.5px solid #ef4444; color: #fca5a5; }
        .footer { text-align: center; font-size: 11px; color: #64748b; margin-top: 24px; }
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""header"">
            <h1>UNIVERSAL GYM</h1>
            <p>24/7 Member Attendance Self Check-In</p>
            <div id=""dateBadge"" class=""badge"">Loading Daily Pass...</div>
        </div>
        <div class=""body"">
            <form id=""attendanceForm"">
                <div class=""form-group"">
                    <label for=""memberId"">MEMBER ID</label>
                    <input type=""text"" id=""memberId"" placeholder=""e.g. GM100001"" required autocomplete=""off"" style=""text-transform: uppercase;"">
                </div>
                <div class=""form-group"">
                    <label for=""firstName"">FIRST NAME</label>
                    <input type=""text"" id=""firstName"" placeholder=""e.g. Juan"" required>
                </div>
                <div class=""form-group"">
                    <label for=""lastName"">LAST NAME</label>
                    <input type=""text"" id=""lastName"" placeholder=""e.g. Dela Cruz"" required>
                </div>
                <button type=""submit"" class=""btn-submit"" id=""btnSubmit"">Log Attendance</button>
            </form>
            <div id=""resultBox"" class=""result-box""></div>
            <div class=""footer"">UniversalGym Automated Attendance &bull; Official Portal</div>
        </div>
    </div>

    <script>
        const urlParams = new URLSearchParams(window.location.search);
        const qrDate = urlParams.get('date') || '';
        const qrToken = urlParams.get('token') || '';

        const badge = document.getElementById('dateBadge');
        if (qrDate) {
            badge.innerText = '📅 Daily Pass: ' + qrDate;
        } else {
            badge.innerText = '🔒 Counter Daily Pass Active';
        }

        const form = document.getElementById('attendanceForm');
        const resultBox = document.getElementById('resultBox');
        const btn = document.getElementById('btnSubmit');

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            btn.disabled = true;
            btn.innerText = 'Checking In...';
            resultBox.className = 'result-box';
            resultBox.style.display = 'none';

            const payload = {
                memberId: document.getElementById('memberId').value.trim(),
                firstName: document.getElementById('firstName').value.trim(),
                lastName: document.getElementById('lastName').value.trim(),
                qrDate: qrDate,
                qrToken: qrToken
            };

            try {
                const res = await fetch('/api/attendance', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const data = await res.json();

                if (data.success) {
                    resultBox.className = 'result-box success';
                    resultBox.innerHTML = '<strong>✅ Check-in Recorded!</strong><br>' +
                                          data.fullName + ' (' + data.memberId + ')<br>' +
                                          'Time: ' + (data.checkInTime || 'Just now') + '<br>' +
                                          '<small style=""color:#a7f3d0;"">Your attendance is securely logged in UniversalGym cloud.</small>';
                    form.reset();
                } else {
                    resultBox.className = 'result-box error';
                    resultBox.innerHTML = '<strong>❌ Check-in Failed</strong><br>' + (data.message || 'Verification error.');
                }
            } catch (err) {
                resultBox.className = 'result-box error';
                resultBox.innerHTML = '<strong>Network Error</strong><br>Could not contact attendance server. Please try again.';
            } finally {
                btn.disabled = false;
                btn.innerText = 'Log Attendance';
            }
        });
    </script>
</body>
</html>";
    }
}

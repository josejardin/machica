using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace GymRegistrationApp.Services;

/// <summary>
/// Manages a secure, zero-configuration Cloudflare Tunnel so gym attendance
/// can be accessed publicly by any smartphone on 4G/5G mobile data or outside Wi-Fi.
/// </summary>
public static class CloudflareTunnelService
{
    private static Process? _tunnelProcess;
    private static readonly object _syncLock = new();
    private static string? _publicUrl;
    private static string _statusMessage = "Offline";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static string? PublicUrl
    {
        get => _publicUrl;
        private set
        {
            _publicUrl = value;
            if (!string.IsNullOrEmpty(value))
            {
                UrlReady?.Invoke(value);
            }
        }
    }

    public static string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            StatusChanged?.Invoke(value);
        }
    }

    public static bool IsRunning => _tunnelProcess != null && !_tunnelProcess.HasExited && !string.IsNullOrEmpty(_publicUrl);

    public static event Action<string>? UrlReady;
    public static event Action<string>? StatusChanged;

    public static string GetCloudflaredPath()
    {
        // 1. App directory
        var localAppDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloudflared.exe");
        if (File.Exists(localAppDir)) return localAppDir;

        // 2. Working directory
        var cwd = Path.Combine(Directory.GetCurrentDirectory(), "cloudflared.exe");
        if (File.Exists(cwd)) return cwd;

        // 3. LocalAppData
        var localAppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GymRegistrationApp",
            "cloudflared.exe");
        if (File.Exists(localAppData)) return localAppData;

        // 4. In PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, "cloudflared.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }

        return localAppData;
    }

    public static async Task<string?> StartTunnelAsync(int localPort = 5080, int timeoutSeconds = 25)
    {
        lock (_syncLock)
        {
            if (IsRunning && !string.IsNullOrEmpty(_publicUrl))
            {
                return _publicUrl;
            }
            StopTunnel();
        }

        var binaryPath = GetCloudflaredPath();
        if (!File.Exists(binaryPath))
        {
            StatusMessage = "Downloading public tunnel service...";
            var downloaded = await TryDownloadCloudflaredAsync(binaryPath);
            if (!downloaded || !File.Exists(binaryPath))
            {
                StatusMessage = "Cloudflare tunnel utility is not installed. Using local Wi-Fi link.";
                return null;
            }
        }

        StatusMessage = "Connecting to Cloudflare secure tunnel network...";

        var tcs = new TaskCompletionSource<string?>();
        var urlRegex = new Regex(@"https://[a-zA-Z0-9-]+\.trycloudflare\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = $"tunnel --url http://127.0.0.1:{localPort} --no-autoupdate",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            DataReceivedEventHandler outputHandler = (_, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;
                var match = urlRegex.Match(e.Data);
                if (match.Success)
                {
                    var baseDomain = match.Value.TrimEnd('/');
                    var fullAttendanceUrl = $"{baseDomain}/attendance";
                    lock (_syncLock)
                    {
                        PublicUrl = fullAttendanceUrl;
                        StatusMessage = "🟢 Public Online Access Active (Cloudflare HTTPS)";
                    }
                    tcs.TrySetResult(fullAttendanceUrl);
                }
            };

            process.OutputDataReceived += outputHandler;
            process.ErrorDataReceived += outputHandler;
            process.Exited += (_, _) =>
            {
                lock (_syncLock)
                {
                    if (string.IsNullOrEmpty(_publicUrl))
                    {
                        StatusMessage = "Tunnel stopped unexpectedly.";
                        tcs.TrySetResult(null);
                    }
                }
            };

            if (!process.Start())
            {
                StatusMessage = "Could not start tunnel process.";
                return null;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_syncLock)
            {
                _tunnelProcess = process;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            cts.Token.Register(() => tcs.TrySetResult(null));

            var result = await tcs.Task;
            if (string.IsNullOrEmpty(result))
            {
                StatusMessage = "Could not obtain public tunnel URL (timed out or no internet).";
            }
            return result;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tunnel error: {ex.Message}";
            return null;
        }
    }

    public static void StopTunnel()
    {
        lock (_syncLock)
        {
            if (_tunnelProcess != null)
            {
                try
                {
                    if (!_tunnelProcess.HasExited)
                    {
                        _tunnelProcess.Kill(entireProcessTree: true);
                        _tunnelProcess.WaitForExit(2000);
                    }
                }
                catch { }
                finally
                {
                    _tunnelProcess?.Dispose();
                    _tunnelProcess = null;
                }
            }
            _publicUrl = null;
            StatusMessage = "Public tunnel stopped.";
        }
    }

    private static async Task<bool> TryDownloadCloudflaredAsync(string destination)
    {
        try
        {
            var dir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            const string downloadUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";
            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return false;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            await contentStream.CopyToAsync(fileStream);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

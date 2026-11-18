using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GymRegistrationApp.Services;

public sealed record GeoLocationResult(double Latitude, double Longitude, string Source, bool Success);

/// <summary>
/// Automatically detects the physical GPS location of the gym laptop/computer
/// using local Wi-Fi beacon triangulation and fast fallback IP geolocation.
/// Dynamically calibrates every time QR Attendance is launched.
/// </summary>
public static class GeoLocationService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(6) };

    public static async Task<GeoLocationResult> DetectCurrentLocationAsync()
    {
        // 1. Try Wi-Fi Triangulation (BeaconDB) using nearby Wi-Fi BSSIDs
        try
        {
            var wifiAps = GetNearbyWifiAccessPoints();
            if (wifiAps.Count > 0)
            {
                var payload = JsonSerializer.Serialize(new { wifiAccessPoints = wifiAps });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var res = await HttpClient.PostAsync("https://beacondb.net/v1/geolocate", content);
                if (res.IsSuccessStatusCode)
                {
                    var jsonStr = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonStr);
                    if (doc.RootElement.TryGetProperty("location", out var loc) &&
                        loc.TryGetProperty("lat", out var latEl) &&
                        loc.TryGetProperty("lng", out var lngEl))
                    {
                        var lat = latEl.GetDouble();
                        var lng = lngEl.GetDouble();
                        return new GeoLocationResult(lat, lng, "Wi-Fi Triangulation", true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Wi-Fi geolocation error: {ex.Message}");
        }

        // 2. Fallback: Fast IP Geolocation (ip-api.com)
        try
        {
            var res = await HttpClient.GetStringAsync("http://ip-api.com/json/");
            using var doc = JsonDocument.Parse(res);
            if (doc.RootElement.TryGetProperty("status", out var status) && status.GetString() == "success" &&
                doc.RootElement.TryGetProperty("lat", out var latEl) &&
                doc.RootElement.TryGetProperty("lon", out var lonEl))
            {
                var lat = latEl.GetDouble();
                var lng = lonEl.GetDouble();
                var city = doc.RootElement.TryGetProperty("city", out var cityEl) ? cityEl.GetString() : "Auto";
                return new GeoLocationResult(lat, lng, $"Network IP ({city})", true);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IP geolocation error: {ex.Message}");
        }

        // 3. Fallback: ipwho.is
        try
        {
            var res = await HttpClient.GetStringAsync("https://ipwho.is/");
            using var doc = JsonDocument.Parse(res);
            if (doc.RootElement.TryGetProperty("success", out var success) && success.GetBoolean() &&
                doc.RootElement.TryGetProperty("latitude", out var latEl) &&
                doc.RootElement.TryGetProperty("longitude", out var lonEl))
            {
                return new GeoLocationResult(latEl.GetDouble(), lonEl.GetDouble(), "Network Geolocation", true);
            }
        }
        catch { }

        // Default fallback if offline
        return new GeoLocationResult(14.7226, 121.0460, "Default Cached Location", false);
    }

    private static List<object> GetNearbyWifiAccessPoints()
    {
        var list = new List<object>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show networks mode=bssid",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return list;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);

            var matches = Regex.Matches(output, @"BSSID\s+\d+\s+:\s+([0-9a-fA-F:]{17})[\s\S]*?Signal\s+:\s+(\d+)%");
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var mac = match.Groups[1].Value.ToLowerInvariant();
                    if (int.TryParse(match.Groups[2].Value, out var signalPercent))
                    {
                        var dBm = Math.Clamp((signalPercent / 2) - 100, -100, -30);
                        list.Add(new { macAddress = mac, signalStrength = dBm });
                        if (list.Count >= 10) break;
                    }
                }
            }
        }
        catch { }

        return list;
    }
}

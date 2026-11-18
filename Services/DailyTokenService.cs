using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GymRegistrationApp.Services;

/// <summary>
/// Provides cryptographically secure daily tokens for gym attendance QR codes.
/// Ensures that QR codes change daily and cannot be reused or faked by taking photos on previous days,
/// and binds the token to the gym's physical GPS location to prevent remote check-ins from home.
/// </summary>
public static class DailyTokenService
{
    private const string SecretSalt = "UniversalGym_Attendance_AntiFraud_Secret_2026";

    /// <summary>
    /// Generates a unique 16-character hex token for a specific date and optional gym coordinates.
    /// </summary>
    public static string GenerateDailyToken(DateTime date, double gymLat = 0, double gymLng = 0)
    {
        var latStr = Math.Abs(gymLat) > 0.0001 ? gymLat.ToString("F4", CultureInfo.InvariantCulture) : "";
        var lngStr = Math.Abs(gymLng) > 0.0001 ? gymLng.ToString("F4", CultureInfo.InvariantCulture) : "";
        var raw = $"{SecretSalt}_{date:yyyy-MM-dd}_{latStr}_{lngStr}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).Substring(0, 16);
    }

    /// <summary>
    /// Validates whether a submitted date and token match today's valid daily token (Manila time).
    /// </summary>
    public static bool ValidateDailyToken(string? dateString, string? token, double gymLat, double gymLng, out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(dateString) || string.IsNullOrWhiteSpace(token))
        {
            errorMessage = "Invalid QR code: Missing daily security token. Please scan the current printed poster at the gym counter.";
            return false;
        }

        if (!DateTime.TryParse(dateString, out var parsedDate))
        {
            errorMessage = "Invalid QR code: Malformed date in QR link.";
            return false;
        }

        var todayManila = AttendanceService.ManilaNow().Date;

        if (parsedDate.Date < todayManila)
        {
            errorMessage = $"❌ Expired QR Code: This QR code was only valid on {parsedDate:MMM dd, yyyy}. Photos taken on previous days are rejected to prevent fraud. Please scan today's active poster at the gym counter.";
            return false;
        }

        if (parsedDate.Date > todayManila)
        {
            errorMessage = "❌ Invalid QR Code: The date in this QR code is from the future.";
            return false;
        }

        var expectedToken = GenerateDailyToken(todayManila, gymLat, gymLng);
        if (!string.Equals(token.Trim(), expectedToken, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "❌ Tampered QR Code: Security verification failed. Please scan the official poster at the gym counter.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Backwards compatible overload without coordinates.
    /// </summary>
    public static bool ValidateDailyToken(string? dateString, string? token, out string errorMessage)
    {
        return ValidateDailyToken(dateString, token, 0, 0, out errorMessage);
    }

    /// <summary>
    /// Calculates the distance in meters between two GPS coordinates using the Haversine formula.
    /// </summary>
    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

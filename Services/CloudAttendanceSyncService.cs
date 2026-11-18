using System.Net.Http;
using System.Text;
using System.Text.Json;
using GymRegistrationApp.Models;

namespace GymRegistrationApp.Services;

/// <summary>
/// Synchronizes attendance records that gym members submitted to the 24/7 Cloud Attendance Web App
/// while the gym laptop was shut down or offline.
/// </summary>
public static class CloudAttendanceSyncService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(12) };

    public static async Task<SyncResult> SyncFromCloudAsync(string cloudBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(cloudBaseUrl))
        {
            return new SyncResult(false, "No cloud attendance URL configured.", 0);
        }

        try
        {
            var cleanBase = cloudBaseUrl.Split('?')[0].TrimEnd('/');
            // If URL ends with /attendance, remove it to get base
            if (cleanBase.EndsWith("/attendance", StringComparison.OrdinalIgnoreCase))
            {
                cleanBase = cleanBase.Substring(0, cleanBase.Length - 11);
            }

            var syncEndpoint = $"{cleanBase}/api/sync";
            var ackEndpoint = $"{cleanBase}/api/sync/ack";

            using var response = await _http.GetAsync(syncEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                return new SyncResult(false, $"Cloud server returned status: {response.StatusCode}", 0);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("records", out var recordsElement) || recordsElement.ValueKind != JsonValueKind.Array)
            {
                return new SyncResult(true, "No pending cloud attendance records.", 0);
            }

            var syncedIds = new List<string>();
            var newRecordsCount = 0;
            var today = AttendanceService.ManilaNow().Date;

            foreach (var item in recordsElement.EnumerateArray())
            {
                var cloudId = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                var memberId = item.TryGetProperty("memberId", out var midProp) ? midProp.GetString() ?? "" : "";
                var fullName = item.TryGetProperty("fullName", out var fnProp) ? fnProp.GetString() ?? "" : "";
                var firstName = item.TryGetProperty("firstName", out var fstProp) ? fstProp.GetString() ?? "" : "";
                var lastName = item.TryGetProperty("lastName", out var lstProp) ? lstProp.GetString() ?? "" : "";
                var dateStr = item.TryGetProperty("attendanceDate", out var dtProp) ? dtProp.GetString() ?? "" : "";
                var timeStr = item.TryGetProperty("checkInTime", out var tmProp) ? tmProp.GetString() ?? "" : "";

                if (string.IsNullOrEmpty(memberId)) continue;
                if (!DateTime.TryParse(dateStr, out var recordDate)) recordDate = today;

                // Check if already in local attendance
                var alreadyExists = DataStore.AttendanceRecords.Any(a =>
                    !a.IsDeleted &&
                    a.MemberId.Equals(memberId, StringComparison.OrdinalIgnoreCase) &&
                    a.AttendanceDate.Date == recordDate.Date);

                if (!alreadyExists)
                {
                    // Find member in DataStore to ensure name accuracy
                    var dbMember = DataStore.Members.FirstOrDefault(m => m.Id.Equals(memberId, StringComparison.OrdinalIgnoreCase));
                    if (dbMember != null)
                    {
                        fullName = dbMember.FullName;
                        firstName = dbMember.FirstName;
                        lastName = dbMember.LastName;
                    }

                    var newAttId = DataStore.NewAttendanceId();
                    var newAtt = new AttendanceRecord
                    {
                        Id = newAttId,
                        MemberId = memberId,
                        MemberName = fullName,
                        AttendanceDate = recordDate.Date,
                        CheckInTime = DateTime.Now,
                        RecordedBy = "Cloud-Sync"
                    };

                    DataStore.AttendanceRecords.Add(newAtt);

                    var newVisit = new DailyGymVisit
                    {
                        Id = DataStore.NewDailyGymVisitId(),
                        MemberId = memberId,
                        FirstName = firstName,
                        LastName = lastName,
                        IsGymMember = true,
                        VisitDate = recordDate.Date,
                        Fee = 0,
                        PaymentMethod = "Included",
                        RegisteredBy = "Cloud-Sync"
                    };
                    DataStore.DailyGymVisits.Add(newVisit);

                    newRecordsCount++;
                }

                if (!string.IsNullOrEmpty(cloudId))
                {
                    syncedIds.Add(cloudId);
                }
            }

            if (newRecordsCount > 0)
            {
                DataStore.SaveAttendanceRecords();
                DataStore.SaveDailyGymVisits();
            }

            // Acknowledge synced records to Cloud
            if (syncedIds.Count > 0)
            {
                try
                {
                    var ackPayload = JsonSerializer.Serialize(new { syncedIds });
                    using var ackContent = new StringContent(ackPayload, Encoding.UTF8, "application/json");
                    await _http.PostAsync(ackEndpoint, ackContent);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ACK send error: {ex.Message}");
                }
            }

            var msg = newRecordsCount > 0
                ? $"Successfully synced {newRecordsCount} attendance record(s) from Cloud!"
                : "Cloud attendance is up to date (no new records).";

            return new SyncResult(true, msg, newRecordsCount);
        }
        catch (Exception ex)
        {
            return new SyncResult(false, $"Sync error: {ex.Message}", 0);
        }
    }

    public sealed record SyncResult(bool Success, string Message, int SyncedCount);
}

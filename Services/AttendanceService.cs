using MySql.Data.MySqlClient;
using GymRegistrationApp.Models;

namespace GymRegistrationApp.Services;

public sealed record AttendanceSubmission(string FirstName, string MiddleName, string LastName, string MemberId);

public sealed record AttendanceResult(
    bool Success,
    string Message,
    string MemberId = "",
    string FullName = "",
    DateTime? AttendanceDate = null,
    DateTime? CheckInTime = null);

public static class AttendanceService
{
    private static readonly TimeZoneInfo ManilaTimeZone = ResolveManilaTimeZone();

    public static AttendanceResult Record(AttendanceSubmission submission, string recordedBy)
    {
        var memberId = submission.MemberId.Trim().ToUpperInvariant();
        var first = submission.FirstName.Trim();
        var middle = submission.MiddleName.Trim();
        var last = submission.LastName.Trim();

        if (memberId.Length == 0 && (first.Length == 0 || last.Length == 0))
        {
            return new(false, "Please provide your Member ID or Full Name.");
        }

        var now = ManilaNow();
        var date = now.Date;

        // Try MySQL first if connected
        if (DatabaseConnection.TestConnection())
        {
            try
            {
                using var connection = DatabaseConnection.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    string dbId, dbFirst, dbMiddle, dbLast, fullName, status;
                    DateTime expiry;

                    if (memberId.Length > 0)
                    {
                        using var cmd = new MySqlCommand(@"
SELECT id, first_name, middle_name, last_name, full_name, expiry_date, status
FROM members
WHERE id=@id AND is_deleted=0
FOR UPDATE", connection, transaction);
                        cmd.Parameters.AddWithValue("@id", memberId);
                        using var reader = cmd.ExecuteReader();
                        if (!reader.Read()) return new(false, $"Member ID '{memberId}' was not found.");

                        dbId = Convert.ToString(reader["id"]) ?? memberId;
                        dbFirst = Convert.ToString(reader["first_name"]) ?? "";
                        dbMiddle = Convert.ToString(reader["middle_name"]) ?? "";
                        dbLast = Convert.ToString(reader["last_name"]) ?? "";
                        fullName = Convert.ToString(reader["full_name"]) ?? $"{dbFirst} {dbLast}".Trim();
                        expiry = Convert.ToDateTime(reader["expiry_date"]).Date;
                        status = Convert.ToString(reader["status"]) ?? "";
                        reader.Close();

                        // If user also provided names, verify they loosely match
                        if (first.Length > 0 && !string.Equals(first, dbFirst, StringComparison.OrdinalIgnoreCase))
                        {
                            return new(false, "The first name does not match the registered Member ID.");
                        }
                    }
                    else
                    {
                        using var cmd = new MySqlCommand(@"
SELECT id, first_name, middle_name, last_name, full_name, expiry_date, status
FROM members
WHERE first_name=@first AND last_name=@last AND is_deleted=0
ORDER BY id DESC LIMIT 1", connection, transaction);
                        cmd.Parameters.AddWithValue("@first", first);
                        cmd.Parameters.AddWithValue("@last", last);
                        using var reader = cmd.ExecuteReader();
                        if (!reader.Read()) return new(false, $"Member '{first} {last}' was not found.");

                        dbId = Convert.ToString(reader["id"]) ?? "";
                        dbFirst = Convert.ToString(reader["first_name"]) ?? first;
                        dbMiddle = Convert.ToString(reader["middle_name"]) ?? "";
                        dbLast = Convert.ToString(reader["last_name"]) ?? last;
                        fullName = Convert.ToString(reader["full_name"]) ?? $"{dbFirst} {dbLast}".Trim();
                        expiry = Convert.ToDateTime(reader["expiry_date"]).Date;
                        status = Convert.ToString(reader["status"]) ?? "";
                        reader.Close();
                    }

                    if (expiry < date || string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase))
                    {
                        return new(false, $"Membership expired on {expiry:MMM dd, yyyy}. Please renew at front desk.", dbId, fullName);
                    }

                    // Check duplicate attendance today
                    using var dupCmd = new MySqlCommand(@"
SELECT COUNT(*) FROM attendance_records
WHERE member_id=@id AND attendance_date=@date AND is_deleted=0", connection, transaction);
                    dupCmd.Parameters.AddWithValue("@id", dbId);
                    dupCmd.Parameters.AddWithValue("@date", date);
                    if (Convert.ToInt32(dupCmd.ExecuteScalar()) > 0)
                    {
                        return new(false, $"Already checked in today ({now:h:mm tt}).", dbId, fullName, date, now);
                    }

                    var attendanceId = DataStore.NewAttendanceId();
                    using var attCmd = new MySqlCommand(@"
INSERT INTO attendance_records
(id, member_id, member_name, attendance_date, check_in_time, recorded_by)
VALUES (@id, @memberId, @name, @date, @time, @by)", connection, transaction);
                    attCmd.Parameters.AddWithValue("@id", attendanceId);
                    attCmd.Parameters.AddWithValue("@memberId", dbId);
                    attCmd.Parameters.AddWithValue("@name", fullName);
                    attCmd.Parameters.AddWithValue("@date", date);
                    attCmd.Parameters.AddWithValue("@time", now);
                    attCmd.Parameters.AddWithValue("@by", recordedBy);
                    attCmd.ExecuteNonQuery();

                    using var visitCmd = new MySqlCommand(@"
INSERT INTO daily_gym_visits
(id, first_name, middle_name, last_name, member_id, is_gym_member, visit_date, fee, payment_method, payment_reference, registered_by)
VALUES (@id, @first, @middle, @last, @memberId, 1, @date, 0.00, 'Included', '', @by)", connection, transaction);
                    visitCmd.Parameters.AddWithValue("@id", DataStore.NewDailyGymVisitId());
                    visitCmd.Parameters.AddWithValue("@first", dbFirst);
                    visitCmd.Parameters.AddWithValue("@middle", dbMiddle);
                    visitCmd.Parameters.AddWithValue("@last", dbLast);
                    visitCmd.Parameters.AddWithValue("@memberId", dbId);
                    visitCmd.Parameters.AddWithValue("@date", date);
                    visitCmd.Parameters.AddWithValue("@by", recordedBy);
                    visitCmd.ExecuteNonQuery();

                    transaction.Commit();

                    // Keep in-memory store in sync
                    DataStore.AddOrUpdateAttendanceFromMemberVisit(new DailyGymVisit
                    {
                        Id = attendanceId,
                        MemberId = dbId,
                        FirstName = dbFirst,
                        MiddleName = dbMiddle,
                        LastName = dbLast,
                        IsGymMember = true,
                        VisitDate = date
                    }, recordedBy);

                    return new(true, $"Welcome to UniversalGym, {fullName}!", dbId, fullName, date, now);
                }
                catch (MySqlException ex) when (ex.Number == 1062)
                {
                    transaction.Rollback();
                    return new(false, "Already checked in today.", memberId);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"MySQL attendance error, falling back to local: {ex.Message}");
            }
        }

        // Offline Fallback using in-memory DataStore
        var offlineMember = !string.IsNullOrEmpty(memberId)
            ? DataStore.Members.FirstOrDefault(m => !m.IsDeleted && m.Id.Equals(memberId, StringComparison.OrdinalIgnoreCase))
            : DataStore.Members.FirstOrDefault(m => !m.IsDeleted &&
                m.FirstName.Equals(first, StringComparison.OrdinalIgnoreCase) &&
                m.LastName.Equals(last, StringComparison.OrdinalIgnoreCase));

        if (offlineMember is null)
        {
            return new(false, "Member record not found in system.", memberId);
        }

        if (offlineMember.ExpiryDate.Date < date || string.Equals(offlineMember.Status, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            return new(false, $"Membership expired on {offlineMember.ExpiryDate:MMM dd, yyyy}.", offlineMember.Id, offlineMember.FullName);
        }

        var existingToday = DataStore.AttendanceRecords.Any(a =>
            !a.IsDeleted &&
            a.MemberId == offlineMember.Id &&
            a.AttendanceDate.Date == date);

        if (existingToday)
        {
            return new(false, "Already checked in today.", offlineMember.Id, offlineMember.FullName, date, now);
        }

        var newAtt = new AttendanceRecord
        {
            Id = DataStore.NewAttendanceId(),
            MemberId = offlineMember.Id,
            MemberName = offlineMember.FullName,
            AttendanceDate = date,
            CheckInTime = now,
            RecordedBy = recordedBy
        };

        DataStore.AttendanceRecords.Add(newAtt);
        DataStore.SaveAttendanceRecords();

        var newVisit = new DailyGymVisit
        {
            Id = DataStore.NewDailyGymVisitId(),
            MemberId = offlineMember.Id,
            FirstName = offlineMember.FirstName,
            MiddleName = offlineMember.MiddleName,
            LastName = offlineMember.LastName,
            IsGymMember = true,
            VisitDate = date,
            Fee = 0,
            PaymentMethod = "Included",
            RegisteredBy = recordedBy
        };
        DataStore.DailyGymVisits.Add(newVisit);
        DataStore.SaveDailyGymVisits();

        return new(true, $"Welcome, {offlineMember.FullName}! (Saved offline)", offlineMember.Id, offlineMember.FullName, date, now);
    }

    public static DateTime ManilaNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ManilaTimeZone);

    private static TimeZoneInfo ResolveManilaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); } catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"); } catch { }
        return TimeZoneInfo.Local;
    }
}

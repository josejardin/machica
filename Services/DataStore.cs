using GymRegistrationApp.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace GymRegistrationApp.Services;

public static class DataStore
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GymRegistrationApp");
    private static readonly string MembersFile = Path.Combine(Folder, "members.json");
    private static readonly string PaymentsFile = Path.Combine(Folder, "payments.json");
    private static readonly string UsersFile = Path.Combine(Folder, "users.json");
    private static readonly string StaffFile = Path.Combine(Folder, "staff.json");
    private static readonly string MembershipRenewalsFile = Path.Combine(Folder, "membership-renewals.json");
    private static readonly string DailyGymVisitsFile = Path.Combine(Folder, "daily-gym-visits.json");
    private static readonly string AttendanceFile = Path.Combine(Folder, "attendance.json");
    private static readonly string StaffActivityFile = Path.Combine(Folder, "staff-activity.json");
    private static readonly string PlansFile = Path.Combine(Folder, "gym-plans.json");
    private static readonly string ExpensesFile = Path.Combine(Folder, "expenses.json");
    private static readonly string ExpenseCategoriesFile = Path.Combine(Folder, "expense-categories.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly object MemberIdLock = new();
    public static List<Member> Members { get; private set; } = new();
    public static List<Payment> Payments { get; private set; } = new();
    public static List<AppUser> Users { get; private set; } = new();
    public static List<Staff> Staff { get; private set; } = new();
    public static List<MembershipRenewal> MembershipRenewals { get; private set; } = new();
    public static List<DailyGymVisit> DailyGymVisits { get; private set; } = new();
    public static List<AttendanceRecord> AttendanceRecords { get; private set; } = new();
    public static List<StaffActivityRecord> StaffActivity { get; private set; } = new();
    public static List<GymPlan> Plans { get; private set; } = DefaultPlans();
    public static List<Expense> Expenses { get; private set; } = new();
    public static List<string> ExpenseCategories { get; private set; } = DefaultExpenseCategories();
    public static event EventHandler? DataReloaded;
    private static bool _databaseAvailable;

    public static void Initialize()
    {
        Directory.CreateDirectory(Folder);
        _databaseAvailable = false;
        if (DatabaseConnection.TestConnection() && DatabaseConnection.TryEnsureSchema())
        {
            try
            {
                _databaseAvailable = true;
                LoadFromDatabase();
                EnsureAdminOwnerAccount();
                SaveUsers();
                SaveStaff();
                if (Users.Count == 0)
                {
                    Users.Add(new AppUser { Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), Role = "Owner" });
                    SaveUsers();
                }
                return;
            }
            catch (Exception ex) { Console.Error.WriteLine($"Database initialization error: {ex.Message}"); }
        }
        LoadFromFiles();
        EnsureAdminOwnerAccount();
        if (Users.Count == 0) Users.Add(new AppUser { Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), Role = "Owner" });
        SaveAllFiles();
    }

    public static void Reload()
    {
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            LoadFromDatabase();
            DataReloaded?.Invoke(null, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _databaseAvailable = false;
            Console.Error.WriteLine($"Database refresh error: {ex.Message}");
        }
    }

    private static void EnsureAdminOwnerAccount()
    {
        if (!Users.Any(u => u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)))
        {
            Users.Add(new AppUser
            {
                Username = "admin",
                PasswordHash = PasswordHasher.Hash("admin123"),
                Role = "Owner",
                IsActive = true
            });
        }

        if (!Users.Any(u => u.Username.Equals("staff", StringComparison.OrdinalIgnoreCase)))
        {
            Users.Add(new AppUser
            {
                Username = "staff",
                PasswordHash = PasswordHasher.Hash("staff123"),
                Role = "Staff",
                IsActive = true
            });
        }
    }

    private static bool EnsureDatabaseAvailable()
    {
        if (_databaseAvailable) return true;
        if (!DatabaseConnection.TestConnection() || !DatabaseConnection.TryEnsureSchema()) return false;
        _databaseAvailable = true;
        return true;
    }

    private static void LoadFromDatabase()
    {
        using var connection = DatabaseConnection.GetConnection(); connection.Open();
        Members = Read(connection, "SELECT id,first_name,middle_name,last_name,full_name,phone,email,plan,fee,start_date,expiry_date,status,is_deleted,deleted_at,deleted_by FROM members", r => new Member { Id = S(r,"id"), FirstName=S(r,"first_name"), MiddleName=S(r,"middle_name"), LastName=S(r,"last_name"), FullName=S(r,"full_name"), Phone=S(r,"phone"), Email=S(r,"email"), Plan=S(r,"plan"), Fee=D(r,"fee"), StartDate=DT(r,"start_date"), ExpiryDate=DT(r,"expiry_date"), Status=S(r,"status"), IsDeleted=B(r,"is_deleted"), DeletedAt=ND(r,"deleted_at"), DeletedBy=S(r,"deleted_by") });
        Payments = Read(connection, "SELECT reference,member_id,member_name,amount,COALESCE(amount_paid,amount) amount_paid,COALESCE(change_amount,0) change_amount,method,COALESCE(payment_type,'Membership Payment') payment_type,COALESCE(collected_by,'') collected_by,date,status,COALESCE(is_deleted,0) is_deleted,deleted_at,COALESCE(deleted_by,'') deleted_by FROM payments", r => new Payment { Reference=S(r,"reference"), MemberId=S(r,"member_id"), MemberName=S(r,"member_name"), Amount=D(r,"amount"), AmountPaid=D(r,"amount_paid"), Change=D(r,"change_amount"), Method=S(r,"method"), PaymentType=S(r,"payment_type"), CollectedBy=S(r,"collected_by"), Date=DT(r,"date"), Status=S(r,"status"), IsDeleted=B(r,"is_deleted"), DeletedAt=ND(r,"deleted_at"), DeletedBy=S(r,"deleted_by") });
        Users = Read(connection, "SELECT username,password,role,is_active,is_deleted,deleted_at,deleted_by FROM users", r => new AppUser { Username=S(r,"username"), PasswordHash=HashOrLegacy(S(r,"password")), Role=S(r,"role"), IsActive=B(r,"is_active"), IsDeleted=B(r,"is_deleted"), DeletedAt=ND(r,"deleted_at"), DeletedBy=S(r,"deleted_by") });
        Staff = Read(connection, "SELECT id,username,password,role,full_name,email,phone_number,birthday,home_address,joined_date,status,permissions_json,is_deleted,deleted_at,deleted_by FROM users WHERE full_name <> ''", r => MapStaff(r));
         NormalizeStaffRoles();
        MembershipRenewals = Read(connection, "SELECT reference,member_id,member_name,previous_plan,new_plan,previous_status,previous_start_date,previous_expiry_date,previous_fee,new_start_date,new_expiry_date,new_fee,amount_paid,outstanding_amount,payment_method,payment_reference,renewal_date,processed_by,status FROM membership_renewals", r => new MembershipRenewal { Reference=S(r,"reference"), MemberId=S(r,"member_id"), MemberName=S(r,"member_name"), PreviousPlan=S(r,"previous_plan"), NewPlan=S(r,"new_plan"), PreviousStatus=S(r,"previous_status"), PreviousStartDate=DT(r,"previous_start_date"), PreviousExpiryDate=DT(r,"previous_expiry_date"), PreviousFee=D(r,"previous_fee"), NewStartDate=DT(r,"new_start_date"), NewExpiryDate=DT(r,"new_expiry_date"), NewFee=D(r,"new_fee"), AmountPaid=D(r,"amount_paid"), OutstandingAmount=D(r,"outstanding_amount"), PaymentMethod=S(r,"payment_method"), PaymentReference=S(r,"payment_reference"), RenewalDate=DT(r,"renewal_date"), ProcessedBy=S(r,"processed_by"), Status=S(r,"status") });
         DailyGymVisits = Read(connection, "SELECT id,first_name,middle_name,last_name,COALESCE(member_id,'') member_id,is_gym_member,visit_date,fee,payment_method,payment_reference,registered_by,is_deleted,deleted_at,deleted_by FROM daily_gym_visits", r => new DailyGymVisit { Id=S(r,"id"), FirstName=S(r,"first_name"), MiddleName=S(r,"middle_name"), LastName=S(r,"last_name"), MemberId=S(r,"member_id"), IsGymMember=B(r,"is_gym_member"), VisitDate=DT(r,"visit_date"), Fee=D(r,"fee"), PaymentMethod=S(r,"payment_method"), PaymentReference=S(r,"payment_reference"), RegisteredBy=S(r,"registered_by"), IsDeleted=B(r,"is_deleted"), DeletedAt=ND(r,"deleted_at"), DeletedBy=S(r,"deleted_by") });
        AttendanceRecords = Read(connection, "SELECT id,member_id,member_name,attendance_date,check_in_time,recorded_by,is_deleted,deleted_at,deleted_by FROM attendance_records", r => new AttendanceRecord { Id=S(r,"id"), MemberId=S(r,"member_id"), MemberName=S(r,"member_name"), AttendanceDate=DT(r,"attendance_date"), CheckInTime=DT(r,"check_in_time"), RecordedBy=S(r,"recorded_by"), IsDeleted=B(r,"is_deleted"), DeletedAt=ND(r,"deleted_at"), DeletedBy=S(r,"deleted_by") });
        StaffActivity = Read(connection, "SELECT id,username,staff_name,account_type,action,details,COALESCE(member_id,'') member_id,occurred_at FROM staff_activity", r => new StaffActivityRecord { Id=S(r,"id"), Username=S(r,"username"), StaffName=S(r,"staff_name"), AccountType=S(r,"account_type"), Action=S(r,"action"), Details=S(r,"details"), MemberId=S(r,"member_id"), OccurredAt=DT(r,"occurred_at") });
        Plans = DatabaseConnection.GetGymPlans();
        if (Plans.Count == 0) Plans = DefaultPlans();
         Expenses = Read(connection, "SELECT id,expense_name,category,amount,expense_date,payment_method,COALESCE(description,'') description,recorded_by,status,is_deleted,deleted_at,deleted_by FROM expenses", r => new Expense { Id=S(r,"id"), Name=S(r,"expense_name"), Category=S(r,"category"), Amount=D(r,"amount"), ExpenseDate=DT(r,"expense_date"), PaymentMethod=S(r,"payment_method"), Description=S(r,"description"), RecordedBy=S(r,"recorded_by"), Status=S(r,"status"), IsDeleted=B(r,"is_deleted"), DeletedAt=ND(r,"deleted_at"), DeletedBy=S(r,"deleted_by") });
         ExpenseCategories = Read(connection, "SELECT name FROM expense_categories WHERE is_active=1 AND name <> 'Internet Bill' ORDER BY name", r => S(r, "name"));
         if (ExpenseCategories.Count == 0) ExpenseCategories = DefaultExpenseCategories();
         NormalizeDailyGymMemberVisits();
         foreach (var visit in DailyGymVisits.Where(v => !v.IsDeleted && v.IsGymMember)) AddOrUpdateAttendanceFromMemberVisit(visit, visit.RegisteredBy);
         RecalculateMemberBalances(); SyncArchiveRecords();
    }

    private static List<T> Read<T>(MySqlConnection connection, string sql, Func<MySqlDataReader, T> map)
    {
        var values = new List<T>();
        try
        {
            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    values.Add(map(reader));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Row mapping error in query '{sql}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Read query failed for '{sql}': {ex.Message}");
        }
        return values;
    }
    private static string S(MySqlDataReader r,string n) => r[n] is DBNull ? "" : Convert.ToString(r[n]) ?? "";
    private static decimal D(MySqlDataReader r,string n) => r[n] is DBNull ? 0m : Convert.ToDecimal(r[n]);
    private static DateTime DT(MySqlDataReader r,string n) => r[n] is DBNull ? DateTime.Today : Convert.ToDateTime(r[n]);
    private static DateTime? ND(MySqlDataReader r,string n) => r[n] is DBNull ? null : Convert.ToDateTime(r[n]);
    private static bool B(MySqlDataReader r,string n) => r[n] is not DBNull && Convert.ToBoolean(r[n]);
    private static string HashOrLegacy(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal) ? value : PasswordHasher.Hash(value);
    public static Staff MapStaff(MySqlDataReader r)
    {
        var fullName = S(r, "full_name");
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.FirstOrDefault() ?? "";
        var lastName = nameParts.Length > 1 ? nameParts[^1] : "";
        var middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : "";
        var permissions = StaffPermissions.CreateDefault();

        var permissionsJson = S(r, "permissions_json");
        if (!string.IsNullOrWhiteSpace(permissionsJson))
        {
            try { permissions = JsonSerializer.Deserialize<StaffPermissions>(permissionsJson) ?? permissions; }
            catch (JsonException) { }
        }

        return new Staff
        {
            Id = Convert.ToInt32(r["id"]),
            Username = S(r, "username"),
            PasswordHash = HashOrLegacy(S(r, "password")),
            FullName = fullName,
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            Email = S(r, "email"),
            PhoneNumber = S(r, "phone_number"),
            Birthday = DT(r, "birthday"),
             Position = string.IsNullOrWhiteSpace(S(r, "role")) ? "Staff" : S(r, "role"),
            HomeAddress = S(r, "home_address"),
            JoinedDate = DT(r, "joined_date"),
            Status = S(r, "status"),
            IsDeleted = B(r, "is_deleted"),
            DeletedAt = ND(r, "deleted_at"),
            DeletedBy = S(r, "deleted_by"),
            Permissions = permissions
        };
    }

    public static void SaveMembers()
    {
        Save(MembersFile, Members);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var m in Members)
            {
                Execute(@"INSERT INTO members(id,first_name,middle_name,last_name,full_name,phone,email,plan,fee,start_date,expiry_date,status,is_deleted,deleted_at,deleted_by) VALUES(@id,@first,@middle,@last,@full,@phone,@email,@plan,@fee,@start,@expiry,@status,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE first_name=VALUES(first_name),middle_name=VALUES(middle_name),last_name=VALUES(last_name),full_name=VALUES(full_name),phone=VALUES(phone),email=VALUES(email),plan=VALUES(plan),fee=VALUES(fee),start_date=VALUES(start_date),expiry_date=VALUES(expiry_date),status=VALUES(status),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)", P("@id",m.Id),P("@first",m.FirstName),P("@middle",m.MiddleName),P("@last",m.LastName),P("@full",m.FullName),P("@phone",m.Phone),P("@email",m.Email),P("@plan",m.Plan),P("@fee",m.Fee),P("@start",m.StartDate.Date),P("@expiry",m.ExpiryDate.Date),P("@status",m.Status),P("@deleted",m.IsDeleted),P("@deletedAt",m.DeletedAt),P("@deletedBy",m.DeletedBy));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveMembers MySQL error: {ex.Message}"); }
    }

    public static void SavePayments()
    {
        Save(PaymentsFile, Payments);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var p in Payments)
            {
                Execute(@"INSERT INTO payments(reference,member_id,member_name,amount,amount_paid,change_amount,method,payment_type,collected_by,date,status,is_deleted,deleted_at,deleted_by) VALUES(@ref,NULLIF(@member,''),@name,@amount,@paid,@change,@method,@type,@by,@date,@status,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE member_id=VALUES(member_id),member_name=VALUES(member_name),amount=VALUES(amount),amount_paid=VALUES(amount_paid),change_amount=VALUES(change_amount),method=VALUES(method),payment_type=VALUES(payment_type),collected_by=VALUES(collected_by),date=VALUES(date),status=VALUES(status),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)", P("@ref",p.Reference),P("@member",p.MemberId),P("@name",p.MemberName),P("@amount",p.Amount),P("@paid",p.AmountPaid == 0 ? p.Amount : p.AmountPaid),P("@change",p.Change),P("@method",p.Method),P("@type",p.PaymentType),P("@by",p.CollectedBy),P("@date",p.Date),P("@status",p.Status),P("@deleted",p.IsDeleted),P("@deletedAt",p.DeletedAt),P("@deletedBy",p.DeletedBy));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SavePayments MySQL error: {ex.Message}"); }
    }

    public static void SaveUsers()
    {
        Save(UsersFile, Users);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var u in Users)
            {
                Execute("INSERT INTO users(username,password,role,is_active,is_deleted,deleted_at,deleted_by) VALUES(@username,@password,@role,@active,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE password=VALUES(password),role=VALUES(role),is_active=VALUES(is_active),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)",P("@password",string.IsNullOrWhiteSpace(u.PasswordHash)?u.Password:u.PasswordHash),P("@role",u.Role),P("@active",u.IsActive),P("@deleted",u.IsDeleted),P("@deletedAt",u.DeletedAt),P("@deletedBy",u.DeletedBy),P("@username",u.Username));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveUsers MySQL error: {ex.Message}"); }
    }

    public static void SaveStaff()
    {
        Save(StaffFile, Staff);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var s in Staff) UpsertStaff(s);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveStaff MySQL error: {ex.Message}"); }
    }

    public static void SavePlans()
    {
        Save(PlansFile, Plans);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var plan in Plans) DatabaseConnection.SaveGymPlan(plan);
            Plans = DatabaseConnection.GetGymPlans();
            if (Plans.Count == 0) Plans = DefaultPlans();
        }
        catch (Exception ex) { Console.Error.WriteLine($"SavePlans MySQL error: {ex.Message}"); }
    }

    public static void UpsertStaff(Staff s)
    {
        if (string.IsNullOrWhiteSpace(s.Position)) s.Position = "Staff";
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            Execute("INSERT INTO users(username,password,role,full_name,email,phone_number,birthday,home_address,joined_date,status,permissions_json,is_active,is_deleted,deleted_at,deleted_by) VALUES(@username,@password,@role,@full,@email,@phone,@birthday,@address,@joined,@status,@permissions,1,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE password=VALUES(password),role=VALUES(role),full_name=VALUES(full_name),email=VALUES(email),phone_number=VALUES(phone_number),birthday=VALUES(birthday),home_address=VALUES(home_address),joined_date=VALUES(joined_date),status=VALUES(status),permissions_json=VALUES(permissions_json),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)",P("@username",s.Username),P("@password",string.IsNullOrWhiteSpace(s.PasswordHash)?s.Password:s.PasswordHash),P("@role",s.Position),P("@full",s.FullName),P("@email",s.Email),P("@phone",s.PhoneNumber),P("@birthday",s.Birthday),P("@address",s.HomeAddress),P("@joined",s.JoinedDate),P("@status",s.Status),P("@permissions",JsonSerializer.Serialize(s.Permissions)),P("@deleted",s.IsDeleted),P("@deletedAt",s.DeletedAt),P("@deletedBy",s.DeletedBy));
        }
        catch (Exception ex) { Console.Error.WriteLine($"UpsertStaff MySQL error: {ex.Message}"); }
    }

    public static void SaveMembershipRenewals()
    {
        Save(MembershipRenewalsFile, MembershipRenewals);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var r in MembershipRenewals)
            {
                Execute("INSERT INTO membership_renewals(reference,member_id,member_name,previous_plan,new_plan,previous_status,previous_start_date,previous_expiry_date,previous_fee,new_start_date,new_expiry_date,new_fee,amount_paid,outstanding_amount,payment_method,payment_reference,renewal_date,processed_by,status) VALUES(@ref,@id,@name,@pp,@np,@ps,@pstart,@pexp,@pfee,@nstart,@nexp,@nfee,@paid,@out,@method,@payref,@date,@by,@status) ON DUPLICATE KEY UPDATE status=VALUES(status),amount_paid=VALUES(amount_paid),outstanding_amount=VALUES(outstanding_amount)",P("@ref",r.Reference),P("@id",r.MemberId),P("@name",r.MemberName),P("@pp",r.PreviousPlan),P("@np",r.NewPlan),P("@ps",r.PreviousStatus),P("@pstart",r.PreviousStartDate),P("@pexp",r.PreviousExpiryDate),P("@pfee",r.PreviousFee),P("@nstart",r.NewStartDate),P("@nexp",r.NewExpiryDate),P("@nfee",r.NewFee),P("@paid",r.AmountPaid),P("@out",r.OutstandingAmount),P("@method",r.PaymentMethod),P("@payref",r.PaymentReference),P("@date",r.RenewalDate),P("@by",r.ProcessedBy),P("@status",r.Status));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveMembershipRenewals MySQL error: {ex.Message}"); }
    }

    public static void SaveDailyGymVisits()
    {
        Save(DailyGymVisitsFile, DailyGymVisits);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var v in DailyGymVisits)
            {
                Execute("INSERT INTO daily_gym_visits(id,first_name,middle_name,last_name,member_id,is_gym_member,visit_date,fee,payment_method,payment_reference,registered_by,is_deleted,deleted_at,deleted_by) VALUES(@id,@first,@middle,@last,NULLIF(@member,''),@gym,@date,@fee,@method,@ref,@by,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE first_name=VALUES(first_name),middle_name=VALUES(middle_name),last_name=VALUES(last_name),member_id=VALUES(member_id),is_gym_member=VALUES(is_gym_member),visit_date=VALUES(visit_date),fee=VALUES(fee),payment_method=VALUES(payment_method),payment_reference=VALUES(payment_reference),registered_by=VALUES(registered_by),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)",P("@id",v.Id),P("@first",v.FirstName),P("@middle",v.MiddleName),P("@last",v.LastName),P("@member",v.MemberId),P("@gym",v.IsGymMember),P("@date",v.VisitDate.Date),P("@fee",v.Fee),P("@method",v.PaymentMethod),P("@ref",v.PaymentReference),P("@by",v.RegisteredBy),P("@deleted",v.IsDeleted),P("@deletedAt",v.DeletedAt),P("@deletedBy",v.DeletedBy));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveDailyGymVisits MySQL error: {ex.Message}"); }
    }

    public static void SaveAttendanceRecords()
    {
        Save(AttendanceFile, AttendanceRecords);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var a in AttendanceRecords)
            {
                Execute("INSERT INTO attendance_records(id,member_id,member_name,attendance_date,check_in_time,recorded_by,is_deleted,deleted_at,deleted_by) VALUES(@id,@member,@name,@date,@time,@by,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE member_name=VALUES(member_name),check_in_time=VALUES(check_in_time),recorded_by=VALUES(recorded_by),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)",P("@id",a.Id),P("@member",a.MemberId),P("@name",a.MemberName),P("@date",a.AttendanceDate.Date),P("@time",a.CheckInTime),P("@by",a.RecordedBy),P("@deleted",a.IsDeleted),P("@deletedAt",a.DeletedAt),P("@deletedBy",a.DeletedBy));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveAttendanceRecords MySQL error: {ex.Message}"); }
    }
    public static void AddOrUpdateAttendanceFromMemberVisit(DailyGymVisit visit, string recordedBy)
    {
        if (!visit.IsGymMember || string.IsNullOrWhiteSpace(visit.MemberId)) return;
        var member = Members.FirstOrDefault(m => !m.IsDeleted && m.Id == visit.MemberId);
        if (member is null) return;

        var attendance = AttendanceRecords.FirstOrDefault(a => a.MemberId == member.Id && a.AttendanceDate.Date == visit.VisitDate.Date);
        if (attendance is null)
        {
            attendance = new AttendanceRecord { Id = NewAttendanceId(), MemberId = member.Id };
            AttendanceRecords.Add(attendance);
        }

        attendance.MemberName = member.FullName;
        attendance.AttendanceDate = visit.VisitDate.Date;
        attendance.CheckInTime = DateTime.Now;
        attendance.RecordedBy = recordedBy;
        attendance.IsDeleted = false;
        attendance.DeletedAt = null;
        attendance.DeletedBy = "";
        SaveAttendanceRecords();
    }

    public static void SaveArchiveRecord(string recordType, string recordId, string displayName, DateTime archivedAt, string archivedBy, string details = "")
    {
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            Execute("INSERT INTO archive_records(record_type,record_id,display_name,archived_at,archived_by,details) VALUES(@type,@id,@name,@date,@by,@details) ON DUPLICATE KEY UPDATE display_name=VALUES(display_name),archived_at=VALUES(archived_at),archived_by=VALUES(archived_by),details=VALUES(details)", P("@type", recordType), P("@id", recordId), P("@name", displayName), P("@date", archivedAt), P("@by", archivedBy), P("@details", details));
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveArchiveRecord MySQL error: {ex.Message}"); }
    }

    public static void RemoveArchiveRecord(string recordType, string recordId)
    {
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            Execute("DELETE FROM archive_records WHERE record_type=@type AND record_id=@id", P("@type", recordType), P("@id", recordId));
        }
        catch (Exception ex) { Console.Error.WriteLine($"RemoveArchiveRecord MySQL error: {ex.Message}"); }
    }

    private static void SyncArchiveRecords()
    {
        foreach (var member in Members.Where(m => m.IsDeleted)) SaveArchiveRecord("Member", member.Id, member.FullName, member.DeletedAt ?? DateTime.Now, member.DeletedBy);
        foreach (var payment in Payments.Where(p => p.IsDeleted || string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase))) SaveArchiveRecord("Payment", payment.Reference, payment.MemberName, payment.DeletedAt ?? DateTime.Now, payment.DeletedBy, $"Amount: {payment.Amount:N2}");
        foreach (var staff in Staff.Where(s => s.IsDeleted)) SaveArchiveRecord("Staff", staff.Username, staff.FullName, staff.DeletedAt ?? DateTime.Now, staff.DeletedBy, staff.Position);
        foreach (var visit in DailyGymVisits.Where(v => v.IsDeleted)) SaveArchiveRecord("DailyGymVisit", visit.Id, visit.FullName, visit.DeletedAt ?? DateTime.Now, visit.DeletedBy, visit.VisitDate.ToString("yyyy-MM-dd"));
    }
    public static void SaveStaffActivity()
    {
        Save(StaffActivityFile, StaffActivity);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var a in StaffActivity) Execute("INSERT IGNORE INTO staff_activity(id,username,staff_name,account_type,action,details,member_id,occurred_at) VALUES(@id,@user,@name,@type,@action,@details,NULLIF(@member,''),@date)",P("@id",a.Id),P("@user",a.Username),P("@name",a.StaffName),P("@type",a.AccountType),P("@action",a.Action),P("@details",a.Details),P("@member",a.MemberId),P("@date",a.OccurredAt));
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveStaffActivity MySQL error: {ex.Message}"); }
    }
    public static void SaveExpenses()
    {
        Save(ExpensesFile, Expenses);
        Save(ExpenseCategoriesFile, ExpenseCategories);
        if (!EnsureDatabaseAvailable()) return;
        try
        {
            foreach (var category in ExpenseCategories.Where(c => !string.IsNullOrWhiteSpace(c)))
                Execute("INSERT IGNORE INTO expense_categories(name) VALUES(@name)", P("@name", category.Trim()));
            foreach (var expense in Expenses)
                Execute("INSERT INTO expenses(id,expense_name,category,amount,expense_date,payment_method,description,recorded_by,status,is_deleted,deleted_at,deleted_by) VALUES(@id,@name,@category,@amount,@date,@method,@description,@by,@status,@deleted,@deletedAt,@deletedBy) ON DUPLICATE KEY UPDATE expense_name=VALUES(expense_name),category=VALUES(category),amount=VALUES(amount),expense_date=VALUES(expense_date),payment_method=VALUES(payment_method),description=VALUES(description),recorded_by=VALUES(recorded_by),status=VALUES(status),is_deleted=VALUES(is_deleted),deleted_at=VALUES(deleted_at),deleted_by=VALUES(deleted_by)", P("@id",expense.Id), P("@name",expense.Name), P("@category",expense.Category), P("@amount",expense.Amount), P("@date",expense.ExpenseDate.Date), P("@method",expense.PaymentMethod), P("@description",expense.Description), P("@by",expense.RecordedBy), P("@status",expense.Status), P("@deleted",expense.IsDeleted), P("@deletedAt",expense.DeletedAt), P("@deletedBy",expense.DeletedBy));
        }
        catch (Exception ex) { Console.Error.WriteLine($"SaveExpenses MySQL error: {ex.Message}"); }
    }
    public static IEnumerable<Expense> ActiveExpenses => Expenses.Where(e => !e.IsDeleted && string.Equals(e.Status, "Active", StringComparison.OrdinalIgnoreCase));
    public static void PermanentlyDeleteMember(string memberId)
    {
        Members.RemoveAll(m => m.Id == memberId);
        AttendanceRecords.RemoveAll(a => a.MemberId == memberId);
        MembershipRenewals.RemoveAll(r => r.MemberId == memberId);
        foreach (var visit in DailyGymVisits.Where(v => v.MemberId == memberId)) visit.MemberId = "";

        if (!EnsureDatabaseAvailable())
        {
            SaveMembers(); SaveAttendanceRecords(); SaveMembershipRenewals(); SaveDailyGymVisits();
            return;
        }

        Execute("DELETE FROM attendance_records WHERE member_id=@member", P("@member", memberId));
        Execute("DELETE FROM membership_renewals WHERE member_id=@member", P("@member", memberId));
        Execute("DELETE FROM members WHERE id=@member", P("@member", memberId));
    }

    public static void PermanentlyDeletePayment(string reference)
    {
        Payments.RemoveAll(p => p.Reference == reference);
        if (!EnsureDatabaseAvailable()) { SavePayments(); return; }
        Execute("DELETE FROM payments WHERE reference=@reference", P("@reference", reference));
    }

    public static void PermanentlyDeleteStaff(string username)
    {
        Staff.RemoveAll(s => s.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        Users.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (!EnsureDatabaseAvailable()) { SaveStaff(); SaveUsers(); return; }
        Execute("DELETE FROM users WHERE username=@username", P("@username", username));
    }

    public static void PermanentlyDeleteDailyGymVisit(string visitId, string paymentReference)
    {
        DailyGymVisits.RemoveAll(v => v.Id == visitId);
        if (!string.IsNullOrWhiteSpace(paymentReference)) Payments.RemoveAll(p => p.Reference == paymentReference);
        if (!EnsureDatabaseAvailable()) { SaveDailyGymVisits(); SavePayments(); return; }
        Execute("DELETE FROM daily_gym_visits WHERE id=@id", P("@id", visitId));
        if (!string.IsNullOrWhiteSpace(paymentReference)) Execute("DELETE FROM payments WHERE reference=@reference", P("@reference", paymentReference));
    }
    private static void Execute(string sql, params MySqlParameter[] p) => DatabaseConnection.ExecuteQuery(sql,p);
    private static MySqlParameter P(string name,object? value) => new(name,value ?? DBNull.Value);

    public static void RecalculateMemberBalances() { foreach(var m in Members.Where(x=>!x.IsDeleted)) { m.TotalPaid=Payments.Where(p=>p.MemberId==m.Id&&!p.IsDeleted&&p.Status=="Paid").Sum(p=>p.AmountPaid>0?p.AmountPaid:p.Amount); m.RemainingBalance=Math.Max(0,m.Fee-m.TotalPaid); } }
    public static IEnumerable<Payment> ActivePayments => Payments.Where(p=>!p.IsDeleted&&!string.Equals(p.Status,"Deleted",StringComparison.OrdinalIgnoreCase));
    public static decimal GetPlanFee(string plan) => Plans.FirstOrDefault(p => p.Name.Equals(plan, StringComparison.OrdinalIgnoreCase))?.Price ?? Member.MonthlyFee;
    public static DateTime GetPlanExpiry(string plan,DateTime start) => start.Date.AddMonths(Plans.FirstOrDefault(p => p.Name.Equals(plan, StringComparison.OrdinalIgnoreCase))?.Months ?? 1);
    public static List<GymPlan> GetPlans() => Plans.Where(p => p.IsActive).OrderBy(p => p.Months).ThenBy(p => p.Name).ToList();
    private static List<GymPlan> DefaultPlans() => new() { new GymPlan { Name = "Daily", Months = 1, Price = 50m }, new GymPlan { Name = "Monthly", Months = 1, Price = Member.MonthlyFee }, new GymPlan { Name = "2-Month Plan", Months = 2, Price = Member.TwoMonthFee }, new GymPlan { Name = "Annual Plan", Months = 12, Price = Member.MonthlyFee * 12 } };
    private static void NormalizeDailyGymMemberVisits() { foreach(var v in DailyGymVisits.Where(v=>v.IsGymMember)) { Payments.RemoveAll(p=>!string.IsNullOrWhiteSpace(v.PaymentReference)&&p.Reference==v.PaymentReference); v.Fee=0; v.PaymentMethod="Included"; v.PaymentReference=""; } }
    public static void RecordStaffActivity(string username,string action,string details,string memberId="") { var staff=Staff.FirstOrDefault(s=>s.Username.Equals(username,StringComparison.OrdinalIgnoreCase)); var user=Users.FirstOrDefault(u=>u.Username.Equals(username,StringComparison.OrdinalIgnoreCase)); StaffActivity.Add(new StaffActivityRecord { Id="ACT-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+Random.Shared.Next(100,999),Username=username,StaffName=staff?.FullName??username,AccountType=user?.Role??staff?.Position??"Staff",Action=action,Details=details,MemberId=memberId,OccurredAt=DateTime.Now }); SaveStaffActivity(); }
    public static string NewMemberId()
    {
        lock (MemberIdLock)
        {
            var next = Members
                .Select(m => m.Id)
                .Where(id => id.StartsWith("GM", StringComparison.OrdinalIgnoreCase))
                .Select(id => int.TryParse(id[2..], out var number) ? number : 100000)
                .DefaultIfEmpty(100000)
                .Max() + 1;

            var candidate = $"GM{next:D6}";
            while (Members.Any(m => m.Id.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                next++;
                candidate = $"GM{next:D6}";
            }

            return candidate;
        }
    }
    public static string NewPaymentReference()=>"PAY-"+DateTime.Now.ToString("yyyyMMdd")+Random.Shared.Next(1000000,9999999).ToString().Substring(0,7);
    public static string NewRenewalReference()=>"REN-"+DateTime.Now.ToString("yyyyMMdd")+Random.Shared.Next(1000000,9999999).ToString().Substring(0,7);
    public static string NewDailyGymVisitId()=>"DG-"+DateTime.Now.ToString("yyyyMMddHHmmss")+Random.Shared.Next(100,999);
    public static string NewAttendanceId()=>"ATT-"+DateTime.Now.ToString("yyyyMMddHHmmss")+Random.Shared.Next(100,999);
    public static string NewExpenseId()=>"EXP-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+Random.Shared.Next(100,999);
    public static string DataFolder=>Folder;

     private static void LoadFromFiles() { Members=Load<List<Member>>(MembersFile)??new(); Payments=Load<List<Payment>>(PaymentsFile)??new(); Users=Load<List<AppUser>>(UsersFile)??new(); Staff=Load<List<Staff>>(StaffFile)??new(); NormalizeStaffRoles(); MembershipRenewals=Load<List<MembershipRenewal>>(MembershipRenewalsFile)??new(); DailyGymVisits=Load<List<DailyGymVisit>>(DailyGymVisitsFile)??new(); AttendanceRecords=Load<List<AttendanceRecord>>(AttendanceFile)??new(); StaffActivity=Load<List<StaffActivityRecord>>(StaffActivityFile)??new(); Plans=Load<List<GymPlan>>(PlansFile)??DefaultPlans(); Expenses=Load<List<Expense>>(ExpensesFile)??new(); ExpenseCategories=Load<List<string>>(ExpenseCategoriesFile)??DefaultExpenseCategories(); ExpenseCategories.RemoveAll(c => c.Equals("Internet Bill", StringComparison.OrdinalIgnoreCase)); EnsureDailyPlan(); RecalculateMemberBalances(); }

    private static void NormalizeStaffRoles()
    {
        foreach (var staff in Staff)
        {
            var user = Users.FirstOrDefault(u => u.Username.Equals(staff.Username, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(staff.Position)) staff.Position = "Staff";
            if (user is not null && string.IsNullOrWhiteSpace(user.Role)) user.Role = staff.Position;
        }
    }
    private static void EnsureDailyPlan() { if (!Plans.Any(p => p.Name.Equals("Daily", StringComparison.OrdinalIgnoreCase))) Plans.Add(new GymPlan { Name = "Daily", Months = 1, Price = 50m }); }
    private static void SaveAllFiles() { Save(MembersFile,Members);Save(PaymentsFile,Payments);Save(UsersFile,Users);Save(StaffFile,Staff);Save(MembershipRenewalsFile,MembershipRenewals);Save(DailyGymVisitsFile,DailyGymVisits);Save(AttendanceFile,AttendanceRecords);Save(StaffActivityFile,StaffActivity);Save(PlansFile,Plans);Save(ExpensesFile,Expenses);Save(ExpenseCategoriesFile,ExpenseCategories); }
    private static List<string> DefaultExpenseCategories() => new() { "Electricity Bill", "Water Bill", "Rental Payment", "Cleaning Supplies", "Maintenance and Repairs", "Office Supplies", "Other Expenses" };
    private static T? Load<T>(string path) { try{return File.Exists(path)?JsonSerializer.Deserialize<T>(File.ReadAllText(path)):default;}catch{return default;} }
    private static void Save<T>(string path, T value)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Local save error for {path}: {ex.Message}");
        }
    }
}

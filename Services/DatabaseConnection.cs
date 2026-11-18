using GymRegistrationApp.Models;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.Json;

namespace GymRegistrationApp.Services;

public static class DatabaseConnection
{
    private const string DefaultConnectionString = "server=127.0.0.1;port=3306;user=root;password=;database=gym_registration_db;Allow User Variables=True;Connection Timeout=5;Default Command Timeout=30;";
    private static readonly object ConfigurationLock = new();
    private static string? _connectionString;

    private static string ConnectionString
    {
        get
        {
            if (_connectionString is not null) return _connectionString;
            lock (ConfigurationLock)
            {
                if (_connectionString is not null) return _connectionString;
                _connectionString = Environment.GetEnvironmentVariable("GYM_DB_CONNECTION_STRING")
                    ?? ReadConfiguredConnectionString()
                    ?? DefaultConnectionString;
                return _connectionString;
            }
        }
    }

    public static MySqlConnection GetConnection() => new(ConnectionString);

    public static bool TestConnection()
    {
        try
        {
            using var connection = GetConnection();
            connection.Open();
            return connection.State == ConnectionState.Open;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database connection error: {ex.Message}");
            return false;
        }
    }

    public static bool TryEnsureSchema()
    {
        try
        {
            EnsureSchema();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database schema error: {ex.Message}");
            return false;
        }
    }

    public static int ExecuteQuery(string query, params MySqlParameter[] parameters)
    {
        try
        {
            using var connection = GetConnection();
            connection.Open();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddRange(parameters);
            return command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ExecuteQuery error: {ex.Message}");
            throw;
        }
    }

    public static object? ExecuteScalar(string query, params MySqlParameter[] parameters)
    {
        try
        {
            using var connection = GetConnection();
            connection.Open();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddRange(parameters);
            return command.ExecuteScalar();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ExecuteScalar error: {ex.Message}");
            return null;
        }
    }

    public static void EnsureSchema()
    {
        const string migration = @"
 ALTER TABLE members ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0, ADD COLUMN IF NOT EXISTS deleted_at DATETIME NULL, ADD COLUMN IF NOT EXISTS deleted_by VARCHAR(100) NOT NULL DEFAULT '';
CREATE TABLE IF NOT EXISTS archive_records (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY, record_type VARCHAR(40) NOT NULL, record_id VARCHAR(100) NOT NULL, display_name VARCHAR(300) NOT NULL DEFAULT '', archived_at DATETIME NOT NULL, archived_by VARCHAR(100) NOT NULL DEFAULT '', details TEXT NULL, UNIQUE KEY uq_archive_record (record_type, record_id), KEY idx_archive_date (archived_at), KEY idx_archive_type (record_type)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
ALTER TABLE payments ADD COLUMN IF NOT EXISTS amount_paid DECIMAL(10,2) NOT NULL DEFAULT 0.00, ADD COLUMN IF NOT EXISTS change_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00, ADD COLUMN IF NOT EXISTS payment_type VARCHAR(60) NOT NULL DEFAULT 'Membership Payment', ADD COLUMN IF NOT EXISTS collected_by VARCHAR(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0, ADD COLUMN IF NOT EXISTS deleted_at DATETIME NULL, ADD COLUMN IF NOT EXISTS deleted_by VARCHAR(100) NOT NULL DEFAULT '';
CREATE TABLE IF NOT EXISTS attendance_records (id VARCHAR(80) PRIMARY KEY, member_id VARCHAR(50) NOT NULL, member_name VARCHAR(300) NOT NULL, attendance_date DATE NOT NULL, check_in_time DATETIME NOT NULL, recorded_by VARCHAR(100) NOT NULL DEFAULT '', is_deleted TINYINT(1) NOT NULL DEFAULT 0, deleted_at DATETIME NULL, deleted_by VARCHAR(100) NOT NULL DEFAULT '', UNIQUE KEY uq_attendance_member_day (member_id, attendance_date), KEY idx_attendance_date (attendance_date), CONSTRAINT fk_attendance_member FOREIGN KEY (member_id) REFERENCES members(id) ON UPDATE CASCADE ON DELETE RESTRICT) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS daily_gym_visits (id VARCHAR(80) PRIMARY KEY, first_name VARCHAR(100) NOT NULL, last_name VARCHAR(100) NOT NULL DEFAULT '', member_id VARCHAR(50) NULL, is_gym_member TINYINT(1) NOT NULL DEFAULT 0, visit_date DATE NOT NULL, fee DECIMAL(10,2) NOT NULL DEFAULT 0.00, payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash', payment_reference VARCHAR(100) NOT NULL DEFAULT '', registered_by VARCHAR(100) NOT NULL DEFAULT '', is_deleted TINYINT(1) NOT NULL DEFAULT 0, deleted_at DATETIME NULL, deleted_by VARCHAR(100) NOT NULL DEFAULT '', KEY idx_visits_date (visit_date), KEY idx_visits_member (member_id), CONSTRAINT fk_visit_member FOREIGN KEY (member_id) REFERENCES members(id) ON UPDATE CASCADE ON DELETE SET NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
ALTER TABLE daily_gym_visits ADD COLUMN IF NOT EXISTS middle_name VARCHAR(100) NOT NULL DEFAULT '';
CREATE TABLE IF NOT EXISTS membership_renewals (reference VARCHAR(100) PRIMARY KEY, member_id VARCHAR(50) NOT NULL, member_name VARCHAR(300) NOT NULL, previous_plan VARCHAR(30) NOT NULL DEFAULT '', new_plan VARCHAR(30) NOT NULL DEFAULT '', previous_status VARCHAR(30) NOT NULL DEFAULT '', previous_start_date DATE NOT NULL, previous_expiry_date DATE NOT NULL, previous_fee DECIMAL(10,2) NOT NULL DEFAULT 0.00, new_start_date DATE NOT NULL, new_expiry_date DATE NOT NULL, new_fee DECIMAL(10,2) NOT NULL DEFAULT 0.00, amount_paid DECIMAL(10,2) NOT NULL DEFAULT 0.00, outstanding_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00, payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash', payment_reference VARCHAR(100) NOT NULL DEFAULT '', renewal_date DATETIME NOT NULL, processed_by VARCHAR(100) NOT NULL DEFAULT '', status VARCHAR(30) NOT NULL DEFAULT 'Completed', KEY idx_renewals_member (member_id), KEY idx_renewals_date (renewal_date), CONSTRAINT fk_renewal_member FOREIGN KEY (member_id) REFERENCES members(id) ON UPDATE CASCADE ON DELETE RESTRICT) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS staff_activity (id VARCHAR(100) PRIMARY KEY, username VARCHAR(50) NOT NULL, staff_name VARCHAR(300) NOT NULL DEFAULT '', account_type VARCHAR(30) NOT NULL DEFAULT 'Staff', action VARCHAR(100) NOT NULL, details TEXT NOT NULL, member_id VARCHAR(50) NULL, occurred_at DATETIME NOT NULL, KEY idx_activity_user_date (username, occurred_at)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
ALTER TABLE users ADD COLUMN IF NOT EXISTS full_name VARCHAR(300) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS email VARCHAR(150) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS phone_number VARCHAR(30) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS birthday DATE NULL, ADD COLUMN IF NOT EXISTS home_address VARCHAR(300) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS joined_date DATE NULL, ADD COLUMN IF NOT EXISTS status VARCHAR(30) NOT NULL DEFAULT 'Active', ADD COLUMN IF NOT EXISTS permissions_json TEXT NULL, ADD COLUMN IF NOT EXISTS is_active TINYINT(1) NOT NULL DEFAULT 1, ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0, ADD COLUMN IF NOT EXISTS deleted_at DATETIME NULL, ADD COLUMN IF NOT EXISTS deleted_by VARCHAR(100) NOT NULL DEFAULT '';
CREATE TABLE IF NOT EXISTS staff_positions (id INT NOT NULL AUTO_INCREMENT, name VARCHAR(100) NOT NULL, created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (id), UNIQUE KEY uq_staff_position_name (name)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS gym_plans (id INT NOT NULL AUTO_INCREMENT, name VARCHAR(100) NOT NULL, months INT NOT NULL DEFAULT 1, price DECIMAL(12,2) NOT NULL DEFAULT 0, is_active TINYINT(1) NOT NULL DEFAULT 1, PRIMARY KEY (id), UNIQUE KEY uq_gym_plans_name (name)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
INSERT IGNORE INTO staff_positions (name) VALUES ('Front Desk Staff'), ('Admin');
INSERT IGNORE INTO gym_plans (name, months, price) VALUES ('Daily', 1, 50.00), ('Monthly', 1, 600.00), ('2-Month Plan', 2, 1200.00), ('Annual Plan', 12, 7200.00);
CREATE TABLE IF NOT EXISTS expense_categories (id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, name VARCHAR(120) NOT NULL UNIQUE, is_active TINYINT(1) NOT NULL DEFAULT 1, created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS expenses (id VARCHAR(80) PRIMARY KEY, expense_name VARCHAR(200) NOT NULL, category VARCHAR(120) NOT NULL, amount DECIMAL(12,2) NOT NULL, expense_date DATE NOT NULL, payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash', description TEXT NULL, recorded_by VARCHAR(100) NOT NULL DEFAULT '', status VARCHAR(30) NOT NULL DEFAULT 'Active', is_deleted TINYINT(1) NOT NULL DEFAULT 0, deleted_at DATETIME NULL, deleted_by VARCHAR(100) NOT NULL DEFAULT '', created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, KEY idx_expenses_date (expense_date), KEY idx_expenses_category (category), KEY idx_expenses_status (status)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
INSERT IGNORE INTO expense_categories (name) VALUES ('Electricity Bill'),('Water Bill'),('Internet Bill'),('Rental Payment'),('Cleaning Supplies'),('Maintenance and Repairs'),('Office Supplies'),('Other Expenses');
UPDATE expense_categories SET is_active=0 WHERE name='Internet Bill';
UPDATE payments SET amount_paid = amount WHERE amount_paid = 0 AND amount <> 0;";
        using var connection = GetConnection();
        connection.Open();
        foreach (var statement in migration.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Migration statement notice: {ex.Message}");
            }
        }

        try
        {
            using var allowWalkInPayments = new MySqlCommand("ALTER TABLE payments MODIFY COLUMN member_id VARCHAR(50) NULL", connection);
            allowWalkInPayments.ExecuteNonQuery();
        }
        catch { }
    }

    public static bool UpdateStaffPosition(string originalName, string newName) => ExecuteQuery("UPDATE staff_positions SET name=@newName WHERE name=@originalName", new MySqlParameter("@newName", newName.Trim()), new MySqlParameter("@originalName", originalName)) > 0;

    public static bool DeleteStaffPosition(string positionName) => ExecuteQuery("DELETE FROM staff_positions WHERE name=@name AND name NOT IN ('Admin','Front Desk Staff')", new MySqlParameter("@name", positionName)) > 0;

    public static List<GymPlan> GetGymPlans()
    {
        var result = new List<GymPlan>();
        try
        {
            using var connection = GetConnection(); connection.Open();
            using var command = new MySqlCommand("SELECT id,name,months,price,is_active FROM gym_plans WHERE is_active=1 ORDER BY months,id", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read()) result.Add(new GymPlan { Id = reader.GetInt32("id"), Name = reader.GetString("name"), Months = reader.GetInt32("months"), Price = reader.GetDecimal("price"), IsActive = reader.GetBoolean("is_active") });
        }
        catch (Exception ex) { Console.Error.WriteLine($"Gym plans error: {ex.Message}"); }
        return result;
    }

    public static int SaveGymPlan(GymPlan plan) => ExecuteQuery("INSERT INTO gym_plans(id,name,months,price,is_active) VALUES(@id,@name,@months,@price,@active) ON DUPLICATE KEY UPDATE name=VALUES(name),months=VALUES(months),price=VALUES(price),is_active=VALUES(is_active)", new MySqlParameter("@id", plan.Id), new MySqlParameter("@name", plan.Name.Trim()), new MySqlParameter("@months", plan.Months), new MySqlParameter("@price", plan.Price), new MySqlParameter("@active", plan.IsActive));

    public static int DeleteGymPlan(int id) => ExecuteQuery("UPDATE gym_plans SET is_active=0 WHERE id=@id", new MySqlParameter("@id", id));
    private static string? ReadConfiguredConnectionString()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return null;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.TryGetProperty("ConnectionStrings", out var strings)
                && strings.TryGetProperty("GymDatabase", out var value)
                ? value.GetString()
                : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database configuration error: {ex.Message}");
            return null;
        }
    }

    public static List<string> GetStaffPositions()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Front Desk Staff", "Admin" };
        try
        {
            using var connection = GetConnection(); connection.Open();
            using var command = new MySqlCommand("SELECT name FROM staff_positions ORDER BY name", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read()) result.Add(reader.GetString(0));
        }
        catch (Exception ex) { Console.Error.WriteLine($"Staff positions error: {ex.Message}"); }
        return result.OrderBy(x => x).ToList();
    }

    public static bool InsertStaffPosition(string positionName)
    {
        if (string.IsNullOrWhiteSpace(positionName)) return false;
        return ExecuteQuery("INSERT IGNORE INTO staff_positions (name) VALUES (@name)", new MySqlParameter("@name", positionName.Trim())) > 0;
    }

    public static List<Staff> GetStaffUsers()
    {
        var staff = new List<Staff>();
        try
        {
            using var connection = GetConnection(); connection.Open();
            using var command = new MySqlCommand("SELECT id, username, password, role, full_name, email, phone_number, birthday, home_address, joined_date, status, permissions_json, is_deleted FROM users WHERE is_deleted=0 AND status='Active' ORDER BY joined_date DESC, username", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read()) staff.Add(DataStore.MapStaff(reader));
        }
        catch (Exception ex) { Console.Error.WriteLine($"Staff load error: {ex.Message}"); }
        return staff;
    }

    public static bool InsertStaff(Staff staff)
    {
        try { DataStore.UpsertStaff(staff); return true; }
        catch (Exception ex) { Console.Error.WriteLine($"Staff save error: {ex.Message}"); return false; }
    }

    public static int GetStaffCount() => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM users WHERE is_deleted=0 AND status='Active'") ?? 0);

    public static bool DeleteStaff(int id)
    {
        return ExecuteQuery("UPDATE users SET is_active=0,is_deleted=1,status='Inactive',deleted_at=NOW() WHERE id=@id", new MySqlParameter("@id", id)) > 0;
    }
}

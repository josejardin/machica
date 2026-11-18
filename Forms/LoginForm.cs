using GymRegistrationApp.Services;

namespace GymRegistrationApp.Forms;

public partial class LoginForm : Form
{
    public LoginForm() 
    { 
        InitializeComponent(); 
        AcceptButton = btnLogin;

        // Test database connection when form loads
        TestDatabaseConnection();
    }

    private void TestDatabaseConnection()
    {
        bool isConnected = DatabaseConnection.TestConnection();
        // The connection is tested silently; errors are logged but don't block login
        if (isConnected)
        {
            // Could add a status indicator here if desired
        }
    }

    private void btnLogin_Click(object? sender, EventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Text;
        var user = DataStore.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        var staff = DataStore.Staff.FirstOrDefault(s => s.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        var staffIsActive = staff is null || (!staff.IsDeleted && string.Equals(staff.Status, "Active", StringComparison.OrdinalIgnoreCase));
        if (user is null || user.IsDeleted || !user.IsActive || !staffIsActive || !PasswordHasher.Verify(password, user.PasswordHash, user.Password))
        {
            lblError.Text = "Incorrect username or password.";
            txtPassword.SelectAll();
            txtPassword.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.Hash(password);
            user.Password = string.Empty;
            DataStore.SaveUsers();
        }

        var effectiveRole = string.IsNullOrWhiteSpace(user.Role)
            ? (staff?.Position ?? "Staff")
            : user.Role;
        DataStore.RecordStaffActivity(user.Username, "Login", $"{effectiveRole} account signed in.");

        Hide();
        using (var main = new MainForm(user.Username, effectiveRole, staff?.Permissions))
        {
            main.ShowDialog();
            // if the main form indicated a sign-out, show the login UI again
            if (main.IsSignOut)
            {
                // reset login inputs and show the form again
                txtPassword.Text = string.Empty;
                txtUsername.Text = string.Empty;
                lblError.Text = string.Empty;
                Show();
                return;
            }
        }
        Close();
    }
    private void chkShowPassword_CheckedChanged(object? sender, EventArgs e) => txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

    private void lblBrand_Click(object? sender, EventArgs e)
    {

    }
}

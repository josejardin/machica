using System.Text.RegularExpressions;
using GymRegistrationApp.Models;
using GymRegistrationApp.Services;

namespace GymRegistrationApp.Forms;

public partial class MemberDialog : Form
{
    private readonly Member? _editing;
    public Member? ResultMember { get; private set; }
    public string PaymentMethod => cmbMethod.SelectedItem?.ToString() ?? "Cash";
    public decimal AmountPaid => numAmountPaid.Value;
    public decimal Change => Math.Max(0m, AmountPaid - numFee.Value);

    public MemberDialog(Member? member = null)
    {
        _editing = member; 
        InitializeComponent();
        cmbPlan.Items.AddRange(DataStore.GetPlans().Select(p => p.Name).Cast<object>().ToArray());
        if (cmbPlan.SelectedIndex < 0) cmbPlan.SelectedIndex = 0;
        cmbMethod.SelectedIndex = 0;
        cmbMethod.Enabled = false;

        // Test database connection on form load
        TestDatabaseConnection();

        if (member is not null)
        {
            lblTitle.Text = "Edit member details";
            cmbPlan.Enabled = false;
            dtStart.Enabled = false;
            numFee.Enabled = false;
            // populate name parts if available, fallback to splitting FullName
            if (!string.IsNullOrWhiteSpace(member.FirstName) || !string.IsNullOrWhiteSpace(member.LastName))
            {
                txtFirstName.Text = member.FirstName;
                txtMiddleName.Text = member.MiddleName;
                txtLastName.Text = member.LastName;
            }
            else if (!string.IsNullOrWhiteSpace(member.FullName))
            {
                var parts = member.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0) txtFirstName.Text = parts[0];
                if (parts.Length > 2) txtMiddleName.Text = string.Join(' ', parts[1..^1]);
                if (parts.Length > 1) txtLastName.Text = parts[^1];
            }
            txtPhone.Text = member.Phone; 
            txtEmail.Text = member.Email;
            cmbPlan.SelectedItem = member.Plan; 
            if (cmbPlan.SelectedIndex < 0) cmbPlan.SelectedIndex = 0;
            dtStart.Value = member.StartDate < dtStart.MinDate ? DateTime.Today : member.StartDate;
            numFee.Value = Math.Min(numFee.Maximum, Math.Max(numFee.Minimum, DataStore.GetPlanFee(member.Plan)));
            var existingPayment = DataStore.Payments
                .Where(p => p.MemberId == member.Id && !p.IsDeleted && p.Status == "Paid")
                .OrderByDescending(p => p.Date)
                .FirstOrDefault();
            numAmountPaid.Value = Math.Min(numAmountPaid.Maximum, existingPayment?.AmountPaid > 0m ? existingPayment.AmountPaid : existingPayment?.Amount ?? 0m);
            numAmountPaid.Enabled = false;
            btnPrintReceipt.Visible = existingPayment is not null;
            btnSave.Text = "Save Changes";
        }
        else
        {
            numAmountPaid.Value = numFee.Value;
        }
        UpdateChangeLabel();
    }

    private void TestDatabaseConnection()
    {
        // Silently test connection; the form continues regardless
        try
        {
            _ = DatabaseConnection.TestConnection();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection test in MemberDialog: {ex.Message}");
        }
    }
    private void cmbPlan_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_editing is not null) return;
        numFee.Value = DataStore.GetPlanFee(cmbPlan.SelectedItem?.ToString() ?? "Monthly");
        numAmountPaid.Value = numFee.Value;
        UpdateChangeLabel();
    }
    private void numAmountPaid_ValueChanged(object? sender, EventArgs e) => UpdateChangeLabel();

    private void UpdateChangeLabel() => lblChange.Text = $"Change\r\n₱{Change:N2}";

    private void btnPrintReceipt_Click(object? sender, EventArgs e)
    {
        if (_editing is null) return;
        var payment = DataStore.Payments
            .Where(p => p.MemberId == _editing.Id && !p.IsDeleted && p.Status == "Paid")
            .OrderByDescending(p => p.Date)
            .FirstOrDefault();
        if (payment is null)
        {
            MessageBox.Show("No payment receipt is available for this member.", "Receipt unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            ReceiptService.PrintReceipt(_editing, payment);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to print the receipt: {ex.Message}", "Print receipt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    private void btnSave_Click(object? sender, EventArgs e)
    {
        var first = txtFirstName.Text.Trim(); var middle = txtMiddleName.Text.Trim(); var last = txtLastName.Text.Trim();
        var phone = txtPhone.Text.Trim(); var email = txtEmail.Text.Trim();
        if (first.Length < 1 || last.Length < 1) { MessageBox.Show("Please enter the member's first and last name."); txtFirstName.Focus(); return; }
        if (!Regex.IsMatch(phone, @"^[0-9]{11}$")) { MessageBox.Show("Phone number must contain exactly 11 digits."); txtPhone.Focus(); return; }
        if (!Regex.IsMatch(email, @"^[^@\s]+@gmail\.com$", RegexOptions.IgnoreCase)) { MessageBox.Show("Please enter a valid Gmail address ending in @gmail.com."); txtEmail.Focus(); return; }
        if (AmountPaid < 0m) { MessageBox.Show("Amount paid cannot be negative."); numAmountPaid.Focus(); return; }
        var plan = _editing?.Plan ?? cmbPlan.SelectedItem?.ToString() ?? "Monthly";
        var start = _editing?.StartDate.Date ?? dtStart.Value.Date;
        var expiry = _editing?.ExpiryDate ?? DataStore.GetPlanExpiry(plan, start);
        var full = string.IsNullOrWhiteSpace(middle) ? $"{first} {last}" : $"{first} {middle} {last}";
        ResultMember = new Member
        {
            Id = _editing?.Id ?? DataStore.NewMemberId(),
            FirstName = first,
            MiddleName = middle,
            LastName = last,
            FullName = full,
            Phone = phone,
            Email = email,
            Plan = plan,
            Fee = _editing?.Fee ?? DataStore.GetPlanFee(plan),
            TotalPaid = _editing?.TotalPaid ?? 0m,
            RemainingBalance = _editing?.RemainingBalance ?? 0m,
            StartDate = start,
            ExpiryDate = expiry,
            Status = _editing?.Status ?? "Active",
            IsDeleted = _editing?.IsDeleted ?? false,
            DeletedAt = _editing?.DeletedAt,
            DeletedBy = _editing?.DeletedBy ?? ""
        };
        DialogResult = DialogResult.OK; Close();
    }

    private void MemberDialog_Load(object? sender, EventArgs e)
    {

    }
}

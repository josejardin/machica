using GymRegistrationApp.Models;
using GymRegistrationApp.Services;

namespace GymRegistrationApp.Forms;

public sealed class AttendanceForm : Form
{
    private readonly string _username;
    private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Long, Width = 150 };
    private readonly TextBox _searchBox = new() { Width = 280, PlaceholderText = "Scan or type Member ID (Press Enter)" };
    private readonly Label _selectedSummary = new() { AutoSize = true };
    private readonly Label _monthSummary = new() { AutoSize = true };
    private readonly Label _selectedMember = new() { AutoSize = true, Text = "No member selected", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
    private readonly Label _feedbackBanner = new() { AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Visible = false };
    private readonly DataGridView _membersGrid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private readonly DataGridView _attendanceGrid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private Member? SelectedMember => (_membersGrid.CurrentRow?.DataBoundItem as MemberSearchResult)?.Member;

    public AttendanceForm(string username, DateTime? selectedDate = null)
    {
        _username = username;
        _datePicker.Value = selectedDate?.Date ?? DateTime.Today;
        Text = "Member Attendance & Check-In";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(950, 650);
        Size = new Size(1150, 750);
        BackColor = Color.FromArgb(245, 246, 249);
        BuildLayout();
        _datePicker.ValueChanged += (_, _) => RefreshView();
        _searchBox.TextChanged += (_, _) => RefreshMembers();
        _searchBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FastCheckIn();
            }
        };
        RefreshView();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 6, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // Title
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Date Controls
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); // Metrics
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // Search & Fast Check-In
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); // Members awaiting attendance
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); // Attended today list

        var title = new Label { Text = "Member Attendance & Fast Check-In", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Dock = DockStyle.Fill };
        root.Controls.Add(title, 0, 0);

        var calendar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var previous = Button("◀ Previous Day"); previous.Click += (_, _) => _datePicker.Value = _datePicker.Value.Date.AddDays(-1);
        var today = Button("📅 Today"); today.Click += (_, _) => _datePicker.Value = DateTime.Today;
        var next = Button("Next Day ▶"); next.Click += (_, _) => _datePicker.Value = _datePicker.Value.Date.AddDays(1);
        calendar.Controls.AddRange(new Control[] { previous, _datePicker, today, next });
        root.Controls.Add(calendar, 0, 1);

        var metrics = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = Color.White, Padding = new Padding(14) };
        metrics.Controls.Add(_selectedSummary);
        metrics.Controls.Add(new Label { Width = 28 });
        metrics.Controls.Add(_monthSummary);
        metrics.Controls.Add(new Label { Width = 28 });
        metrics.Controls.Add(_feedbackBanner);
        root.Controls.Add(metrics, 0, 2);

        var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        searchPanel.Controls.Add(new Label { Text = "Quick Scan / ID:", AutoSize = true, Margin = new Padding(0, 8, 6, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
        searchPanel.Controls.Add(_searchBox);

        var fastCheckInBtn = Button("⚡ Check In (Enter)");
        fastCheckInBtn.Click += (_, _) => FastCheckIn();
        searchPanel.Controls.Add(fastCheckInBtn);

        searchPanel.Controls.Add(new Label { Width = 15 });
        searchPanel.Controls.Add(_selectedMember);

        var addSelectedBtn = Button("Check-in Selected");
        addSelectedBtn.BackColor = Color.FromArgb(60, 80, 110);
        addSelectedBtn.Click += (_, _) => AddAttendance();
        searchPanel.Controls.Add(addSelectedBtn);

        root.Controls.Add(searchPanel, 0, 3);

        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MEMBER", DataPropertyName = "DisplayName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MEMBER ID", DataPropertyName = "Id", Width = 150 });
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PLAN", DataPropertyName = "Plan", Width = 130 });
        _membersGrid.SelectionChanged += (_, _) => UpdateSelectedMember();
        root.Controls.Add(_membersGrid, 0, 4);

        _attendanceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ATTENDED TODAY", DataPropertyName = "MemberName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _attendanceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MEMBER ID", DataPropertyName = "MemberId", Width = 150 });
        _attendanceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CHECK-IN TIME", DataPropertyName = "CheckInTime", Width = 170, DefaultCellStyle = new DataGridViewCellStyle { Format = "h:mm tt" } });
        _attendanceGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "RECORDED BY", DataPropertyName = "RecordedBy", Width = 140 });
        root.Controls.Add(_attendanceGrid, 0, 5);
        Controls.Add(root);
    }

    private static Button Button(string text) => new() 
    { 
        Text = text, 
        AutoSize = true, 
        FlatStyle = FlatStyle.Flat, 
        BackColor = Color.FromArgb(255, 132, 35), 
        ForeColor = Color.White, 
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Margin = new Padding(4, 0, 4, 0),
        Padding = new Padding(6, 4, 6, 4),
        Cursor = Cursors.Hand
    };

    private void RefreshView()
    {
        var date = _datePicker.Value.Date;
        var attendance = DataStore.AttendanceRecords.Where(a => a.AttendanceDate.Date == date && !a.IsDeleted).OrderByDescending(a => a.CheckInTime).ToList();
        var monthStart = new DateTime(date.Year, date.Month, 1);
        var monthAttendance = DataStore.AttendanceRecords.Count(a => a.AttendanceDate >= monthStart && a.AttendanceDate < monthStart.AddMonths(1) && !a.IsDeleted);
        _selectedSummary.Text = $"👥 {attendance.Count} members checked in on this date";
        _monthSummary.Text = $"📈 This month: {monthAttendance} attendances";
        _attendanceGrid.DataSource = null;
        _attendanceGrid.DataSource = attendance;
        RefreshMembers();
    }

    private void RefreshMembers()
    {
        var date = _datePicker.Value.Date;
        var search = _searchBox.Text.Trim();
        var attendedIds = DataStore.AttendanceRecords
            .Where(a => a.AttendanceDate.Date == date && !a.IsDeleted)
            .Select(a => a.MemberId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var members = DataStore.Members
            .Where(m => !m.IsDeleted && m.StartDate.Date <= date && m.ExpiryDate.Date >= date && string.Equals(m.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Where(m => !attendedIds.Contains(m.Id))
            .Where(m => string.IsNullOrWhiteSpace(search) || m.Id.Contains(search, StringComparison.OrdinalIgnoreCase) || GetMemberName(m).Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => GetMemberName(m))
            .ToList();

        _membersGrid.DataSource = null;
        _membersGrid.DataSource = members.Select(m => new MemberSearchResult(m)).ToList();
        UpdateSelectedMember();
    }

    private void UpdateSelectedMember()
    {
        _selectedMember.Text = SelectedMember is { } member ? $"Selected: {GetMemberName(member)}" : "No member selected";
    }

    private void FastCheckIn()
    {
        var query = _searchBox.Text.Trim();
        Member? target = null;

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Exact ID match first
            target = DataStore.Members.FirstOrDefault(m => !m.IsDeleted && m.Id.Equals(query, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                // Name match
                target = DataStore.Members.FirstOrDefault(m => !m.IsDeleted && GetMemberName(m).Contains(query, StringComparison.OrdinalIgnoreCase));
            }
        }

        target ??= SelectedMember;

        if (target is null)
        {
            ShowFeedback("⚠️ Member not found. Please enter a valid Member ID.", Color.Crimson);
            return;
        }

        RecordCheckIn(target);
        _searchBox.Clear();
        _searchBox.Focus();
    }

    private void AddAttendance()
    {
        var member = SelectedMember;
        if (member is null) 
        { 
            MessageBox.Show("Please select a member from the list first.", "Attendance", MessageBoxButtons.OK, MessageBoxIcon.Information); 
            return; 
        }

        RecordCheckIn(member);
    }

    private void RecordCheckIn(Member member)
    {
        var date = _datePicker.Value.Date;
        if (DataStore.AttendanceRecords.Any(a => !a.IsDeleted && a.MemberId == member.Id && a.AttendanceDate.Date == date))
        {
            ShowFeedback($"⚠️ {GetMemberName(member)} is already checked in for today.", Color.DarkOrange);
            return;
        }

        if (DatabaseConnection.TestConnection())
        {
            var result = AttendanceService.Record(
                new AttendanceSubmission(member.FirstName, member.MiddleName, member.LastName, member.Id),
                _username);

            if (!result.Success)
            {
                ShowFeedback($"❌ Check-in failed: {result.Message}", Color.Crimson);
                RefreshView();
                return;
            }

            DataStore.Reload();
            RefreshView();
            ShowFeedback($"✅ Checked In: {GetMemberName(member)} ({DateTime.Now:h:mm tt})", Color.FromArgb(43, 157, 105));
            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
            return;
        }

        // Offline fallback
        DataStore.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = DataStore.NewAttendanceId(),
            MemberId = member.Id,
            MemberName = GetMemberName(member),
            AttendanceDate = date,
            CheckInTime = DateTime.Now,
            RecordedBy = _username
        });
        DataStore.SaveAttendanceRecords();
        RefreshView();
        ShowFeedback($"✅ Checked In (Offline): {GetMemberName(member)} ({DateTime.Now:h:mm tt})", Color.FromArgb(43, 157, 105));
        try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
    }

    private void ShowFeedback(string message, Color color)
    {
        _feedbackBanner.Text = message;
        _feedbackBanner.ForeColor = color;
        _feedbackBanner.Visible = true;
    }

    private static string GetMemberName(Member member) => !string.IsNullOrWhiteSpace(member.FullName)
        ? member.FullName
        : string.Join(" ", new[] { member.FirstName, member.MiddleName, member.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private sealed class MemberSearchResult
    {
        public Member Member { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Plan { get; }

        public MemberSearchResult(Member member)
        {
            Member = member;
            Id = member.Id;
            DisplayName = GetMemberName(member);
            Plan = member.Plan;
        }
    }
}

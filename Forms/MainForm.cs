using System.Globalization;
using System.Text;
using GymRegistrationApp.Models;
using GymRegistrationApp.Services;

namespace GymRegistrationApp.Forms;

public partial class MainForm : Form
{
    // indicates whether the main form was closed due to a sign-out action
    public bool IsSignOut { get; private set; }

    private readonly string _username;
    private readonly string _role;
    private readonly StaffPermissions _permissions;
    private string _currentPage = "Dashboard";
    private string _membersView = "All Members";
    private readonly CultureInfo _pesoCulture = new("fil-PH");
    private DateTimePicker? dtpIncomeReportDate;
    private ComboBox? cmbIncomeReportMonth;
    private ComboBox? cmbIncomeReportYear;
    private DataGridView? gridIncomeReportTransactions;
    private Panel? pnlIncomeRevenueChart;
    private Label? lblIncomeReportSelection;
    private Label? lblIncomeReportDaySummary;
    private Label? lblIncomeReportMonthSummary;
    private Label? lblIncomeReportYearSummary;
    private Label? lblIncomeTodayDetail;
    private Label? lblIncomeMonthlyDetail;
    private Label? lblIncomeRecordedDetail;
    private Label? lblIncomeChartSubtitle;
    private Panel? _dashboardHost;
    private readonly List<Panel> _dashboardPanels = new();
    private DateTime _dashboardSelectedDate = DateTime.Today;
    private DateTimePicker? _dashboardDatePicker;
    private bool _archiveEventsInitialized;
    private string _sessionEndReason = "Session timeout / application closed";
    private readonly System.Windows.Forms.Timer _databaseRefreshTimer = new() { Interval = 30000 };

    public MainForm(string username, string role, StaffPermissions? permissions = null) 
    { 
        _username = username; 
        _role = role; 
        _permissions = permissions ?? StaffPermissions.CreateDefault();
        InitializeComponent(); 
        FormClosed += MainForm_FormClosed;
        InitializeIncomeReportsPage();
        InitializeDeletedRecordsPage();
        BuildOverviewDashboard();
        lblUser.Text = username + "  ·  " + role; 
        btnMembers.Enabled = HasPermission(p => p.CanViewMembers);
        btnPayments.Enabled = HasPermission(p => p.CanManagePayments);
        btnDailyGym.Enabled = HasPermission(p => p.CanManageDailyGym);
        btnQRCode.Enabled = HasPermission(p => p.CanManageDailyGym);
        btnReports.Enabled = HasPermission(p => p.CanViewReports);
        btnExpenses.Enabled = HasPermission(p => p.CanManageExpenses);
        btnIncomeReports.Enabled = HasPermission(p => p.CanViewReports);
        btnStaffUsers.Enabled = HasPermission(p => p.CanManageStaff);
        btnDeletedRecords.Enabled = HasPermission(p => p.CanViewDeletedRecords);

        // Test and initialize database connection
        TestAndInitializeDatabase();

        ShowPage("Dashboard"); 
        RefreshData(); 
        _ = TriggerCloudSyncSilentAsync();
        _databaseRefreshTimer.Tick += (_, _) =>
        {
            DataStore.Reload();
            RefreshData();
            _ = TriggerCloudSyncSilentAsync();
        };
        _databaseRefreshTimer.Start();
    }
    private void btnActivityHistory_Click(object? s, EventArgs e)
    {
        if (!HasPermission(p => p.CanManageStaff)) return;
        using var dialog = new StaffActivityHistoryForm();
        dialog.ShowDialog(this);
    }

    private bool HasPermission(Func<StaffPermissions, bool> permission) =>
        string.Equals(_role, "Owner", StringComparison.OrdinalIgnoreCase) || permission(_permissions);

    private void TestAndInitializeDatabase()
    {
        try
        {
            bool isConnected = DatabaseConnection.TestConnection();
            if (isConnected)
            {
                // Database is connected; ready for operations
            }
            else
            {
                // Log warning but continue with local data
                Console.WriteLine("Warning: Database connection failed. Using local data storage.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
        }
        UpdateDatabaseStatus();
    }

    private void UpdateDatabaseStatus()
    {
        if (lblDbStatus == null) return;
        try
        {
            bool isConnected = DatabaseConnection.TestConnection();
            if (isConnected)
            {
                lblDbStatus.Text = "🟢 MySQL Online";
                lblDbStatus.ForeColor = Color.FromArgb(39, 174, 96);
                lblDbStatus.BackColor = Color.FromArgb(235, 247, 238);
            }
            else
            {
                lblDbStatus.Text = "🟠 Offline Local Storage";
                lblDbStatus.ForeColor = Color.FromArgb(211, 84, 0);
                lblDbStatus.BackColor = Color.FromArgb(254, 243, 235);
            }
        }
        catch
        {
            lblDbStatus.Text = "🟠 Offline Local Storage";
            lblDbStatus.ForeColor = Color.FromArgb(211, 84, 0);
            lblDbStatus.BackColor = Color.FromArgb(254, 243, 235);
        }
    }

    private async Task TriggerCloudSyncAsync()
    {
        btnSyncCloud.Enabled = false;
        btnSyncCloud.Text = "⏳ Syncing...";
        try
        {
            var settingsUrl = QRCodeManagementForm.LoadSavedAttendanceUrl();
            if (string.IsNullOrWhiteSpace(settingsUrl) || !settingsUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Please configure your Cloud Attendance URL in the QR Code Management screen first.\n\n" +
                    "Example: https://universalgym-attendance.vercel.app/attendance",
                    "Configure Cloud URL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = await CloudAttendanceSyncService.SyncFromCloudAsync(settingsUrl);
            if (result.Success)
            {
                if (result.SyncedCount > 0)
                {
                    RefreshData();
                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                    MessageBox.Show(result.Message, "Cloud Sync Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("All attendance records are already up to date with the Cloud.", "Cloud Sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show($"Could not sync with Cloud:\n{result.Message}", "Cloud Sync Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sync error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSyncCloud.Enabled = true;
            btnSyncCloud.Text = "☁️ Sync Cloud";
        }
    }

    private async Task TriggerCloudSyncSilentAsync()
    {
        try
        {
            var settingsUrl = QRCodeManagementForm.LoadSavedAttendanceUrl();
            if (string.IsNullOrWhiteSpace(settingsUrl) || !settingsUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;

            var result = await CloudAttendanceSyncService.SyncFromCloudAsync(settingsUrl);
            if (result.Success && result.SyncedCount > 0)
            {
                Invoke(() =>
                {
                    RefreshData();
                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                });
            }
        }
        catch { }
    }
    private void ShowPage(string page)
    {
        var allowed = page switch
        {
            "Members" or "AllMembers" => HasPermission(p => p.CanViewMembers),
            "Payments" => HasPermission(p => p.CanManagePayments),
            "DailyGym" => HasPermission(p => p.CanManageDailyGym),
            "IncomeReports" or "Reports" => HasPermission(p => p.CanViewReports),
            "StaffUsers" => HasPermission(p => p.CanManageStaff),
            "DeletedRecords" => HasPermission(p => p.CanViewDeletedRecords),
            _ => true
        };
        if (!allowed)
        {
            MessageBox.Show("Your account does not have permission to access this page.", "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            page = "Dashboard";
        }

        _currentPage = page;
        lblPageTitle.Text = page switch
        {
            "Members" or "AllMembers" => "All Members",
            "IncomeReports" => "Income Reports",
            "DeletedRecords" => "Archive",
            "StaffUsers" => "Users",
            _ => page
        };
        // set visibility for all pages
        pageDashboard.Visible = page == "Dashboard";
        pageMembers.Visible = page == "Members" || page == "AllMembers";
        pagePayments.Visible = page == "Payments";
        pageReports.Visible = page == "Reports";
        // new feature pages
        pageActiveMemberships.Visible = page == "ActiveMemberships";
        pageExpiredMemberships.Visible = page == "ExpiredMemberships";
        pageRenewalHistory.Visible = page == "RenewalHistory";
        pagePaymentHistory.Visible = page == "PaymentHistory";
        pageUnpaidBalances.Visible = false;
        if (page == "DailyGym") { OpenDailyGym(); return; }
        pageIncomeReports.Visible = page == "IncomeReports";
        pageStaffUsers.Visible = page == "StaffUsers";
        pageDeletedRecords.Visible = page == "DeletedRecords";

        if (page == "Members" || page == "AllMembers")
        {
            ApplyMembersView(_membersView);
        }
        else
        {
            gridMembers.Visible = false;
            gridActiveMemberships.Visible = false;
            gridExpiredMemberships.Visible = false;
            gridRenewalHistory.Visible = false;
            gridPaymentHistory.Visible = false;
        }

        // reset all nav buttons
        foreach (var b in new[] { btnDashboard, btnMembers, btnDailyGym, btnIncomeReports, btnStaffUsers, btnDeletedRecords, btnPayments, btnExpenses, btnReports })
        {
            b.BackColor = Color.FromArgb(18, 23, 38);
            b.ForeColor = Color.FromArgb(210, 215, 226);
        }

        // highlight active button
        var active = page switch
        {
            "Dashboard" => btnDashboard,
            "Members" or "AllMembers" => btnMembers,
            "DailyGym" => btnDailyGym,
            "IncomeReports" => btnIncomeReports,
            "StaffUsers" => btnStaffUsers,
            "DeletedRecords" => btnDeletedRecords,
            "Payments" => btnPayments,
            "Expenses" => btnExpenses,
            "Reports" => btnReports,
            _ => btnDashboard,
        };
        active.BackColor = Color.FromArgb(255, 132, 35); active.ForeColor = Color.White;

        RefreshData();
    }

    private void ApplyMembersView(string view)
    {
        _membersView = view;
        if (cmbMembersView != null && cmbMembersView.Items.Count > 0 && cmbMembersView.SelectedItem?.ToString() != view)
        {
            cmbMembersView.SelectedItem = view;
        }

        var isAll = string.Equals(view, "All Members", StringComparison.OrdinalIgnoreCase);
        var isActive = string.Equals(view, "Active Members", StringComparison.OrdinalIgnoreCase);
        var isExpired = string.Equals(view, "Expired Members", StringComparison.OrdinalIgnoreCase);
        var isRenewal = string.Equals(view, "Renewal History", StringComparison.OrdinalIgnoreCase);

        btnAddMember.Visible = isAll && HasPermission(p => p.CanAddMembers);
        btnEditMember.Visible = isAll && HasPermission(p => p.CanEditMembers);
        btnDeleteMember.Visible = isAll && HasPermission(p => p.CanDeleteMembers);
        btnExportMembers.Visible = isAll;

        gridMembers.Visible = isAll;
        gridActiveMemberships.Visible = isActive;
        gridExpiredMemberships.Visible = isExpired;
        gridRenewalHistory.Visible = isRenewal;
        gridPaymentHistory.Visible = false;

        if (isAll) { LoadMembersGrid(); }
        else if (isActive) { LoadActiveMembershipsGrid(); }
        else if (isExpired) { LoadExpiredMembershipsGrid(); }
        else if (isRenewal) { LoadRenewalHistoryGrid(); }
    }

    private void cmbMembersView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbMembersView?.SelectedItem is null) return;
        ApplyMembersView(cmbMembersView.SelectedItem.ToString() ?? "All Members");
    }
    private void RefreshData()
    {
        var statusesChanged = false;
        foreach (var member in DataStore.Members.Where(m => m.ExpiryDate.Date < DateTime.Today && !string.Equals(m.Status, "Expired", StringComparison.OrdinalIgnoreCase)))
        {
            member.Status = "Expired";
            statusesChanged = true;
        }
        if (statusesChanged) DataStore.SaveMembers();

        DataStore.RecalculateMemberBalances();
        var active = DataStore.Members.Count(m => !m.IsDeleted && m.ExpiryDate.Date >= DateTime.Today && m.Status == "Active");
        var revenue = DataStore.ActivePayments.Where(p => p.Status == "Paid").Sum(p => p.Amount);
        lblMemberCount.Text = "TOTAL MEMBERS\n" + DataStore.Members.Count(m => !m.IsDeleted);
        lblActiveCount.Text = "ACTIVE MEMBERS\n" + active;
        lblRevenue.Text = "TOTAL REVENUE\n" + FormatPeso(revenue);
        lblPaymentCount.Text = "PAYMENT RECORDS\n" + DataStore.ActivePayments.Count();
        lblReportMembers.Text = DataStore.Members.Count.ToString(); lblReportActive.Text = active.ToString(); lblReportRevenue.Text = FormatPeso(revenue);
        if (_dashboardHost != null)
        {
            foreach (var panel in _dashboardPanels) panel.Invalidate();
            _dashboardHost.Invalidate(true);
        }
        LoadMembersGrid(); LoadPaymentsGrid(); LoadActiveMembershipsGrid(); LoadExpiredMembershipsGrid(); LoadRenewalHistoryGrid(); LoadPaymentHistoryGrid(); LoadIncomeReportsData(); LoadStaffUsersData(); LoadDeletedRecordsData();
        UpdateDatabaseStatus();
    }

    private void BuildOverviewDashboard()
    {
        pageDashboard.Controls.Clear();
        pageDashboard.AutoScroll = true;
        _dashboardHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = pageDashboard.BackColor,
            AutoScroll = true,
            Padding = new Padding(20, 20, 20, 24)
        };
        pageDashboard.Controls.Add(_dashboardHost);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = pageDashboard.BackColor,
            MinimumSize = new Size(760, 0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 278));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 278));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 310));
        _dashboardHost.Controls.Add(root);
        _dashboardHost.Resize += (_, _) =>
        {
            root.Width = Math.Max(root.MinimumSize.Width, _dashboardHost.ClientSize.Width - _dashboardHost.Padding.Horizontal);
        };
        root.Width = Math.Max(root.MinimumSize.Width, _dashboardHost.ClientSize.Width - _dashboardHost.Padding.Horizontal);

        var heading = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4, 0, 4, 10),
            BackColor = pageDashboard.BackColor
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var headingText = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 2, 0, 0),
            BackColor = pageDashboard.BackColor,
            Margin = new Padding(0)
        };
        headingText.Controls.Add(new Label
        {
            Text = "Overview dashboard",
            AutoSize = true,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 36, 52),
            Margin = new Padding(0, 0, 0, 1)
        });
        heading.Controls.Add(headingText, 0, 0);
        var filterPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12),
            Margin = new Padding(0)
        };
        var dateNavigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 0),
            BackColor = pageDashboard.BackColor
        };
        var previous = DashboardDateButton("Previous");
        previous.Click += (_, _) => SetDashboardDate(_dashboardSelectedDate.AddDays(-1));
        var today = DashboardDateButton("Today");
        today.Click += (_, _) => SetDashboardDate(DateTime.Today);
        var next = DashboardDateButton("Next");
        next.Click += (_, _) => SetDashboardDate(_dashboardSelectedDate.AddDays(1));
        _dashboardDatePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dddd, MMMM d, yyyy",
            Value = _dashboardSelectedDate,
            Width = 300,
            Height = 34,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(8, 0, 8, 0),
            TabStop = true,
            Enabled = true
        };
        _dashboardDatePicker.ValueChanged += (_, _) => SetDashboardDate(_dashboardDatePicker.Value.Date);
        dateNavigation.Controls.AddRange(new Control[] { previous, _dashboardDatePicker, today, next });
        filterPanel.Controls.Add(dateNavigation);
        heading.Controls.Add(filterPanel, 0, 1);
        root.Controls.Add(heading, 0, 0);

        var summaryCards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = pageDashboard.BackColor,
            Padding = new Padding(0, 0, 0, 16),
            Margin = new Padding(0)
        };
        summaryCards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        summaryCards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var canViewOverviewMemberships = HasPermission(p => p.CanViewOverviewMemberships);
        var canViewOverviewDailyGym = HasPermission(p => p.CanViewOverviewDailyGym);
        var canViewOverviewPayments = HasPermission(p => p.CanViewOverviewPayments);
        var canViewOverviewRevenue = HasPermission(p => p.CanViewOverviewRevenue);

        var firstCards = new List<(string Title, Color Accent, Action Click, Action<Graphics, Rectangle, string> Draw)>();
        if (canViewOverviewMemberships)
        {
            firstCards.Add(("TOTAL REGISTERED MEMBERS", Color.FromArgb(255, 132, 35), () => ShowPage("Members"), DrawTotalCard));
            firstCards.Add(("ACTIVE MEMBERSHIPS", Color.FromArgb(43, 157, 105), () => { ApplyMembersView("Active Members"); ShowPage("Members"); }, DrawActiveCard));
            firstCards.Add(("EXPIRED MEMBERSHIPS", Color.FromArgb(205, 79, 70), () => { ApplyMembersView("Expired Members"); ShowPage("Members"); }, DrawExpiredCard));
        }
        if (canViewOverviewDailyGym)
            firstCards.Add(("DAILY VISITORS", Color.FromArgb(213, 139, 43), () => OpenDailyGym(_dashboardSelectedDate), DrawDailyVisitorsCard));

        var firstCardRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = Math.Max(1, firstCards.Count),
            RowCount = 1,
            BackColor = pageDashboard.BackColor,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0)
        };
        for (var i = 0; i < firstCardRow.ColumnCount; i++) firstCardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / firstCardRow.ColumnCount));
        for (var i = 0; i < firstCards.Count; i++)
        {
            var card = firstCards[i];
            AddDashboardCard(firstCardRow, i, card.Title, card.Accent, card.Click, card.Draw);
        }
        summaryCards.Controls.Add(firstCardRow, 0, 0);

        var secondCards = new List<(string Title, Color Accent, Action Click, Action<Graphics, Rectangle, string> Draw)>();
        if (canViewOverviewRevenue)
        {
            secondCards.Add(("WALK-IN DAILY INCOME", Color.FromArgb(43, 157, 105), () => OpenDailyGym(_dashboardSelectedDate), DrawWalkInRevenueCard));
            secondCards.Add(("RECORDED REVENUE", Color.FromArgb(81, 102, 181), () => ShowPage("IncomeReports"), DrawRevenueCard));
            secondCards.Add(("SELECTED DATE INCOME", Color.FromArgb(119, 87, 181), () => ShowPage("IncomeReports"), DrawSelectedDateIncomeCard));
        }
        if (canViewOverviewPayments)
            secondCards.Add(("PAYMENT RECORDS", Color.FromArgb(81, 102, 181), () => ShowPage("Payments"), DrawPaymentCard));
        if (HasPermission(p => p.CanManageExpenses))
        {
            secondCards.Add(("TOTAL EXPENSES", Color.FromArgb(205, 79, 70), () => OpenExpenses(), DrawTotalExpensesCard));
            secondCards.Add(("MONTHLY EXPENSES", Color.FromArgb(213, 139, 43), () => OpenExpenses(), DrawMonthlyExpensesCard));
            secondCards.Add(("NET INCOME", Color.FromArgb(43, 157, 105), () => OpenReports(), DrawNetIncomeCard));
        }

        var secondCardRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = Math.Max(1, secondCards.Count),
            RowCount = 1,
            BackColor = pageDashboard.BackColor,
            Margin = new Padding(0, 8, 0, 0),
            Padding = new Padding(0)
        };
        for (var i = 0; i < secondCardRow.ColumnCount; i++) secondCardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / secondCardRow.ColumnCount));
        for (var i = 0; i < secondCards.Count; i++)
        {
            var card = secondCards[i];
            AddDashboardCard(secondCardRow, i, card.Title, card.Accent, card.Click, card.Draw);
        }
        summaryCards.Controls.Add(secondCardRow, 0, 1);
        root.Controls.Add(summaryCards, 0, 1);

        var rowOne = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = pageDashboard.BackColor, Padding = new Padding(0, 0, 0, 18), Margin = new Padding(0) };
        rowOne.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); rowOne.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        if (canViewOverviewMemberships)
        {
            AddChart(rowOne, 0, "Registration Activity", "New members registered in the last six months", DrawRegistrations, null);
            AddChart(rowOne, 1, "Membership Status", "Actual active and expired memberships", DrawStatus, () => { ApplyMembersView("Active Members"); ShowPage("Members"); });
        }
        root.Controls.Add(rowOne, 0, 2);

        var rowTwo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = pageDashboard.BackColor, Padding = new Padding(0, 0, 0, 18), Margin = new Padding(0) };
        rowTwo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); rowTwo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        if (canViewOverviewMemberships)
        {
            AddChart(rowTwo, 0, "Plan Distribution", "Members by current membership plan", DrawPlans, () => ShowPage("Members"));
            AddChart(rowTwo, 1, "Membership Expiration Outlook", "Upcoming expirations by month", DrawExpirations, () => { ApplyMembersView("Expired Members"); ShowPage("Members"); });
        }
        root.Controls.Add(rowTwo, 0, 3);

        var finalRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = pageDashboard.BackColor, Padding = new Padding(0, 0, 0, 18), Margin = new Padding(0) };
        finalRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); finalRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var dailyChart = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = pageDashboard.BackColor };
        if (canViewOverviewDailyGym)
        {
            AddChart(dailyChart, 0, "Daily Gym Activity", "Walk-in visits for the last seven days", DrawDailyActivity, () => OpenDailyGym());
            finalRow.Controls.Add(dailyChart, 0, 0);
        }
        var revenueChart = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = pageDashboard.BackColor };
        if (canViewOverviewRevenue)
        {
            AddChart(revenueChart, 0, "Revenue Trend", "Recorded payments by month", DrawRevenueTrend, () => ShowPage("IncomeReports"));
            finalRow.Controls.Add(revenueChart, 1, 0);
        }
        root.Controls.Add(finalRow, 0, 4);
    }

    private void DrawDailyActivity(Graphics g, Rectangle r)
    {
        var first = DateTime.Today.AddDays(-6);
        var values = Enumerable.Range(0, 7).Select(i => DataStore.DailyGymVisits.Count(v => !v.IsDeleted && v.VisitDate.Date == first.AddDays(i).Date)).ToArray();
        DrawBars(g, r, values, i => first.AddDays(i).ToString("ddd"), Color.FromArgb(213, 139, 43));
    }
    private void AddDashboardCard(TableLayoutPanel parent, int column, string title, Color accent, Action click, Action<Graphics, Rectangle, string> draw)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, Tag = title };
        panel.Paint += (_, e) => draw(e.Graphics, panel.ClientRectangle, title);
        panel.Click += (_, _) => click(); parent.Controls.Add(panel, column % parent.ColumnCount, column / parent.ColumnCount); _dashboardPanels.Add(panel);
    }

    private void AddChart(TableLayoutPanel parent, int column, string title, string subtitle, Action<Graphics, Rectangle> draw, Action? click)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Cursor = click == null ? Cursors.Default : Cursors.Hand };
        panel.Paint += (_, e) => { DrawChartFrame(e.Graphics, panel.ClientRectangle, title, subtitle); draw(e.Graphics, new Rectangle(18, 68, Math.Max(1, panel.Width - 36), Math.Max(1, panel.Height - 84))); };
        if (click != null)
        {
            panel.MouseClick += (_, e) =>
            {
                if (string.Equals(title, "Membership Status", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyMembersView(e.Y > 60 ? "Expired Members" : "Active Members"); ShowPage("Members");
                }
                else click();
            };
        }
        parent.Controls.Add(panel, column, 0); _dashboardPanels.Add(panel);
    }

    private void DrawChartFrame(Graphics g, Rectangle r, string title, string subtitle)
    {
        using var titleBrush = new SolidBrush(Color.FromArgb(29, 36, 52)); using var subBrush = new SolidBrush(Color.FromArgb(130, 136, 147));
        g.DrawString(title, new Font("Segoe UI", 12, FontStyle.Bold), titleBrush, 14, 12); g.DrawString(subtitle, new Font("Segoe UI", 8.5f), subBrush, 14, 34);
    }

    private void DrawTotalCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, DataStore.Members.Count(m => !m.IsDeleted).ToString("N0"), Color.FromArgb(255, 132, 35));
    private void DrawActiveCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, ActiveMembers().Count().ToString("N0"), Color.FromArgb(43, 157, 105));
    private void DrawExpiredCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, ExpiredMembers().Count().ToString("N0"), Color.FromArgb(205, 79, 70));
    private void DrawDailyVisitorsCard(Graphics g, Rectangle r, string t) => DrawSelectedDateCard(g, r, t, DataStore.DailyGymVisits.Count(v => v.VisitDate.Date == _dashboardSelectedDate && !v.IsDeleted).ToString("N0"), Color.FromArgb(213, 139, 43));
    private void DrawWalkInRevenueCard(Graphics g, Rectangle r, string t) => DrawSelectedDateCard(g, r, t, FormatPeso(SelectedDateDailyGymPayments().Sum(p => p.Amount)), Color.FromArgb(43, 157, 105));
    private void DrawRevenueCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, FormatPeso(DataStore.ActivePayments.Where(p => p.Status == "Paid").Sum(p => p.Amount)), Color.FromArgb(81, 102, 181));
    private void DrawSelectedDateIncomeCard(Graphics g, Rectangle r, string t) => DrawSelectedDateCard(g, r, t, FormatPeso(SelectedDateIncomePayments().Sum(p => p.Amount)), Color.FromArgb(119, 87, 181));
    private void DrawPaymentCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, DataStore.ActivePayments.Count().ToString("N0"), Color.FromArgb(81, 102, 181));
    private void DrawTotalExpensesCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, FormatPeso(DataStore.ActiveExpenses.Sum(e => e.Amount)), Color.FromArgb(205, 79, 70));
    private void DrawMonthlyExpensesCard(Graphics g, Rectangle r, string t)
    {
        var monthStart = new DateTime(_dashboardSelectedDate.Year, _dashboardSelectedDate.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var monthlyExpenses = DataStore.ActiveExpenses
            .Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate < nextMonthStart)
            .Sum(e => e.Amount);

        DrawCard(g, r, t, FormatPeso(monthlyExpenses), Color.FromArgb(213, 139, 43));
    }
    private void DrawNetIncomeCard(Graphics g, Rectangle r, string t) => DrawCard(g, r, t, FormatPeso(DataStore.ActivePayments.Where(p => p.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount) - DataStore.ActiveExpenses.Sum(e => e.Amount)), Color.FromArgb(43, 157, 105));
    private void DrawDailySummary(Graphics g, Rectangle r)
    { using var brush = new SolidBrush(Color.FromArgb(29, 36, 52)); g.DrawString($"Daily Gym today: {DataStore.DailyGymVisits.Count(v => v.VisitDate.Date == DateTime.Today && !v.IsDeleted):N0} visitors  •  WALK-IN DAILY INCOME {FormatPeso(DataStore.DailyGymVisits.Where(v => v.VisitDate.Date == DateTime.Today && !v.IsDeleted).Sum(v => v.Fee))}", new Font("Segoe UI", 10, FontStyle.Bold), brush, r.Left, r.Top + 14); }
    private IEnumerable<Payment> SelectedDatePayments() => DataStore.ActivePayments.Where(p => p.Status == "Paid" && p.Date.Date == _dashboardSelectedDate);
    private IEnumerable<Payment> SelectedDateDailyGymPayments() => SelectedDatePayments().Where(p => string.Equals(p.PaymentType, "Daily Gym Pass", StringComparison.OrdinalIgnoreCase));
    private IEnumerable<Payment> SelectedDateIncomePayments() => SelectedDatePayments().Where(p => string.Equals(p.PaymentType, "Membership Payment", StringComparison.OrdinalIgnoreCase) || string.Equals(p.PaymentType, "Membership Renewal", StringComparison.OrdinalIgnoreCase) || string.Equals(p.PaymentType, "Daily Gym Pass", StringComparison.OrdinalIgnoreCase));
    private void SetDashboardDate(DateTime date)
    {
        _dashboardSelectedDate = date.Date;
        if (_dashboardDatePicker != null && _dashboardDatePicker.Value.Date != _dashboardSelectedDate) _dashboardDatePicker.Value = _dashboardSelectedDate;
        if (_dashboardHost != null) { foreach (var panel in _dashboardPanels) panel.Invalidate(); _dashboardHost.Invalidate(true); }
    }
    private static Button DashboardDateButton(string text) => new() { Text = text, AutoSize = false, Width = 90, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(255, 132, 35), ForeColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
    private void OpenDailyGym(DateTime? date = null)
    {
        if (!HasPermission(p => p.CanManageDailyGym)) return;
        using var form = new DailyGymForm(_username, date ?? _dashboardSelectedDate);
        form.ShowDialog(this);
        RefreshData();
    }
    private void DrawCard(Graphics g, Rectangle r, string title, string value, Color accent)
    { using var bg = new SolidBrush(Color.White); g.FillRectangle(bg, r); using var bar = new SolidBrush(accent); g.FillRectangle(bar, 0, 0, 5, r.Height); using var tb = new SolidBrush(Color.FromArgb(105, 111, 125)); using var vb = new SolidBrush(Color.FromArgb(29, 36, 52)); g.DrawString(title, new Font("Segoe UI", 8.5f, FontStyle.Bold), tb, 16, 15); g.DrawString(value, new Font("Segoe UI", 16, FontStyle.Bold), vb, 16, 44); }
    private void DrawSelectedDateCard(Graphics g, Rectangle r, string title, string value, Color accent)
    { using var bg = new SolidBrush(Color.White); g.FillRectangle(bg, r); using var bar = new SolidBrush(accent); g.FillRectangle(bar, 0, 0, 5, r.Height); using var tb = new SolidBrush(Color.FromArgb(105, 111, 125)); using var vb = new SolidBrush(Color.FromArgb(29, 36, 52)); g.DrawString(title, new Font("Segoe UI", 8.5f, FontStyle.Bold), tb, 16, 9); g.DrawString(_dashboardSelectedDate.ToString("MMM d, yyyy"), new Font("Segoe UI", 8.5f), tb, 16, 29); g.DrawString(value, new Font("Segoe UI", 16, FontStyle.Bold), vb, 16, 54); }
    private IEnumerable<Member> ActiveMembers() => DataStore.Members.Where(m => !m.IsDeleted && m.ExpiryDate.Date >= DateTime.Today && !string.Equals(m.Status, "Expired", StringComparison.OrdinalIgnoreCase));
    private IEnumerable<Member> ExpiredMembers() => DataStore.Members.Where(m => !m.IsDeleted && (m.ExpiryDate.Date < DateTime.Today || string.Equals(m.Status, "Expired", StringComparison.OrdinalIgnoreCase)));

    private void DrawRegistrations(Graphics g, Rectangle r)
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5); var values = Enumerable.Range(0, 6).Select(i => DataStore.Members.Count(m => m.StartDate.Year == first.AddMonths(i).Year && m.StartDate.Month == first.AddMonths(i).Month)).ToArray();
        DrawBars(g, r, values, i => first.AddMonths(i).ToString("MMM"), Color.FromArgb(255, 132, 35));
    }

    private void DrawStatus(Graphics g, Rectangle r)
    {
        var active = ActiveMembers().Count(); var expired = ExpiredMembers().Count(); var total = active + expired;
        var center = new Point(r.Left + 75, r.Top + r.Height / 2); var size = Math.Min(110, r.Height - 20); var box = new Rectangle(center.X - size / 2, center.Y - size / 2, size, size);
        using var track = new Pen(Color.FromArgb(232, 235, 240), 18); g.DrawArc(track, box, 0, 360); if (total > 0) { using var green = new Pen(Color.FromArgb(43, 157, 105), 18); g.DrawArc(green, box, -90, 360f * active / total); using var red = new Pen(Color.FromArgb(205, 79, 70), 18); g.DrawArc(red, box, -90 + 360f * active / total, 360f * expired / total); }
        using var brush = new SolidBrush(Color.FromArgb(29, 36, 52)); g.DrawString($"{total:N0}\nTotal", new Font("Segoe UI", 10, FontStyle.Bold), brush, center.X - 24, center.Y - 16);
        DrawLegend(g, r.Left + 160, r.Top + 25, "Active", active, total, Color.FromArgb(43, 157, 105)); DrawLegend(g, r.Left + 160, r.Top + 75, "Expired", expired, total, Color.FromArgb(205, 79, 70));
    }

    private void DrawPlans(Graphics g, Rectangle r)
    {
        var names = new[] { "Monthly", "2-Month Plan" }; var values = names.Select(n => DataStore.Members.Count(m => string.Equals(m.Plan, n, StringComparison.OrdinalIgnoreCase) || (n == "2-Month Plan" && string.Equals(m.Plan, "2M Plan", StringComparison.OrdinalIgnoreCase)))).ToArray(); var total = values.Sum(); DrawBars(g, r, values, i => $"{names[i]} ({(total == 0 ? 0 : values[i] * 100.0 / total):0.0}%)", Color.FromArgb(81, 102, 181));
    }

    private void DrawExpirations(Graphics g, Rectangle r)
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); var values = Enumerable.Range(0, 6).Select(i => DataStore.Members.Count(m => m.ExpiryDate.Date >= first.AddMonths(i) && m.ExpiryDate.Date < first.AddMonths(i + 1))).ToArray(); DrawBars(g, r, values, i => first.AddMonths(i).ToString("MMM"), Color.FromArgb(213, 139, 43));
    }

    private void DrawRevenueTrend(Graphics g, Rectangle r)
    {
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5); var values = Enumerable.Range(0, 6).Select(i => DataStore.ActivePayments.Where(p => p.Status == "Paid" && p.Date >= first.AddMonths(i) && p.Date < first.AddMonths(i + 1)).Sum(p => p.Amount)).ToArray();
        DrawBars(g, r, values, i => first.AddMonths(i).ToString("MMM"), Color.FromArgb(81, 102, 181), true);
    }

    private void DrawBars(Graphics g, Rectangle r, decimal[] values, Func<int, string> label, Color color, bool currency = false)
    { DrawBars(g, r, values.Select(v => (double)v).ToArray(), label, color, currency); }
    private void DrawBars(Graphics g, Rectangle r, int[] values, Func<int, string> label, Color color) => DrawBars(g, r, values.Select(v => (double)v).ToArray(), label, color, false);
    private void DrawBars(Graphics g, Rectangle r, double[] values, Func<int, string> label, Color color, bool currency = false)
    {
        var max = values.DefaultIfEmpty(0).Max(); if (max <= 0) { DrawEmpty(g, r, "No records yet"); return; }
        var baseline = r.Bottom - 27;
        var plotTop = r.Top + (currency ? 30 : 18);
        using var axis = new Pen(Color.FromArgb(220, 224, 231)); g.DrawLine(axis, r.Left, baseline, r.Right, baseline);
        var width = Math.Max(12, r.Width / Math.Max(1, values.Length * 2));
        using var text = new SolidBrush(Color.FromArgb(70, 76, 91));
        using var valueOnBar = new SolidBrush(Color.White);
        using var labelFont = new Font("Segoe UI", 8); using var valueFont = new Font("Segoe UI", 8, FontStyle.Bold);
        for (var i = 0; i < values.Length; i++)
        {
            var x = r.Left + (i + 1) * r.Width / (values.Length + 1) - width / 2;
            var h = (int)((baseline - plotTop) * values[i] / max);
            using var b = new SolidBrush(color); g.FillRectangle(b, x, baseline - h, width, h);
            var monthText = label(i); var valueText = currency ? FormatPeso((decimal)values[i]) : values[i].ToString("N0");
            var monthSize = g.MeasureString(monthText, labelFont); var valueSize = g.MeasureString(valueText, valueFont);
            g.DrawString(monthText, labelFont, text, x + (width - monthSize.Width) / 2, baseline + 5);
            var valueX = x + (width - valueSize.Width) / 2;
            var valueInsideBar = !currency && h >= valueSize.Height + 8;
            var valueY = currency
                ? Math.Max(r.Top + 2, baseline - h - valueSize.Height - 4)
                : valueInsideBar
                    ? baseline - h + 4
                    : Math.Max(plotTop, baseline - h - valueSize.Height - 3);
            g.DrawString(valueText, valueFont, valueInsideBar ? valueOnBar : text, valueX, valueY);
        }
    }

    private void DrawLegend(Graphics g, int x, int y, string name, int count, int total, Color color)
    { using var b = new SolidBrush(color); g.FillRectangle(b, x, y + 3, 10, 10); using var t = new SolidBrush(Color.FromArgb(70, 76, 91)); var percent = total == 0 ? 0 : count * 100.0 / total; g.DrawString($"{name}  {count:N0} ({percent:0.0}%)", new Font("Segoe UI", 8.5f), t, x + 17, y); }
    private void DrawEmpty(Graphics g, Rectangle r, string text) { using var b = new SolidBrush(Color.FromArgb(140, 146, 158)); using var f = new Font("Segoe UI", 9, FontStyle.Italic); var s = g.MeasureString(text, f); g.DrawString(text, f, b, r.Left + (r.Width - s.Width) / 2, r.Top + (r.Height - s.Height) / 2); }
    private void DrawOutstanding(Graphics g, Rectangle r)
    { var count = DataStore.Members.Count(m => m.RemainingBalance > 0); var amount = DataStore.Members.Where(m => m.RemainingBalance > 0).Sum(m => m.RemainingBalance); using var t = new SolidBrush(Color.FromArgb(126, 78, 10)); g.DrawString($"Outstanding balances   {count:N0} members", new Font("Segoe UI", 9, FontStyle.Bold), t, r.Left, r.Top); g.DrawString(FormatPeso(amount) + " due  •  View unpaid balances →", new Font("Segoe UI", 9), t, r.Left, r.Top + 24); }
    private void LoadMembersGrid()
    {
        var q = txtMemberSearch.Text.Trim();
        var rows = DataStore.Members.Where(m => !m.IsDeleted && (string.IsNullOrWhiteSpace(q) || m.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) || m.Id.Contains(q, StringComparison.OrdinalIgnoreCase) || m.Phone.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(m => m.StartDate).Select(m => new { m.Id, m.FullName, m.Phone, m.Email, m.Plan, Fee = m.Fee.ToString("N2"), m.StartDate, m.ExpiryDate, Status = m.ExpiryDate.Date < DateTime.Today ? "Expired" : m.Status }).ToList();
        gridMembers.DataSource = null; gridMembers.DataSource = rows;
    }
    private void LoadPaymentsGrid()
    {
        var q = txtPaymentSearch.Text.Trim();
        var selectedDate = dtpPaymentDate.Value.Date;
        var rows = DataStore.Payments
            .Where(p => !p.IsDeleted && !string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase)
                && p.Date.Date == selectedDate
                && (string.IsNullOrWhiteSpace(q) || p.MemberName.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Reference.Contains(q, StringComparison.OrdinalIgnoreCase) || p.MemberId.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.Date)
            .Select(p => new { p.Reference, p.MemberId, p.MemberName, Amount = "₱" + p.Amount.ToString("N2"), p.Method, p.Date, p.Status })
            .ToList();
        gridPayments.DataSource = null; gridPayments.DataSource = rows;
        // Adjust column widths and enable text wrapping for auto-generated columns
        AdjustAutoGeneratedColumns(gridPayments);
    }

    private void AdjustAutoGeneratedColumns(DataGridView grid)
    {
        foreach (DataGridViewColumn col in grid.Columns)
        {
            // Disable text wrapping to keep data on single line
            col.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            // Increase width for columns that typically contain longer text
            if (col.Name == "Reference" || col.Name == "MemberId")
            {
                col.Width = 220;
            }
            else if (col.Name == "MemberName")
            {
                col.Width = 240;
            }
            else if (col.Name == "Date")
            {
                col.Width = 180;
            }
            else if (col.Name == "Amount" || col.Name == "Method" || col.Name == "Status")
            {
                col.Width = 120;
            }
        }
    }

    private void LoadActiveMembershipsGrid()
    {
        if (gridActiveMemberships == null) return;
        var rows = DataStore.Members.Where(m => m.ExpiryDate.Date >= DateTime.Today && m.Status == "Active")
            .OrderBy(m => m.FullName)
            .Select(m => new { m.Id, m.FullName, m.Plan, ExpiryDate = m.ExpiryDate.ToString("yyyy-MM-dd"), Fee = "₱" + m.Fee.ToString("N2"), m.Status })
            .ToList();
        gridActiveMemberships.DataSource = null; gridActiveMemberships.DataSource = rows;
    }

    private void LoadExpiredMembershipsGrid()
    {
        if (gridExpiredMemberships == null) return;
        var rows = DataStore.Members.Where(m => m.ExpiryDate.Date < DateTime.Today || string.Equals(m.Status, "Expired", StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.ExpiryDate)
            .Select(m => new { m.Id, m.FullName, m.Plan, ExpiryDate = m.ExpiryDate.ToString("yyyy-MM-dd"), Fee = "₱" + m.Fee.ToString("N2"), m.Status })
            .ToList();
        gridExpiredMemberships.DataSource = null; gridExpiredMemberships.DataSource = rows;
    }

    private void LoadRenewalHistoryGrid()
    {
        if (gridRenewalHistory == null) return;
        var rows = DataStore.MembershipRenewals
            .Where(r => string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.RenewalDate)
            .Select(r => new
            {
                r.Reference,
                Member = r.MemberName,
                RenewalDate = r.RenewalDate.ToString("MMM d, yyyy"),
                PlanChange = $"{r.PreviousPlan} → {r.NewPlan}",
                Amount = "₱" + r.AmountPaid.ToString("N2"),
                ProcessedBy = r.ProcessedBy
            })
            .ToList();
        gridRenewalHistory.DataSource = null; gridRenewalHistory.DataSource = rows;
    }

    private void ProcessMembershipRenewal(Member member)
    {
        if (!string.Equals(member.Status, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Only members with an Expired status can be renewed from Expired Members.", "Renewal unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new RenewalDialog(member);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var expectedFee = DataStore.GetPlanFee(dlg.SelectedPlan);
        var expectedExpiry = DataStore.GetPlanExpiry(dlg.SelectedPlan, dlg.StartDate);
        if (!string.Equals(dlg.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)
            || dlg.RenewalFee != expectedFee
            || dlg.ExpiryDate != expectedExpiry)
        {
            MessageBox.Show("Renewal must use a supported plan fee, an automatically calculated expiry date, and Cash payment.", "Invalid renewal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var previousPlan = member.Plan;
        var previousStartDate = member.StartDate;
        var previousExpiryDate = member.ExpiryDate;
        var previousFee = member.Fee;
        var previousStatus = member.Status;

        var payment = new Payment
        {
            Reference = DataStore.NewPaymentReference(),
            MemberId = member.Id,
            MemberName = member.FullName,
            Amount = dlg.RenewalFee,
            AmountPaid = dlg.AmountPaid,
            Change = dlg.Change,
            Method = dlg.PaymentMethod,
            PaymentType = "Membership Renewal",
            CollectedBy = _username,
            Date = DateTime.Now,
            Status = "Paid"
        };

        member.Plan = dlg.SelectedPlan;
        member.StartDate = dlg.StartDate;
        member.ExpiryDate = dlg.ExpiryDate;
        member.Fee = dlg.RenewalFee;
        member.Status = "Active";

        DataStore.Payments.Add(payment);
        DataStore.RecalculateMemberBalances();
        DataStore.SavePayments();
        DataStore.SaveMembers();

        var renewal = new MembershipRenewal
        {
            Reference = DataStore.NewRenewalReference(),
            MemberId = member.Id,
            MemberName = member.FullName,
            PreviousPlan = previousPlan,
            PreviousStatus = previousStatus,
            PreviousStartDate = previousStartDate,
            PreviousExpiryDate = previousExpiryDate,
            PreviousFee = previousFee,
            NewPlan = dlg.SelectedPlan,
            NewStartDate = dlg.StartDate,
            NewExpiryDate = dlg.ExpiryDate,
            NewFee = dlg.RenewalFee,
            RenewalDate = DateTime.Now,
            AmountPaid = dlg.AmountPaid,
            OutstandingAmount = Math.Max(0m, dlg.RenewalFee - dlg.AmountPaid),
            PaymentMethod = dlg.PaymentMethod,
            PaymentReference = payment.Reference,
            ProcessedBy = _username,
            Status = "Completed"
        };

        DataStore.MembershipRenewals.Add(renewal);
        DataStore.SaveMembershipRenewals();
        RecordActivity("Membership renewal", $"Renewed {member.FullName} to {renewal.NewPlan}; payment {payment.Reference}.", member.Id);

        MessageBox.Show($"Membership renewed successfully.{Environment.NewLine}Reference: {renewal.Reference}{Environment.NewLine}New expiry: {member.ExpiryDate:d}", "Membership renewed", MessageBoxButtons.OK, MessageBoxIcon.Information);

        try
        {
            ReceiptService.OpenReceipt(member, payment);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The renewal was saved, but the receipt could not be opened: {ex.Message}", "Receipt unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        LoadExpiredMembershipsGrid();
        LoadActiveMembershipsGrid();
        LoadRenewalHistoryGrid();
        LoadPaymentHistoryGrid();
        RefreshData();
    }

    private void LoadPaymentHistoryGrid()
    {
        if (gridPaymentHistory == null) return;
        var rows = DataStore.Payments
            .Where(p => !p.IsDeleted && !string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Date)
            .Select(p => new { p.Reference, p.MemberName, Date = p.Date.ToString("MMM d, yyyy"), p.Method, Amount = "₱" + p.Amount.ToString("N2"), p.Status, p.PaymentType, p.CollectedBy })
            .ToList();
        gridPaymentHistory.DataSource = null; gridPaymentHistory.DataSource = rows;
    }

    private static decimal GetRemainingBalance(Member member)
    {
        return Math.Max(0m, member.Fee - member.TotalPaid);
    }

    private void LoadUnpaidBalancesGrid()
    {
        if (gridUnpaidBalances == null) return;

        var rows = DataStore.Members.Where(m => !m.IsDeleted)
            .Where(m => GetRemainingBalance(m) > 0m)
            .OrderBy(m => m.FullName)
            .Select(m => new
            {
                Id = m.Id,
                MemberInitials = GetInitials(m.FullName),
                MemberName = m.FullName,
                m.Plan,
                AmountDue = "₱" + GetRemainingBalance(m).ToString("N2")
            })
            .ToList();

        gridUnpaidBalances.DataSource = null; gridUnpaidBalances.DataSource = rows;

        UpdateUnpaidBalancesMetrics();
    }

    private void UpdateUnpaidBalancesMetrics()
    {
        if (pageUnpaidBalances == null) return;

        var totalDue = DataStore.Members.Sum(GetRemainingBalance);
        var membersWithBalance = DataStore.Members.Count(m => GetRemainingBalance(m) > 0m);

        foreach (var ctrl in pageUnpaidBalances.Controls)
        {
            if (ctrl is Panel panel && panel.BackColor.R == 220 && panel.BackColor.G == 20 && panel.BackColor.B == 20)
            {
                foreach (var child in panel.Controls)
                {
                    if (child is Label lbl)
                    {
                        if (lbl.Font?.Size == 36)
                        {
                            lbl.Text = "₱" + totalDue.ToString("N2");
                        }
                        else if (lbl.Text.Contains("members with balances"))
                        {
                            lbl.Text = membersWithBalance + " members with balances";
                        }
                    }
                }
            }

            if (ctrl is Label progressLabel && progressLabel.Text.EndsWith("%"))
            {
                int percent = membersWithBalance > 0 ? Math.Min(100, (int)Math.Round((membersWithBalance / (double)Math.Max(DataStore.Members.Count, 1)) * 100d)) : 0;
                progressLabel.Text = percent + "%";
            }

            if (ctrl is ProgressBar prgBar)
            {
                prgBar.Value = membersWithBalance > 0 ? Math.Min(100, (int)Math.Round((membersWithBalance / (double)Math.Max(DataStore.Members.Count, 1)) * 100d)) : 0;
            }
        }
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(' ');
        if (parts.Length == 0) return "??";
        if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
        return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
    }


    private void gridExpiredMemberships_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var grid = gridExpiredMemberships;
        var col = grid.Columns[e.ColumnIndex].Name;
        var id = grid.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
        if (string.IsNullOrEmpty(id)) return;
        var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null) return;

        if (col == "Renew")
        {
            ProcessMembershipRenewal(member);
            return;
        }
    }

    private void gridActiveMemberships_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var grid = gridActiveMemberships;
        var col = grid.Columns[e.ColumnIndex].Name;
        var id = grid.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
        if (string.IsNullOrEmpty(id)) return;
        var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null) return;

        // Active Members is intentionally read-only. General record management remains in All Members.
    }

    private void gridPaymentHistory_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var grid = gridPaymentHistory;
        var col = grid.Columns[e.ColumnIndex].Name;
        var reference = grid.Rows[e.RowIndex].Cells["Reference"].Value?.ToString();
        if (string.IsNullOrEmpty(reference)) return;

        if (col == "Delete")
        {
            var payment = DataStore.Payments.FirstOrDefault(p => p.Reference == reference);
            if (payment is null) return;
            var ok = MessageBox.Show($"Delete payment {reference}? The record will be hidden from active reports but still remain in history.", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ok == DialogResult.Yes)
            {
                payment.IsDeleted = true;
                payment.DeletedAt = DateTime.Now;
                payment.DeletedBy = _username;
                payment.Status = "Deleted";
                DataStore.SavePayments();
                DataStore.SaveArchiveRecord("Payment", payment.Reference, payment.MemberName, payment.DeletedAt.Value, payment.DeletedBy, $"Amount: {payment.Amount:N2}");
                DataStore.RecalculateMemberBalances();
                DataStore.SaveMembers();
                RecordActivity("Payment deleted", $"Deleted payment {reference} for {payment.MemberName}.", payment.MemberId);
                LoadPaymentHistoryGrid();
                RefreshData();
            }
        }
    }

    private void gridUnpaidBalances_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var grid = gridUnpaidBalances;
        var col = grid.Columns[e.ColumnIndex].Name;
        var memberId = grid.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
        if (string.IsNullOrEmpty(memberId)) return;

        if (col == "CollectPayment")
        {
            var member = DataStore.Members.FirstOrDefault(m => m.Id == memberId);
            if (member is null) return;

            using var dlg = new PaymentDialog();
            dlg.SelectMember(member);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedMember is null) return;

            var amount = dlg.Amount;
            var remainingBalance = GetRemainingBalance(member);
            if (amount <= 0m || amount > remainingBalance)
            {
                MessageBox.Show($"Payment amount must be greater than zero and cannot exceed ₱{remainingBalance:N2}.", "Invalid payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var payment = new Payment
            {
                Reference = DataStore.NewPaymentReference(),
                MemberId = member.Id,
                MemberName = member.FullName,
                Amount = amount,
                Method = dlg.Method,
                PaymentType = "Balance Payment",
                CollectedBy = _username,
                Date = DateTime.Now,
                Status = "Paid"
            };

            DataStore.Payments.Add(payment);
            DataStore.RecalculateMemberBalances();
            DataStore.SavePayments();
            DataStore.SaveMembers();
            RecordActivity("Payment collected", $"Recorded payment {payment.Reference} for {member.FullName}.", member.Id);

            MessageBox.Show($"Payment recorded successfully.\nPayment reference: {payment.Reference}", "Payment collected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUnpaidBalancesGrid();
            RefreshData();
        }
    }

    private string? SelectedMemberId() => gridMembers.SelectedRows.Count > 0 ? gridMembers.SelectedRows[0].Cells["Id"].Value?.ToString() : null;
    private void btnDashboard_Click(object? s, EventArgs e) => ShowPage("Dashboard");
    private void btnMembers_Click(object? s, EventArgs e) => ShowPage("Members");
    private void btnPayments_Click(object? s, EventArgs e) => ShowPage("Payments");
    private void btnReports_Click(object? s, EventArgs e) => OpenReports();
    private void btnExpenses_Click(object? s, EventArgs e) => OpenExpenses();
    private void OpenQRCodeManagement()
    {
        if (!HasPermission(p => p.CanManageDailyGym)) return;
        using var form = new QRCodeManagementForm(_username);
        form.ShowDialog(this);
    }
    private void OpenReports()
    {
        if (!HasPermission(p => p.CanViewReports)) return;
        using var form = new ReportsForm(); form.ShowDialog(this); RefreshData();
    }
    private void OpenExpenses()
    {
        if (!HasPermission(p => p.CanManageExpenses)) return;
        using var form = new ExpensesForm(_username, HasPermission(p => p.CanManageExpenses)); form.ShowDialog(this); DataStore.Reload(); RefreshData();
    }
    private void btnRefresh_Click(object? s, EventArgs e) => RefreshData();
    private void btnAccountSettings_Click(object? s, EventArgs e)
    {
        using var dialog = new AccountSettingsDialog(_username);
        dialog.ShowDialog(this);
    }

    private void btnLogout_Click(object? s, EventArgs e)
    {
        // mark that the user explicitly signed out so the caller (login form) can react
        IsSignOut = true;
        _sessionEndReason = "Logout";
        Close();
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _databaseRefreshTimer.Stop();
        DataStore.RecordStaffActivity(_username, _sessionEndReason, _sessionEndReason == "Logout" ? "Staff signed out." : "Session ended when the application closed.");
    }

    private void RecordActivity(string action, string details, string memberId = "") =>
        DataStore.RecordStaffActivity(_username, action, details, memberId);
    private void txtMemberSearch_TextChanged(object? s, EventArgs e)
    {
        if (string.Equals(_membersView, "All Members", StringComparison.OrdinalIgnoreCase))
        {
            LoadMembersGrid();
            return;
        }

        if (string.Equals(_membersView, "Active Members", StringComparison.OrdinalIgnoreCase))
        {
            LoadActiveMembershipsGrid();
            return;
        }

        if (string.Equals(_membersView, "Expired Members", StringComparison.OrdinalIgnoreCase))
        {
            LoadExpiredMembershipsGrid();
            return;
        }

        if (string.Equals(_membersView, "Renewal History", StringComparison.OrdinalIgnoreCase))
        {
            LoadRenewalHistoryGrid();
            return;
        }

        if (string.Equals(_membersView, "Payment History", StringComparison.OrdinalIgnoreCase))
        {
            LoadPaymentHistoryGrid();
        }
    }
    private void txtPaymentSearch_TextChanged(object? s, EventArgs e) => LoadPaymentsGrid();
    private void dtpPaymentDate_ValueChanged(object? s, EventArgs e) => LoadPaymentsGrid();
    private void btnAddMember_Click(object? s, EventArgs e)
    {
        if (!HasPermission(p => p.CanAddMembers)) return;
        using var dlg = new MemberDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.ResultMember is null) return;
        var member = dlg.ResultMember;
        if (DataStore.Members.Any(m => m.Phone == member.Phone && !string.IsNullOrWhiteSpace(member.Phone))) { MessageBox.Show("A member with this phone number already exists.", "Duplicate member", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        DataStore.Members.Add(member);
        DataStore.SaveMembers();
        var payment = new Payment
        {
            Reference = DataStore.NewPaymentReference(),
            MemberId = member.Id,
            MemberName = member.FullName,
            AmountPaid = dlg.AmountPaid,
            Change = dlg.Change,
            Amount = Math.Max(0m, dlg.AmountPaid - dlg.Change),
            Method = dlg.PaymentMethod,
            PaymentType = "Membership Payment",
            CollectedBy = _username,
            Date = DateTime.Now,
            Status = "Paid"
        };
        DataStore.Payments.Add(payment);
        DataStore.RecalculateMemberBalances();
        DataStore.SavePayments();
        DataStore.SaveMembers();
        RecordActivity("Member registration", $"Registered {member.FullName}; payment {payment.Reference}.", member.Id);
        RefreshData();
        MessageBox.Show($"Member registered successfully.\nMember ID: {member.Id}\nPayment reference: {payment.Reference}\nAmount paid: PHP {payment.AmountPaid:N2}\nChange: PHP {payment.Change:N2}\nRecorded revenue: PHP {payment.Amount:N2}", "Registration complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        try
        {
            // Always show the receipt after registration. Printing is optional and
            // should not be the only way the user can view the completed receipt.
            ReceiptService.OpenReceipt(member, payment);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The member was saved, but the receipt could not be opened: {ex.Message}", "Receipt unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    private void btnEditMember_Click(object? s, EventArgs e)
    {
        if (!HasPermission(p => p.CanEditMembers)) return;
        var id = SelectedMemberId(); var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null) { MessageBox.Show("Select a member to edit."); return; }
        using var dlg = new MemberDialog(member);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultMember is not null) { var updated = dlg.ResultMember; var idx = DataStore.Members.FindIndex(m => m.Id == id); DataStore.Members[idx] = updated; DataStore.RecalculateMemberBalances(); DataStore.SaveMembers(); RecordActivity("Member updated", $"Updated member details for {updated.FullName}.", updated.Id); RefreshData(); }
    }
    private void btnMemberPass_Click(object? s, EventArgs e)
    {
        var id = SelectedMemberId();
        var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null)
        {
            MessageBox.Show("Please select a member to view or print their Member Pass.", "No member selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new MemberCardDialog(member);
        dlg.ShowDialog(this);
    }
    private void btnDeleteMember_Click(object? s, EventArgs e)
    {
        if (!HasPermission(p => p.CanDeleteMembers)) return;
        var id = SelectedMemberId(); var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null) { MessageBox.Show("Select a member to delete."); return; }
        if (MessageBox.Show($"Move {member.FullName} to Archive? Payment history will remain for auditing.", "Confirm archive", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        member.IsDeleted = true;
        member.DeletedAt = DateTime.Now;
        member.DeletedBy = _username;
        DataStore.SaveMembers();
        DataStore.SaveArchiveRecord("Member", member.Id, member.FullName, member.DeletedAt.Value, member.DeletedBy);
        RecordActivity("Member archived", $"Archived member {member.FullName}.", member.Id); RefreshData();
    }
    private void btnRecordPayment_Click(object? s, EventArgs e)
    {
        if (DataStore.Members.Count == 0) { MessageBox.Show("Register a member first."); return; }
        using var dlg = new PaymentDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedMember is null) return;
        var m = dlg.SelectedMember;
        var payment = new Payment
        {
            Reference = DataStore.NewPaymentReference(),
            MemberId = m.Id,
            MemberName = m.FullName,
            Amount = dlg.Amount,
            Method = dlg.Method,
            PaymentType = "Membership Payment",
            CollectedBy = _username,
            Status = "Paid"
        };

        DataStore.Payments.Add(payment);
        DataStore.RecalculateMemberBalances();
        DataStore.SavePayments();
        DataStore.SaveMembers();
        RecordActivity("Payment collected", $"Recorded payment {payment.Reference} for {m.FullName}.", m.Id);

        RefreshData(); MessageBox.Show($"Payment saved.\nReference: {payment.Reference}", "Payment recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private void btnExportMembers_Click(object? s, EventArgs e) => ExportCsv("members.csv", new[] { "Member ID,Full Name,Phone,Email,Plan,Fee,Start Date,Expiry Date,Status" }.Concat(DataStore.Members.Select(m => string.Join(",", Csv(m.Id), Csv(m.FullName), Csv(m.Phone), Csv(m.Email), Csv(m.Plan), m.Fee, m.StartDate.ToString("yyyy-MM-dd"), m.ExpiryDate.ToString("yyyy-MM-dd"), Csv(m.Status)))));
    private void btnExportPayments_Click(object? s, EventArgs e) => ExportCsv("payments.csv", new[] { "Reference,Member ID,Member Name,Amount,Method,Date,Status" }.Concat(DataStore.Payments.Select(p => string.Join(",", Csv(p.Reference), Csv(p.MemberId), Csv(p.MemberName), p.Amount, Csv(p.Method), p.Date.ToString("yyyy-MM-dd HH:mm"), Csv(p.Status)))));
    private static string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
    private void ExportCsv(string filename, IEnumerable<string> lines)
    {
        using var save = new SaveFileDialog { FileName = filename, Filter = "CSV file (*.csv)|*.csv", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllLines(save.FileName, lines, Encoding.UTF8); MessageBox.Show("Export completed successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show("Could not export file: " + ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void gridMembers_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var grid = gridMembers;
        var col = grid.Columns[e.ColumnIndex].Name;
        if (col != "Delete") return;
        var id = grid.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
        if (string.IsNullOrEmpty(id)) return;
        var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null) return;
        if (MessageBox.Show($"Move {member.FullName} to Archive? Payment history will remain for auditing.", "Confirm archive", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        member.IsDeleted = true;
        member.DeletedAt = DateTime.Now;
        member.DeletedBy = _username;
        DataStore.SaveMembers();
        DataStore.SaveArchiveRecord("Member", member.Id, member.FullName, member.DeletedAt.Value, member.DeletedBy);
        LoadMembersGrid(); LoadActiveMembershipsGrid(); RefreshData();
    }

    private string FormatPeso(decimal amount) => amount.ToString("C2", _pesoCulture);

    private void InitializeIncomeReportsPage()
    {
        if (pageIncomeReports.Controls.OfType<Label>().Any(l => l.Name == "lblIncomeReportSelection")) return;

        pageIncomeReports.Controls.Clear();
        pageIncomeReports.AutoScroll = true;
        pageIncomeReports.Padding = new Padding(24, 20, 24, 20);
        pageIncomeReports.BackColor = Color.FromArgb(248, 249, 251);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 156F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 310F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 278F));
        pageIncomeReports.Controls.Add(content);

        var headerPanel = new Panel { Dock = DockStyle.Fill, Height = 64, Margin = new Padding(0, 0, 0, 14), BackColor = Color.Transparent };
        var lblIncomeHeader = new Label { Name = "lblIncomeHeader", Text = "Income Reports", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Dock = DockStyle.Top, Height = 34 };
        headerPanel.Controls.Add(lblIncomeHeader);
        content.Controls.Add(headerPanel, 0, 0);

        var filterPanel = new Panel
        {
            Name = "pnlIncomeReportFilters",
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 16)
        };

        var filterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));

        var filterTitle = new Label { Text = "Report Filters", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        filterLayout.Controls.Add(filterTitle, 0, 0);
        filterLayout.SetColumnSpan(filterTitle, 4);

        var dateLabel = CreateIncomeFilterLabel("Date");
        var monthLabel = CreateIncomeFilterLabel("Month");
        var yearLabel = CreateIncomeFilterLabel("Year");
        filterLayout.Controls.Add(dateLabel, 0, 1);
        filterLayout.Controls.Add(monthLabel, 1, 1);
        filterLayout.Controls.Add(yearLabel, 2, 1);
        filterLayout.Controls.Add(new Label { Text = "Selected period", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(112, 120, 132), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(8, 0, 0, 2) }, 3, 1);

        dtpIncomeReportDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 30, Value = DateTime.Today, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 4, 8, 0) };
        dtpIncomeReportDate.ValueChanged += (_, _) => LoadIncomeReportsData();

        cmbIncomeReportMonth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 30, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 4, 8, 0) };
        for (var month = 1; month <= 12; month++)
        {
            cmbIncomeReportMonth.Items.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month));
        }
        cmbIncomeReportMonth.SelectedIndexChanged += (_, _) => LoadIncomeReportsData();

        cmbIncomeReportYear = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 30, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 4, 8, 0) };
        var incomeYears = DataStore.Payments
            .Where(p => !p.IsDeleted && !string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Date.Year)
            .Distinct()
            .DefaultIfEmpty(DateTime.Today.Year)
            .OrderBy(y => y)
            .ToList();
        if (!incomeYears.Contains(DateTime.Today.Year)) incomeYears.Add(DateTime.Today.Year);
        foreach (var year in incomeYears.OrderBy(y => y))
        {
            cmbIncomeReportYear.Items.Add(year);
        }
        cmbIncomeReportYear.SelectedIndexChanged += (_, _) => LoadIncomeReportsData();

        filterLayout.Controls.Add(dtpIncomeReportDate, 0, 2);
        filterLayout.Controls.Add(cmbIncomeReportMonth, 1, 2);
        filterLayout.Controls.Add(cmbIncomeReportYear, 2, 2);

        var selectionSummary = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8, 0, 0, 0), Margin = new Padding(0) };
        lblIncomeReportSelection = CreateIncomeSummaryLabel("Selected: ", true);
        lblIncomeReportDaySummary = CreateIncomeSummaryLabel("Day total: ₱0.00");
        lblIncomeReportMonthSummary = CreateIncomeSummaryLabel("Month total: ₱0.00");
        lblIncomeReportYearSummary = CreateIncomeSummaryLabel("Year total: ₱0.00");
        selectionSummary.Controls.Add(lblIncomeReportSelection);
        selectionSummary.Controls.Add(lblIncomeReportDaySummary);
        selectionSummary.Controls.Add(lblIncomeReportMonthSummary);
        selectionSummary.Controls.Add(lblIncomeReportYearSummary);
        filterLayout.Controls.Add(selectionSummary, 3, 2);
        filterLayout.SetRowSpan(selectionSummary, 1);
        filterPanel.Controls.Add(filterLayout);
        content.Controls.Add(filterPanel, 0, 1);

        var cardsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 156,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(0)
        };
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));

        var cardToday = CreateIncomeCard("💼", "Today's income", "₱0.00", "0 transactions today", out var todayValue, out var todayDetail);
        var cardMonthly = CreateIncomeCard("📊", "Monthly income", "₱0.00", "0 transactions this month", out var monthlyValue, out var monthlyDetail);
        var cardRecorded = CreateIncomeCard("💳", "Recorded payments", "₱0.00", "0 visible transactions", out var recordedValue, out var recordedDetail);

        lblIncomeToday = todayValue;
        lblIncomeMonthly = monthlyValue;
        lblIncomeRecorded = recordedValue;
        lblIncomeTodayDetail = todayDetail;
        lblIncomeMonthlyDetail = monthlyDetail;
        lblIncomeRecordedDetail = recordedDetail;

        cardsPanel.Controls.Add(cardToday, 0, 0);
        cardsPanel.Controls.Add(cardMonthly, 1, 0);
        cardsPanel.Controls.Add(cardRecorded, 2, 0);
        content.Controls.Add(cardsPanel, 0, 2);

        var chartSection = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 310, ColumnCount = 1, RowCount = 3, BackColor = Color.White, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 16) };
        chartSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        chartSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        chartSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        chartSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var chartHeader = new Label { Text = "Monthly Revenue", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        lblIncomeChartSubtitle = new Label { Text = "Income performance for 2025", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(112, 120, 132), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        pnlIncomeRevenueChart = new Panel
        {
            Name = "pnlIncomeRevenueChart",
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0)
        };
        pnlIncomeRevenueChart.Paint += (_, e) => DrawMonthlyRevenueChart(e.Graphics, pnlIncomeRevenueChart.Width, pnlIncomeRevenueChart.Height);
        chartSection.Controls.Add(chartHeader, 0, 0);
        chartSection.Controls.Add(lblIncomeChartSubtitle, 0, 1);
        chartSection.Controls.Add(pnlIncomeRevenueChart, 0, 2);
        content.Controls.Add(chartSection, 0, 3);

        var tableSection = new TableLayoutPanel { Dock = DockStyle.Fill, Height = 278, ColumnCount = 1, RowCount = 2, BackColor = Color.White, Padding = new Padding(16), Margin = new Padding(0) };
        tableSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        tableSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var tableHeader = new Label { Text = "Selected Day Payment Details", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        gridIncomeReportTransactions = MakeGrid();
        gridIncomeReportTransactions.Dock = DockStyle.Fill;
        gridIncomeReportTransactions.MinimumSize = new Size(0, 180);
        gridIncomeReportTransactions.AutoGenerateColumns = false;
        gridIncomeReportTransactions.ScrollBars = ScrollBars.Vertical;
        gridIncomeReportTransactions.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        gridIncomeReportTransactions.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        gridIncomeReportTransactions.RowTemplate.Height = 34;
        gridIncomeReportTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridIncomeReportTransactions.RowHeadersVisible = false;
        gridIncomeReportTransactions.AllowUserToAddRows = false;
        gridIncomeReportTransactions.AllowUserToDeleteRows = false;
        gridIncomeReportTransactions.ReadOnly = true;
        gridIncomeReportTransactions.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        gridIncomeReportTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "REFERENCE", DataPropertyName = "Reference", MinimumWidth = 120, Width = 140, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridIncomeReportTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "MemberName", HeaderText = "MEMBER", DataPropertyName = "MemberName", MinimumWidth = 180, Width = 220, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridIncomeReportTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "DATE", DataPropertyName = "Date", MinimumWidth = 110, Width = 120, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridIncomeReportTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "METHOD", DataPropertyName = "Method", MinimumWidth = 90, Width = 100, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridIncomeReportTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentType", HeaderText = "TYPE", DataPropertyName = "PaymentType", MinimumWidth = 120, Width = 150, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridIncomeReportTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "AMOUNT", DataPropertyName = "Amount", MinimumWidth = 100, Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, WrapMode = DataGridViewTriState.False } });
        gridIncomeReportTransactions.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && gridIncomeReportTransactions.Columns[e.ColumnIndex].Name == "Amount")
            {
                if (e.CellStyle != null) e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        };
        gridIncomeReportTransactions.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.RowIndex < gridIncomeReportTransactions.Rows.Count)
            {
                var value = gridIncomeReportTransactions.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(value) && value.Length > 26)
                {
                    e.ToolTipText = value;
                }
            }
        };

        gridIncomeReportTransactions.ColumnHeadersHeight = 34;
        gridIncomeReportTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 132, 35);
        gridIncomeReportTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        gridIncomeReportTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        gridIncomeReportTransactions.EnableHeadersVisualStyles = false;
        tableSection.Controls.Add(tableHeader, 0, 0);
        tableSection.Controls.Add(gridIncomeReportTransactions, 0, 1);
        content.Controls.Add(tableSection, 0, 4);

        if (cmbIncomeReportMonth.Items.Count > 0) cmbIncomeReportMonth.SelectedIndex = DateTime.Today.Month - 1;
        if (cmbIncomeReportYear.Items.Count > 0) cmbIncomeReportYear.SelectedItem = DateTime.Today.Year;
    }

    private static Label CreateIncomeFilterLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(112, 120, 132),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft,
        Padding = new Padding(0, 0, 0, 2)
    };

    private static Label CreateIncomeSummaryLabel(string text, bool emphasized = false) => new()
    {
        Name = emphasized ? "lblIncomeReportSelection" : string.Empty,
        Text = text,
        Font = new Font("Segoe UI", emphasized ? 9.5F : 9F, emphasized ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = emphasized ? Color.FromArgb(58, 66, 76) : Color.FromArgb(112, 120, 132),
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 1)
    };

    private static Panel CreateIncomeCard(string icon, string title, string valueText, string detailText, out Label valueLabel, out Label detailLabel)
    {
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 10, 0)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var iconLabel = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 16), ForeColor = Color.FromArgb(255, 132, 35), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
        valueLabel = new Label { Text = valueText, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        var titleLabel = new Label { Text = title, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(87, 96, 108), Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        detailLabel = new Label { Text = detailText, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(112, 120, 132), Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.TopLeft };

        layout.Controls.Add(iconLabel, 0, 0);
        layout.SetRowSpan(iconLabel, 3);
        layout.Controls.Add(titleLabel, 1, 0);
        layout.Controls.Add(valueLabel, 1, 1);
        layout.Controls.Add(detailLabel, 1, 2);
        container.Controls.Add(layout);
        return container;
    }

    private int GetSelectedIncomeYear() => cmbIncomeReportYear?.SelectedItem is int year ? year : DateTime.Today.Year;

    private int GetSelectedIncomeMonth() => cmbIncomeReportMonth?.SelectedIndex is int monthIndex && monthIndex >= 0 ? monthIndex + 1 : DateTime.Today.Month;

    private void DrawMonthlyRevenueChart(Graphics g, int width, int height)
    {
        var year = GetSelectedIncomeYear();
        var monthlyTotals = Enumerable.Range(1, 12)
            .Select(month => new
            {
                Month = month,
                Total = DataStore.Payments
                    .Where(p => !p.IsDeleted && !string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase)
                        && p.Date.Year == year && p.Date.Month == month)
                    .Sum(p => p.Amount)
            })
            .ToList();

        var maxValue = monthlyTotals.Max(m => m.Total);
        if (maxValue <= 0m) maxValue = 1m;

        var padLeft = 34;
        var padRight = 14;
        var padTop = 18;
        var padBottom = 42;
        var chartArea = new Rectangle(padLeft, padTop, Math.Max(10, width - padLeft - padRight), Math.Max(10, height - padTop - padBottom));
        var barStep = chartArea.Width / 12.0;
        var baseY = chartArea.Bottom;

        using var axisPen = new Pen(Color.FromArgb(203, 208, 217));
        using var textBrush = new SolidBrush(Color.FromArgb(75, 85, 93));
        using var fillBrush = new SolidBrush(Color.FromArgb(255, 132, 35));
        using var gridBrush = new SolidBrush(Color.FromArgb(246, 248, 250));

        g.FillRectangle(gridBrush, chartArea);
        g.DrawLine(axisPen, new Point(chartArea.Left, baseY), new Point(chartArea.Right, baseY));
        g.DrawLine(axisPen, new Point(chartArea.Left, chartArea.Top), new Point(chartArea.Left, baseY));

        for (var month = 1; month <= 12; month++)
        {
            var total = monthlyTotals[month - 1].Total;
            var barWidth = Math.Max(18, (float)(barStep * 0.62));
            var x = (float)(chartArea.Left + (month - 1) * barStep + barStep / 2 - barWidth / 2);
            var ratio = (double)(total / maxValue);
            var barHeight = Math.Max(10, (float)(ratio * (chartArea.Height - 12)));
            var y = baseY - barHeight;

            g.FillRectangle(fillBrush, x, y, barWidth, barHeight);
            g.DrawString(total.ToString("C2", _pesoCulture), new Font("Segoe UI", 7), textBrush, new RectangleF(x - 14, Math.Max(6, y - 18), barWidth + 28, 16));
            g.DrawString(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month), new Font("Segoe UI", 7), textBrush, new RectangleF(x - 10, baseY + 10, barWidth + 20, 16));
        }
    }

    private void LoadIncomeReportsData()
    {
        if (pageIncomeReports == null) return;

        var selectedDate = dtpIncomeReportDate?.Value ?? DateTime.Today;
        var selectedMonth = GetSelectedIncomeMonth();
        var selectedYear = GetSelectedIncomeYear();

        var visiblePayments = DataStore.Payments
            .Where(p => !p.IsDeleted && !string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var dayPayments = visiblePayments.Where(p => p.Date.Date == selectedDate.Date).OrderByDescending(p => p.Date).ToList();
        var monthPayments = visiblePayments.Where(p => p.Date.Year == selectedYear && p.Date.Month == selectedMonth).ToList();
        var yearPayments = visiblePayments.Where(p => p.Date.Year == selectedYear).ToList();

        var dayTotal = dayPayments.Sum(p => p.Amount);
        var monthTotal = monthPayments.Sum(p => p.Amount);
        var yearTotal = yearPayments.Sum(p => p.Amount);

        if (lblIncomeToday != null) lblIncomeToday.Text = FormatPeso(dayTotal);
        if (lblIncomeMonthly != null) lblIncomeMonthly.Text = FormatPeso(monthTotal);
        if (lblIncomeRecorded != null) lblIncomeRecorded.Text = FormatPeso(yearTotal);
        if (lblIncomeTodayDetail != null) lblIncomeTodayDetail.Text = dayPayments.Count == 1 ? "1 transaction on selected date" : $"{dayPayments.Count} transactions on selected date";
        if (lblIncomeMonthlyDetail != null) lblIncomeMonthlyDetail.Text = monthPayments.Count == 1 ? "1 transaction this month" : $"{monthPayments.Count} transactions this month";
        if (lblIncomeRecordedDetail != null) lblIncomeRecordedDetail.Text = yearPayments.Count == 1 ? "1 visible transaction" : $"{yearPayments.Count} visible transactions";

        if (lblIncomeReportSelection != null)
        {
            lblIncomeReportSelection.Text = $"Selected: {selectedDate:MMM d, yyyy} • {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(selectedMonth)} {selectedYear}";
        }
        if (lblIncomeReportDaySummary != null) lblIncomeReportDaySummary.Text = "Day total: " + FormatPeso(dayTotal);
        if (lblIncomeReportMonthSummary != null) lblIncomeReportMonthSummary.Text = "Month total: " + FormatPeso(monthTotal);
        if (lblIncomeReportYearSummary != null) lblIncomeReportYearSummary.Text = "Year total: " + FormatPeso(yearTotal);
        if (lblIncomeChartSubtitle != null) lblIncomeChartSubtitle.Text = $"Income performance for {selectedYear}";

        if (gridIncomeReportTransactions != null)
        {
            var rows = dayPayments
                .OrderByDescending(p => p.Date)
                .Select(p => new
                {
                    Reference = p.Reference,
                    MemberName = p.MemberName,
                    Date = p.Date.ToString("MMM d, yyyy"),
                    Method = p.Method,
                    PaymentType = p.PaymentType,
                    Amount = FormatPeso(p.Amount)
                })
                .ToList();

            if (rows.Count == 0)
            {
                rows.Add(new { Reference = "No payments", MemberName = "No transactions", Date = selectedDate.ToString("MMM d, yyyy"), Method = "-", PaymentType = "-", Amount = "₱0.00" });
            }

            gridIncomeReportTransactions.DataSource = rows;
            gridIncomeReportTransactions.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridIncomeReportTransactions.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            gridIncomeReportTransactions.Columns["Reference"].MinimumWidth = 120;
            gridIncomeReportTransactions.Columns["MemberName"].MinimumWidth = 180;
            gridIncomeReportTransactions.Columns["Date"].MinimumWidth = 110;
            gridIncomeReportTransactions.Columns["Method"].MinimumWidth = 90;
            gridIncomeReportTransactions.Columns["PaymentType"].MinimumWidth = 120;
            gridIncomeReportTransactions.Columns["Amount"].MinimumWidth = 110;
        }

        if (pnlIncomeRevenueChart != null) pnlIncomeRevenueChart.Invalidate();
    }

    // Staff Users page loading
    private void LoadStaffUsersData()
    {
        try
        {
            var profiles = DataStore.Staff
                .Where(s => !s.IsDeleted)
                .GroupBy(s => s.Username, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            if (profiles.Count == 0)
            {
                foreach (var profile in DatabaseConnection.GetStaffUsers().Where(s => !s.IsDeleted))
                    profiles.TryAdd(profile.Username, profile);
            }

            var activeUsers = DataStore.Users
                .Where(u => !u.IsDeleted && u.IsActive)
                .ToList();
            var adminCount = activeUsers.Count(u => IsAdminRole(u.Role));
            var staffCount = activeUsers.Count - adminCount;
            lblTotalStaff.Text = adminCount.ToString();
            lblActiveStaff.Text = staffCount.ToString();
            lblTotalUsers.Text = activeUsers.Count.ToString();

            var search = txtUserSearch?.Text.Trim() ?? string.Empty;
            var displayData = activeUsers
                .Select(user =>
                {
                    profiles.TryGetValue(user.Username, out var profile);
                    var isAdmin = IsAdminRole(user.Role);
                    var name = string.IsNullOrWhiteSpace(profile?.FullName) ? user.Username : profile.FullName;
                    var role = isAdmin ? "Admin" : "Staff";
                    var contact = string.Join("  •  ", new[] { profile?.Email, profile?.PhoneNumber }
                        .Where(value => !string.IsNullOrWhiteSpace(value)));
                    var position = isAdmin
                        ? "System Administrator"
                        : string.IsNullOrWhiteSpace(profile?.Position) ? "Staff" : profile.Position;
                    var joined = profile is null || profile.JoinedDate == default
                        ? "—"
                        : profile.JoinedDate.ToString("MMM d, yyyy");
                    var status = profile is null || string.IsNullOrWhiteSpace(profile.Status) ? "Active" : profile.Status;

                    return new
                    {
                        Id = profile?.Id ?? 0,
                        StaffInitials = GetInitials(name),
                        StaffName = name,
                        Role = role,
                        Contact = string.IsNullOrWhiteSpace(contact) ? "—" : contact,
                        Position = position,
                        Joined = joined,
                        Status = status,
                        SearchText = string.Join(" ", name, contact, profile?.PhoneNumber, role)
                    };
                })
                .Where(row => string.IsNullOrWhiteSpace(search)
                    || row.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(row => row.Role)
                .ThenBy(row => row.StaffName)
                .Select(row => new
                {
                    row.Id,
                    row.StaffInitials,
                    row.StaffName,
                    row.Role,
                    row.Contact,
                    row.Position,
                    row.Joined,
                    row.Status
                })
                .ToList();

            gridStaffDirectory.DataSource = displayData;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading staff data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsAdminRole(string? role) =>
        string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase);

    private static bool IsStaffAccount(Staff staff)
    {
        var user = DataStore.Users.FirstOrDefault(u => u.Username.Equals(staff.Username, StringComparison.OrdinalIgnoreCase));
        var role = string.IsNullOrWhiteSpace(user?.Role) ? staff.Position : user.Role;
        return !IsAdminRole(role);
    }

    private void gridStaffDirectory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        var grid = sender as DataGridView;
        if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

        if (grid.Columns[e.ColumnIndex].Name == "Status")
        {
            var value = e.Value?.ToString();
            if (string.IsNullOrEmpty(value)) return;

            var active = string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase);
            var style = e.CellStyle ?? new DataGridViewCellStyle();
            style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            style.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            style.Padding = new Padding(8, 2, 8, 2);
            style.ForeColor = active ? Color.FromArgb(22, 125, 72) : Color.FromArgb(96, 104, 113);
            style.BackColor = active ? Color.FromArgb(224, 244, 232) : Color.FromArgb(240, 243, 247);
            style.SelectionBackColor = style.BackColor;
            style.SelectionForeColor = style.ForeColor;
            e.CellStyle = style;
        }
        else if (grid.Columns[e.ColumnIndex].Name == "Role")
        {
            var isAdmin = string.Equals(e.Value?.ToString(), "Admin", StringComparison.OrdinalIgnoreCase);
            var style = e.CellStyle ?? new DataGridViewCellStyle();
            style.Alignment = DataGridViewContentAlignment.MiddleCenter;
             style.ForeColor = Color.FromArgb(176, 92, 0);
             style.BackColor = Color.FromArgb(255, 241, 220);
            style.SelectionBackColor = style.BackColor;
            style.SelectionForeColor = style.ForeColor;
            e.CellStyle = style;
        }
        else if (grid.Columns[e.ColumnIndex].Name == "Edit")
        {
            var style = e.CellStyle ?? new DataGridViewCellStyle();
            style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            style.ForeColor = Color.FromArgb(80, 100, 130);
            e.CellStyle = style;
        }
        else if (grid.Columns[e.ColumnIndex].Name == "StaffInitials")
        {
            var style = e.CellStyle ?? new DataGridViewCellStyle();
            style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            style.BackColor = Color.White;
            style.SelectionBackColor = Color.White;
            style.SelectionForeColor = Color.FromArgb(35, 40, 48);
            e.CellStyle = style;
        }
    }

    private void gridStaffDirectory_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        var grid = sender as DataGridView;
        if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

        if (grid.Columns[e.ColumnIndex].Name == "StaffInitials")
        {
            e.PaintBackground(e.ClipBounds, false);

            var graphics = e.Graphics;
            if (graphics is null) return;

            var initials = e.Value?.ToString() ?? "";
            var rect = new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y + 5, 32, 32);
            using var circleBrush = new SolidBrush(Color.FromArgb(40, 42, 46));
            using var textBrush = new SolidBrush(Color.White);
            graphics.FillEllipse(circleBrush, rect);

            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(initials, new Font("Segoe UI", 10, FontStyle.Bold), textBrush, rect, format);
            e.Handled = true;
        }
        else if (grid.Columns[e.ColumnIndex].Name == "Edit")
        {
            e.PaintBackground(e.ClipBounds, true);
            var graphics = e.Graphics;
            if (graphics is null) return;
            using var brush = new SolidBrush(Color.FromArgb(80, 100, 130));
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString("✎", new Font("Segoe UI Symbol", 12f), brush, e.CellBounds, format);
            e.Handled = true;
        }
        else if (grid.Columns[e.ColumnIndex].Name == "Delete")
        {
            e.PaintBackground(e.ClipBounds, true);
            var graphics = e.Graphics;
            if (graphics is null) return;
            using var brush = new SolidBrush(Color.FromArgb(83, 92, 100));
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString("🗑", new Font("Segoe UI Symbol", 12f), brush, e.CellBounds, format);
            e.Handled = true;
        }
    }

    private void btnCreateNewStaff_Click(object? sender, EventArgs e)
    {
        if (!HasPermission(p => p.CanManageStaff)) return;
        using (var dialog = new CreateStaffDialog())
        {
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var staff = dialog.GetStaff();
                if (staff != null)
                {
                    if (DataStore.Users.Any(u => u.Username.Equals(staff.Username, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("An account with this username already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var temporaryPassword = staff.Password;
                    staff.PasswordHash = PasswordHasher.Hash(temporaryPassword);
                    staff.Password = string.Empty;
                    bool success = DatabaseConnection.InsertStaff(staff);

                    staff.Id = DataStore.Staff.Count > 0 ? DataStore.Staff.Max(s => s.Id) + 1 : 1;
                    DataStore.Staff.Add(staff);
                    DataStore.SaveStaff();
                    DataStore.Users.Add(new AppUser
                    {
                        Username = staff.Username,
                        PasswordHash = staff.PasswordHash,
                        Role = string.IsNullOrWhiteSpace(staff.Position) ? "Staff" : staff.Position,
                        IsActive = true
                    });
                    DataStore.SaveUsers();

                    if (success || DataStore.Staff.Count > 0)
                    {
                        MessageBox.Show($"Staff account created successfully!\n\nUsername: {staff.Username}\nTemporary password: {temporaryPassword}\n\nPlease provide these credentials securely and ask the staff member to change the password in Account Settings.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadStaffUsersData();
                    }
                    else
                    {
                        MessageBox.Show("Error creating staff account. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    private void gridStaffDirectory_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (!HasPermission(p => p.CanManageStaff)) return;
        if (e.RowIndex < 0) return;

        if (e.ColumnIndex == gridStaffDirectory.Columns["Edit"].Index)
        {
            var staffId = gridStaffDirectory.Rows[e.RowIndex].Cells["Id"].Value;
            if (staffId is not null && int.TryParse(staffId.ToString(), out var id))
            {
                var staff = DataStore.Staff.FirstOrDefault(s => s.Id == id);
                if (staff is null)
                {
                    MessageBox.Show("This account is managed by the connected database and is not available for local editing.", "Edit unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dialog = new CreateStaffDialog(staff);
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.GetStaff() is Staff updated)
                {
                    staff.FirstName = updated.FirstName;
                    staff.MiddleName = updated.MiddleName;
                    staff.LastName = updated.LastName;
                    staff.FullName = updated.FullName;
                    staff.Email = updated.Email;
                    staff.PhoneNumber = updated.PhoneNumber;
                    staff.Birthday = updated.Birthday;
                    staff.Position = updated.Position;
                    staff.HomeAddress = updated.HomeAddress;
                    staff.Permissions = updated.Permissions;
                    var account = DataStore.Users.FirstOrDefault(u => u.Username.Equals(staff.Username, StringComparison.OrdinalIgnoreCase));
                    if (account is not null)
                    {
                        account.Role = staff.Position;
                        account.IsActive = string.Equals(staff.Status, "Active", StringComparison.OrdinalIgnoreCase);
                    }
                    DataStore.SaveStaff();
                    DataStore.SaveUsers();
                    LoadStaffUsersData();
                    MessageBox.Show("Staff permissions updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            return;
        }

        if (e.ColumnIndex == gridStaffDirectory.Columns["Delete"].Index)
        {
            var row = gridStaffDirectory.Rows[e.RowIndex];
            var staffId = row.Cells["Id"].Value;

            if (staffId != null && int.TryParse(staffId.ToString(), out int id))
            {
                var result = MessageBox.Show("Move this staff account to Archive? It will no longer be able to log in.", "Confirm archive", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    bool success = DatabaseConnection.DeleteStaff(id);

                    var staffToRemove = DataStore.Staff.FirstOrDefault(s => s.Id == id);
                    if (staffToRemove != null)
                    {
                        var account = DataStore.Users.FirstOrDefault(u => u.Username.Equals(staffToRemove.Username, StringComparison.OrdinalIgnoreCase));
                        if (account != null)
                        {
                            account.IsActive = false;
                            account.IsDeleted = true;
                            account.DeletedAt = DateTime.Now;
                            account.DeletedBy = _username;
                            DataStore.SaveUsers();
                        }

                        staffToRemove.IsDeleted = true;
                        staffToRemove.DeletedAt = DateTime.Now;
                        staffToRemove.DeletedBy = _username;
                        staffToRemove.Status = "Inactive";
                        DataStore.SaveStaff();
                        DataStore.SaveArchiveRecord("Staff", staffToRemove.Username, staffToRemove.FullName, staffToRemove.DeletedAt.Value, staffToRemove.DeletedBy, staffToRemove.Position);
                    }

                    if (success || staffToRemove != null)
                    {
                        MessageBox.Show("Staff account moved to Archive successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadStaffUsersData();
                    }
                    else
                    {
                        MessageBox.Show("Error deleting staff member.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    private void InitializeDeletedRecordsPage()
    {
        if (_archiveEventsInitialized) return;
        _archiveEventsInitialized = true;

        gridDeletedStaff.CellContentClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var type = gridDeletedStaff.Rows[e.RowIndex].Cells["RecordType"].Value?.ToString();
            var id = gridDeletedStaff.Rows[e.RowIndex].Cells["RecordId"].Value?.ToString();
            var reference = gridDeletedStaff.Rows[e.RowIndex].Cells["Reference"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(type)) return;

            if (gridDeletedStaff.Columns[e.ColumnIndex].Name == "Restore")
            {
                if (type == "Staff" && int.TryParse(id, out var staffId)) RestoreStaff(staffId);
                else if (type == "Payment" && !string.IsNullOrWhiteSpace(reference)) RestorePayment(reference);
                else if (type == "Member" && !string.IsNullOrWhiteSpace(id)) RestoreMember(id);
                else if (type == "Daily gym visit" && !string.IsNullOrWhiteSpace(id)) RestoreVisit(id);
            }
            else if (gridDeletedStaff.Columns[e.ColumnIndex].Name == "RemoveForever")
            {
                if (type == "Staff" && int.TryParse(id, out var staffId)) PermanentlyDeleteStaff(staffId);
                else if (type == "Payment" && !string.IsNullOrWhiteSpace(reference)) PermanentlyDeletePayment(reference);
                else if (type == "Member" && !string.IsNullOrWhiteSpace(id)) PermanentlyDeleteMember(id);
                else if (type == "Daily gym visit" && !string.IsNullOrWhiteSpace(id)) PermanentlyDeleteVisit(id);
            }
        };
    }

    private void RestorePayment(string reference)
    {
        var payment = DataStore.Payments.FirstOrDefault(p => p.Reference == reference);
        if (payment is null) return;

        payment.IsDeleted = false;
        payment.DeletedAt = null;
        payment.DeletedBy = string.Empty;
        payment.Status = "Paid";
        DataStore.SavePayments();
        DataStore.RemoveArchiveRecord("Payment", payment.Reference);
        DataStore.RecalculateMemberBalances();
        DataStore.SaveMembers();
        LoadDeletedRecordsData();
        RefreshData();
    }

    private void PermanentlyDeletePayment(string reference)
    {
        if (MessageBox.Show("Permanently delete this payment? This cannot be undone.", "Delete permanently", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        DataStore.PermanentlyDeletePayment(reference);
        DataStore.RemoveArchiveRecord("Payment", reference);
        DataStore.RecalculateMemberBalances(); DataStore.SaveMembers();
        LoadDeletedRecordsData(); RefreshData();
    }

    private void RestoreStaff(int id)
    {
        var staff = DataStore.Staff.FirstOrDefault(s => s.Id == id);
        if (staff is null) return;
        staff.IsDeleted = false; staff.DeletedAt = null; staff.DeletedBy = ""; staff.Status = "Active";
        DataStore.RemoveArchiveRecord("Staff", staff.Username);
        var account = DataStore.Users.FirstOrDefault(u => u.Username.Equals(staff.Username, StringComparison.OrdinalIgnoreCase));
        if (account is not null) { account.IsDeleted = false; account.IsActive = true; account.DeletedAt = null; account.DeletedBy = ""; DataStore.SaveUsers(); }
        DataStore.SaveStaff(); LoadDeletedRecordsData(); LoadStaffUsersData();
    }

    private void PermanentlyDeleteStaff(int id)
    {
        if (MessageBox.Show("Permanently delete this staff account? This cannot be undone.", "Delete permanently", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var staff = DataStore.Staff.FirstOrDefault(s => s.Id == id);
        if (staff is null) return;
        DataStore.PermanentlyDeleteStaff(staff.Username);
        LoadDeletedRecordsData(); LoadStaffUsersData();
    }

    private void RestoreMember(string id)
    {
        var member = DataStore.Members.FirstOrDefault(m => m.Id == id);
        if (member is null) return;
        member.IsDeleted = false;
        member.DeletedAt = null;
        member.DeletedBy = "";
        DataStore.RemoveArchiveRecord("Member", member.Id);
        DataStore.SaveMembers(); LoadDeletedRecordsData(); RefreshData();
    }

    private void PermanentlyDeleteMember(string id)
    {
        if (MessageBox.Show("Permanently delete this member? This cannot be undone.", "Delete permanently", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        DataStore.PermanentlyDeleteMember(id);
        DataStore.RemoveArchiveRecord("Member", id);
        LoadDeletedRecordsData(); RefreshData();
    }

    private void RestoreVisit(string id)
    {
        var visit = DataStore.DailyGymVisits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return;
        visit.IsDeleted = false;
        visit.DeletedAt = null;
        visit.DeletedBy = "";
        if (visit.IsGymMember || visit.Fee == 0m)
        {
            DataStore.Payments.RemoveAll(p =>
                (!string.IsNullOrWhiteSpace(visit.PaymentReference) && p.Reference == visit.PaymentReference) ||
                (p.MemberId == visit.Id && string.Equals(p.PaymentType, "Daily Gym Pass", StringComparison.OrdinalIgnoreCase)));
            visit.PaymentReference = "";
        }
        else
        {
            var payment = DataStore.Payments.FirstOrDefault(p => p.Reference == visit.PaymentReference);
            if (payment is not null) { payment.IsDeleted = false; payment.DeletedAt = null; payment.DeletedBy = ""; payment.Status = "Paid"; }
        }
        DataStore.RemoveArchiveRecord("DailyGymVisit", visit.Id);
        DataStore.SaveDailyGymVisits(); DataStore.SavePayments(); LoadDeletedRecordsData(); RefreshData();
    }

    private void PermanentlyDeleteVisit(string id)
    {
        if (MessageBox.Show("Permanently delete this visit and its payment? This cannot be undone.", "Delete permanently", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var visit = DataStore.DailyGymVisits.FirstOrDefault(v => v.Id == id);
        if (visit is null) return;
        DataStore.PermanentlyDeleteDailyGymVisit(visit.Id, visit.PaymentReference);
        DataStore.RemoveArchiveRecord("DailyGymVisit", visit.Id);
        LoadDeletedRecordsData(); RefreshData();
    }

    private void LoadDeletedRecordsData()
    {
        InitializeDeletedRecordsPage();
        var rows = new List<object>();
        rows.AddRange(DataStore.Staff.Where(s => s.IsDeleted).Select(s => new { RecordType = "Staff", RecordId = s.Id.ToString(), Reference = "", Record = s.FullName, Details = s.Position, DeletedDate = (s.DeletedAt ?? DateTime.Now).ToString("MMM d, yyyy") }));
        rows.AddRange(DataStore.Payments.Where(p => p.IsDeleted || string.Equals(p.Status, "Deleted", StringComparison.OrdinalIgnoreCase)).Select(p => new { RecordType = "Payment", RecordId = "", Reference = p.Reference, Record = p.MemberName, Details = $"{p.Reference} · ₱{p.Amount:N2}", DeletedDate = (p.DeletedAt ?? DateTime.Now).ToString("MMM d, yyyy") }));
        rows.AddRange(DataStore.Members.Where(m => m.IsDeleted).Select(m => new { RecordType = "Member", RecordId = m.Id, Reference = "", Record = m.FullName, Details = m.Id, DeletedDate = (m.DeletedAt ?? DateTime.Now).ToString("MMM d, yyyy") }));
        rows.AddRange(DataStore.DailyGymVisits.Where(v => v.IsDeleted).Select(v => new { RecordType = "Daily gym visit", RecordId = v.Id, Reference = "", Record = v.FullName, Details = $"Visit: {v.VisitDate:MMM d, yyyy}", DeletedDate = (v.DeletedAt ?? DateTime.Now).ToString("MMM d, yyyy") }));
        gridDeletedStaff.DataSource = rows;
    }
}


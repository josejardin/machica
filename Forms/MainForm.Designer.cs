#nullable enable

namespace GymRegistrationApp.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private Panel sidebar = null!, topbar = null!, contentPanel = null!;
    private Label lblLogo = null!, lblPageTitle = null!, lblUser = null!, lblDbStatus = null!;
    private Button btnDashboard = null!, btnMembers = null!, btnPayments = null!, btnReports = null!, btnExpenses = null!, btnRefresh = null!, btnLogout = null!, btnAccountSettings = null!, btnDailyGym = null!, btnQRCode = null!;
    private Button btnActiveMemberships = null!, btnExpiredMemberships = null!, btnRenewalHistory = null!, btnUnpaidBalances = null!, btnIncomeReports = null!, btnStaffUsers = null!, btnDeletedRecords = null!;
    private ComboBox cmbMembersView = null!;
    private Panel pageDashboard = null!, pageMembers = null!, pagePayments = null!, pageReports = null!;
    private Panel pageActiveMemberships = null!, pageExpiredMemberships = null!, pageRenewalHistory = null!, pagePaymentHistory = null!, pageUnpaidBalances = null!, pageIncomeReports = null!, pageStaffUsers = null!, pageDeletedRecords = null!;
    private Label lblWelcome = null!, lblMemberCount = null!, lblActiveCount = null!, lblRevenue = null!, lblPaymentCount = null!;
    private DataGridView gridRecent = null!, gridMembers = null!, gridPayments = null!, gridActiveMemberships = null!, gridExpiredMemberships = null!, gridRenewalHistory = null!, gridPaymentHistory = null!, gridUnpaidBalances = null!;
    private Button btnAddMember = null!, btnEditMember = null!, btnDeleteMember = null!, btnMemberPass = null!, btnExportMembers = null!, btnExportPayments = null!, btnAddActiveMember = null!, btnSyncCloud = null!;
    private TextBox txtMemberSearch = null!, txtPaymentSearch = null!;
    private DateTimePicker dtpPaymentDate = null!;
    private Label lblMemberSearch = null!, lblPaymentSearch = null!, lblReportMembers = null!, lblReportRevenue = null!, lblReportActive = null!;
    // Income Reports page controls
    private Label lblIncomeToday = null!, lblIncomeMonthly = null!, lblIncomeRecorded = null!;
    // Staff Users page controls
    private Label lblTotalStaff = null!, lblActiveStaff = null!, lblTotalUsers = null!;
    private DataGridView gridStaffDirectory = null!;
    private TextBox txtUserSearch = null!;
    private Button btnCreateNewStaff = null!, btnActivityHistory = null!;
    // Deleted Records page controls
    private DataGridView gridDeletedStaff = null!;
    protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
    private void InitializeComponent()
    {
        sidebar = new Panel(); topbar = new Panel(); contentPanel = new Panel(); lblLogo = new Label(); lblPageTitle = new Label(); lblUser = new Label();
        btnDashboard = NavButton("▦  Overview", 145);
        btnMembers = NavButton("👥  All members", 195);
        // additional feature buttons are hidden from the sidebar and managed by the members dropdown
        btnActiveMemberships = NavButton("↻  Active memberships", 245);
        btnExpiredMemberships = NavButton("⌛  Expired memberships", 295);
        btnRenewalHistory = NavButton("⟳  Renewal history", 345);
        btnUnpaidBalances = NavButton("⚠  Unpaid balances", 445);
        btnIncomeReports = NavButton("📈  Income reports", 495);
        btnStaffUsers = NavButton("👤  Users", 545);
        btnDeletedRecords = NavButton("🗃  Archive", 585);
        btnPayments = NavButton("₱  Payments", 625); btnReports = NavButton("▤  Reports", 665); btnExpenses = NavButton("₱  Expenses", 705); btnDailyGym = NavButton("◷  Daily Gym", 0); btnQRCode = NavButton("▣  QR Attendance", 0); btnRefresh = new Button(); btnLogout = new Button(); btnAccountSettings = new Button();
        cmbMembersView = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.White, ForeColor = Color.FromArgb(29, 36, 52) };
        pageDashboard = new Panel(); pageMembers = new Panel(); pagePayments = new Panel(); pageReports = new Panel(); pageActiveMemberships = new Panel(); pageExpiredMemberships = new Panel(); pageRenewalHistory = new Panel(); pagePaymentHistory = new Panel(); pageUnpaidBalances = new Panel(); pageIncomeReports = new Panel(); pageStaffUsers = new Panel(); pageDeletedRecords = new Panel();
        lblWelcome = new Label(); lblMemberCount = new Label(); lblActiveCount = new Label(); lblRevenue = new Label(); lblPaymentCount = new Label(); gridRecent = MakeGrid(); gridMembers = MakeGrid(); gridPayments = MakeGrid();
        // add delete action to members grid
        gridMembers.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "🗑", UseColumnTextForButtonValue = true, Width = 40, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        gridMembers.CellContentClick += (s, e) => gridMembers_CellContentClick(s, e);
        // Initialize Staff controls
        btnCreateNewStaff = new Button(); btnActivityHistory = new Button(); gridStaffDirectory = MakeGrid();
        btnCreateNewStaff.Click += (s, e) => btnCreateNewStaff_Click(s, e);
        // Initialize additional grid controls for feature pages
        gridActiveMemberships = MakeGrid(); gridExpiredMemberships = MakeGrid(); gridRenewalHistory = MakeGrid(); gridPaymentHistory = MakeGrid(); gridUnpaidBalances = MakeGrid(); gridDeletedStaff = MakeGrid();
        // Initialize Income Reports labels
        lblIncomeToday = new Label(); lblIncomeMonthly = new Label(); lblIncomeRecorded = new Label();
        // Initialize Staff Users labels
        lblTotalStaff = new Label(); lblActiveStaff = new Label();
        // Initialize additional buttons
        btnAddActiveMember = new Button();
        btnAddMember = ActionButton("+  Register Member");
        btnEditMember = ActionButton("✎  Edit");
        btnMemberPass = new Button
        {
            Text = "▣ Member Pass",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatAppearance = { BorderSize = 0 },
            Cursor = Cursors.Hand
        };
        btnDeleteMember = new Button
        {
            Text = "🗑 Delete",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(190, 54, 54),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatAppearance = { BorderSize = 0 },
            Cursor = Cursors.Hand
        };
        btnExportMembers = new Button
        {
            Text = "Export CSV",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(29, 36, 52),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatAppearance = { BorderSize = 1, BorderColor = Color.Gainsboro },
            Cursor = Cursors.Hand
        };
        btnExportPayments = new Button
        {
            Text = "Export CSV",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(29, 36, 52),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatAppearance = { BorderSize = 1, BorderColor = Color.Gainsboro },
            Cursor = Cursors.Hand
        };
        txtMemberSearch = new TextBox(); txtPaymentSearch = new TextBox(); dtpPaymentDate = new DateTimePicker(); lblMemberSearch = new Label(); lblPaymentSearch = new Label(); lblReportMembers = new Label(); lblReportRevenue = new Label(); lblReportActive = new Label();
        txtMemberSearch.TextChanged += txtMemberSearch_TextChanged;
        txtPaymentSearch.TextChanged += txtPaymentSearch_TextChanged;
        dtpPaymentDate.ValueChanged += dtpPaymentDate_ValueChanged;
        sidebar.SuspendLayout(); topbar.SuspendLayout(); contentPanel.SuspendLayout(); pageDashboard.SuspendLayout(); pageMembers.SuspendLayout(); pagePayments.SuspendLayout(); pageReports.SuspendLayout(); pageIncomeReports.SuspendLayout(); pageStaffUsers.SuspendLayout(); pageDeletedRecords.SuspendLayout(); SuspendLayout();
        sidebar.BackColor = Color.FromArgb(18, 23, 38); sidebar.Dock = DockStyle.Left; sidebar.Width = 225;
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = sidebar.BackColor, Padding = new Padding(10, 12, 10, 8) };
        lblLogo.Text = "UNIVERSAL GYM"; lblLogo.Font = new Font("Segoe UI", 12, FontStyle.Bold); lblLogo.ForeColor = Color.White; lblLogo.Dock = DockStyle.Top; lblLogo.Height = 42; lblLogo.TextAlign = ContentAlignment.MiddleCenter;
        var lblSub = new Label { Text = "GYM MANAGEMENT", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(255, 132, 35), Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.TopCenter };
        headerPanel.Controls.Add(lblSub); headerPanel.Controls.Add(lblLogo);
        // style and add navigation buttons in one scrollable, evenly spaced column
        var navigationPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = sidebar.BackColor, Padding = new Padding(0, 8, 0, 8) };
        var navigationButtons = new[] { btnDashboard, btnMembers, btnDailyGym, btnQRCode, btnIncomeReports, btnPayments, btnExpenses, btnReports, btnStaffUsers, btnDeletedRecords };
        for (var i = 0; i < navigationButtons.Length; i++)
        {
            var b = navigationButtons[i]; b.Width = 205; b.Height = 42; b.Left = 10; b.Top = i * 46; b.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.TextAlign = ContentAlignment.MiddleLeft; b.Padding = new Padding(14, 0, 0, 0); b.Font = new Font("Segoe UI", 10, FontStyle.Bold); b.ForeColor = Color.FromArgb(210, 215, 226); b.BackColor = sidebar.BackColor; b.Cursor = Cursors.Hand; navigationPanel.Controls.Add(b);
        }
        // wire up click handlers
        btnDashboard.Click += btnDashboard_Click;
        btnMembers.Click += btnMembers_Click;
        btnDailyGym.Click += (_, _) => OpenDailyGym();
        btnQRCode.Click += (_, _) => OpenQRCodeManagement();
        btnIncomeReports.Click += (s, e) => ShowPage("IncomeReports");
        btnStaffUsers.Click += (s, e) => ShowPage("StaffUsers");
        btnDeletedRecords.Click += (s, e) => ShowPage("DeletedRecords");
        btnPayments.Click += btnPayments_Click;
        btnExpenses.Click += btnExpenses_Click;
        btnReports.Click += btnReports_Click;
        // bottom area for signed-in info and sign out button — docked so it's always visible
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = sidebar.BackColor, Padding = new Padding(0, 6, 0, 8) };
        var lblSigned = new Label { Text = "SIGNED IN AS", ForeColor = Color.Gray, Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Top, Height = 18, Padding = new Padding(12, 0, 0, 0) };
        lblUser.Text = "Owner"; lblUser.ForeColor = Color.White; lblUser.Font = new Font("Segoe UI", 9, FontStyle.Bold); lblUser.AutoSize = false; lblUser.AutoEllipsis = true; lblUser.TextAlign = ContentAlignment.MiddleLeft; lblUser.Dock = DockStyle.Top; lblUser.Height = 30; lblUser.Padding = new Padding(12, 0, 8, 0);
        btnAccountSettings.Text = "⚙  Account settings"; btnAccountSettings.Dock = DockStyle.Bottom; btnAccountSettings.Height = 38; btnAccountSettings.FlatStyle = FlatStyle.Flat; btnAccountSettings.FlatAppearance.BorderColor = Color.FromArgb(70, 76, 91); btnAccountSettings.ForeColor = Color.White; btnAccountSettings.BackColor = sidebar.BackColor; btnAccountSettings.Click += btnAccountSettings_Click;
        btnLogout.Text = "↪  Sign out"; btnLogout.Dock = DockStyle.Bottom; btnLogout.Height = 42; btnLogout.FlatStyle = FlatStyle.Flat; btnLogout.FlatAppearance.BorderColor = Color.FromArgb(70, 76, 91); btnLogout.ForeColor = Color.White; btnLogout.BackColor = sidebar.BackColor; btnLogout.Click += btnLogout_Click;
        bottomPanel.Controls.Add(btnLogout); bottomPanel.Controls.Add(btnAccountSettings); bottomPanel.Controls.Add(lblUser); bottomPanel.Controls.Add(lblSigned);
        sidebar.Controls.Add(bottomPanel); sidebar.Controls.Add(navigationPanel); sidebar.Controls.Add(headerPanel);
        topbar.Dock = DockStyle.Top; topbar.Height = 76; topbar.BackColor = Color.White; topbar.Padding = new Padding(28, 0, 25, 0);
        lblPageTitle.Text = "Dashboard"; lblPageTitle.Font = new Font("Segoe UI", 19, FontStyle.Bold); lblPageTitle.ForeColor = Color.FromArgb(29, 36, 52); lblPageTitle.Location = new Point(28, 18); lblPageTitle.Size = new Size(400, 42);
        lblDbStatus = new Label
        {
            Text = "🟢 MySQL Online",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 174, 96),
            BackColor = Color.FromArgb(235, 247, 238),
            Padding = new Padding(10, 6, 10, 6),
            AutoSize = true,
            Location = new Point(720, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSyncCloud = new Button
        {
            Text = "☁️ Sync Cloud",
            Size = new Size(130, 36),
            Location = new Point(885, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSyncCloud.FlatAppearance.BorderSize = 0;
        btnSyncCloud.Click += async (_, _) => await TriggerCloudSyncAsync();
        btnRefresh.Text = "↻ Refresh"; btnRefresh.Size = new Size(105, 36); btnRefresh.Location = new Point(1030, 20); btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnRefresh.FlatStyle = FlatStyle.Flat; btnRefresh.FlatAppearance.BorderColor = Color.Gainsboro; btnRefresh.BackColor = Color.White; btnRefresh.Click += btnRefresh_Click;
        topbar.Controls.AddRange(new Control[] { lblPageTitle, lblDbStatus, btnSyncCloud, btnRefresh });
        contentPanel.Dock = DockStyle.Fill; contentPanel.BackColor = Color.FromArgb(246, 247, 250); contentPanel.Padding = new Padding(25);
        pageDashboard.Dock = DockStyle.Fill; pageDashboard.BackColor = contentPanel.BackColor;
        // create additional pages (placeholders)
        pageActiveMemberships = new Panel(); pageExpiredMemberships = new Panel(); pageRenewalHistory = new Panel(); pagePaymentHistory = new Panel(); pageUnpaidBalances = new Panel(); pageIncomeReports = new Panel(); pageStaffUsers = new Panel(); pageDeletedRecords = new Panel();
        // initialize generic feature pages (placeholders)
        foreach (var p in new[] { pageIncomeReports, pageStaffUsers, pageDeletedRecords }) { p.Dock = DockStyle.Fill; p.BackColor = contentPanel.BackColor; p.Visible = false; p.Controls.Add(new Label { Text = "", ForeColor = Color.DimGray, Font = new Font("Segoe UI", 12), Location = new Point(0, 0) }); }

        // Renewal history page - custom layout
        pageRenewalHistory.Dock = DockStyle.Fill; pageRenewalHistory.BackColor = contentPanel.BackColor; pageRenewalHistory.Visible = false; pageRenewalHistory.AutoScroll = true;
        var lblRenewalHeader = new Label { Text = "Membership renewals", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 12), AutoSize = true };
        var lblRenewalSubtitle = new Label { Text = "A complete record of membership renewals.", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 42), AutoSize = true };
        var lblRecordsSection = new Label { Text = "Renewal records", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 82), AutoSize = true };
        var lblRecordCount = new Label { Text = "3 recent records", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 108), AutoSize = true };
        gridRenewalHistory = MakeGrid(); gridRenewalHistory.Location = new Point(12, 138); gridRenewalHistory.Size = new Size(950, 350); gridRenewalHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridRenewalHistory.AutoGenerateColumns = false; gridRenewalHistory.DefaultCellStyle.WrapMode = DataGridViewTriState.True; gridRenewalHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        gridRenewalHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "REFERENCE", DataPropertyName = "Reference", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridRenewalHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Member", HeaderText = "MEMBER", DataPropertyName = "Member", Width = 210, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridRenewalHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "RenewalDate", HeaderText = "RENEWAL DATE", DataPropertyName = "RenewalDate", Width = 135, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridRenewalHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlanChange", HeaderText = "PLAN CHANGE", DataPropertyName = "PlanChange", Width = 210, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        gridRenewalHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "AMOUNT", DataPropertyName = "Amount", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True, Alignment = DataGridViewContentAlignment.MiddleRight } });
        gridRenewalHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcessedBy", HeaderText = "PROCESSED BY", DataPropertyName = "ProcessedBy", Width = 170, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        pageRenewalHistory.Controls.AddRange(new Control[] { lblRenewalHeader, lblRenewalSubtitle, lblRecordsSection, lblRecordCount, gridRenewalHistory });

        // Active memberships page - custom layout
        pageActiveMemberships.Dock = DockStyle.Fill; pageActiveMemberships.BackColor = contentPanel.BackColor; pageActiveMemberships.Visible = false;
        // header and add button
        var lblActiveHeader = new Label { Text = "Active memberships", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 12), AutoSize = true };
        btnAddActiveMember = ActionButton("+ New member"); btnAddActiveMember.Location = new Point(820, 10); btnAddActiveMember.Width = 140; btnAddActiveMember.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAddActiveMember.Click += (s, e) => btnAddMember_Click(s, e);

        // active memberships grid
        gridActiveMemberships = MakeGrid(); gridActiveMemberships.Location = new Point(12, 56); gridActiveMemberships.Size = new Size(950, 420); gridActiveMemberships.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridActiveMemberships.AutoGenerateColumns = false; gridActiveMemberships.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        // hidden id column for identification
        gridActiveMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
        gridActiveMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Member", DataPropertyName = "FullName", Width = 280, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridActiveMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Plan", HeaderText = "Membership", DataPropertyName = "Plan", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridActiveMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "Expiry", DataPropertyName = "ExpiryDate", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridActiveMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fee", HeaderText = "Balance", DataPropertyName = "Fee", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridActiveMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        // action columns: renew, edit, delete using glyphs
        gridActiveMemberships.CellContentClick += (s, e) => gridActiveMemberships_CellContentClick(s, e);

        pageActiveMemberships.Controls.AddRange(new Control[] { lblActiveHeader, btnAddActiveMember, gridActiveMemberships });

        // Expired memberships page - custom layout
        pageExpiredMemberships.Dock = DockStyle.Fill; pageExpiredMemberships.BackColor = contentPanel.BackColor; pageExpiredMemberships.Visible = false;
        var lblExpiredHeader = new Label { Text = "Expired memberships", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 12), AutoSize = true };
        var btnAddExpiredMember = ActionButton("+ New member"); btnAddExpiredMember.Location = new Point(820, 10); btnAddExpiredMember.Width = 140; btnAddExpiredMember.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAddExpiredMember.Click += (s, e) => btnAddMember_Click(s, e);
        gridExpiredMemberships = MakeGrid(); gridExpiredMemberships.Location = new Point(12, 56); gridExpiredMemberships.Size = new Size(950, 420); gridExpiredMemberships.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridExpiredMemberships.AutoGenerateColumns = false; gridExpiredMemberships.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        // hidden id column
        gridExpiredMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
        gridExpiredMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Member", DataPropertyName = "FullName", Width = 280, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridExpiredMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Plan", HeaderText = "Membership", DataPropertyName = "Plan", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridExpiredMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpiryDate", HeaderText = "Expiry", DataPropertyName = "ExpiryDate", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridExpiredMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fee", HeaderText = "Balance", DataPropertyName = "Fee", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridExpiredMemberships.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridExpiredMemberships.Columns.Add(new DataGridViewButtonColumn { Name = "Renew", HeaderText = "ACTIONS", Text = "Renew Membership", UseColumnTextForButtonValue = true, Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        gridExpiredMemberships.CellContentClick += (s, e) => gridExpiredMemberships_CellContentClick(s, e);
        pageExpiredMemberships.Controls.AddRange(new Control[] { lblExpiredHeader, btnAddExpiredMember, gridExpiredMemberships });
        lblWelcome.Text = "Good day! Here's your gym overview."; lblWelcome.Font = new Font("Segoe UI", 11); lblWelcome.ForeColor = Color.Gray; lblWelcome.Location = new Point(0, 0); lblWelcome.Size = new Size(700, 30);
        MakeMetric(lblMemberCount, "TOTAL MEMBERS", "0", 0); MakeMetric(lblActiveCount, "ACTIVE MEMBERS", "0", 275); MakeMetric(lblRevenue, "TOTAL REVENUE", "₱0.00", 550); MakeMetric(lblPaymentCount, "PAYMENT RECORDS", "0", 825);
        var recentTitle = new Label { Text = "Recent Members", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(30, 37, 54), Location = new Point(0, 180), Size = new Size(300, 30) };
        // configure recent grid columns to show all member fields
        ConfigureMemberColumns(gridRecent);
        gridRecent.Location = new Point(0, 220); gridRecent.Size = new Size(1080, 400); gridRecent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pageDashboard.Controls.AddRange(new Control[] { lblWelcome, lblMemberCount, lblActiveCount, lblRevenue, lblPaymentCount, recentTitle, gridRecent });
        pageMembers.Dock = DockStyle.Fill; pageMembers.BackColor = contentPanel.BackColor;
        lblMemberSearch.Text = "Search members"; lblMemberSearch.Location = new Point(25, 8); lblMemberSearch.Size = new Size(120, 24); lblMemberSearch.ForeColor = Color.Gray;
        txtMemberSearch.Location = new Point(25, 34); txtMemberSearch.Size = new Size(260, 32); txtMemberSearch.Font = new Font("Segoe UI", 10); txtMemberSearch.TextChanged += txtMemberSearch_TextChanged;
        cmbMembersView.Location = new Point(295, 33); cmbMembersView.Size = new Size(185, 32); cmbMembersView.Items.AddRange(new object[] { "All Members", "Active Members", "Expired Members", "Renewal History" });
        cmbMembersView.SelectedIndex = 0; cmbMembersView.SelectedIndexChanged += cmbMembersView_SelectedIndexChanged;
        btnAddMember.Location = new Point(490, 27); btnAddMember.Size = new Size(140, 38); btnAddMember.Click += btnAddMember_Click;
        btnEditMember.Location = new Point(638, 27); btnEditMember.Size = new Size(95, 38); btnEditMember.Click += btnEditMember_Click;
        btnMemberPass.Location = new Point(741, 27); btnMemberPass.Size = new Size(145, 38); btnMemberPass.Click += btnMemberPass_Click;
        btnDeleteMember.Location = new Point(894, 27); btnDeleteMember.Size = new Size(95, 38); btnDeleteMember.BackColor = Color.FromArgb(190, 54, 54); btnDeleteMember.Click += btnDeleteMember_Click;
        btnExportMembers.Location = new Point(25, 78); btnExportMembers.Size = new Size(105, 32); btnExportMembers.Click += btnExportMembers_Click;
        // configure members grid columns and layout
        ConfigureMemberColumns(gridMembers);
        gridMembers.Location = new Point(25, 125); gridMembers.Size = new Size(1000, 615); gridMembers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        gridActiveMemberships.Location = new Point(25, 125); gridActiveMemberships.Size = new Size(1000, 615); gridActiveMemberships.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridActiveMemberships.Visible = false;
        gridExpiredMemberships.Location = new Point(25, 125); gridExpiredMemberships.Size = new Size(1000, 615); gridExpiredMemberships.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridExpiredMemberships.Visible = false;
        gridRenewalHistory.Location = new Point(25, 125); gridRenewalHistory.Size = new Size(1000, 615); gridRenewalHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridRenewalHistory.Visible = false;
        gridPaymentHistory.Location = new Point(25, 125); gridPaymentHistory.Size = new Size(1000, 615); gridPaymentHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridPaymentHistory.Visible = false;
        pageMembers.Controls.AddRange(new Control[] { lblMemberSearch, txtMemberSearch, cmbMembersView, btnAddMember, btnEditMember, btnMemberPass, btnDeleteMember, btnExportMembers, gridMembers, gridActiveMemberships, gridExpiredMemberships, gridRenewalHistory, gridPaymentHistory });
        pagePayments.Dock = DockStyle.Fill; pagePayments.BackColor = contentPanel.BackColor;
        lblPaymentSearch.Text = "Search payment records"; lblPaymentSearch.Location = new Point(0, 8); lblPaymentSearch.Size = new Size(200, 24); lblPaymentSearch.ForeColor = Color.Gray;
        txtPaymentSearch.Location = new Point(0, 34); txtPaymentSearch.Size = new Size(330, 32); txtPaymentSearch.Font = new Font("Segoe UI", 10); txtPaymentSearch.TextChanged += txtPaymentSearch_TextChanged;
        var lblPaymentDate = new Label { Text = "Payment date", Location = new Point(360, 8), Size = new Size(160, 24), ForeColor = Color.Gray };
        dtpPaymentDate.Location = new Point(360, 34); dtpPaymentDate.Size = new Size(180, 32); dtpPaymentDate.Format = DateTimePickerFormat.Short; dtpPaymentDate.Value = DateTime.Today;
        btnExportPayments.Location = new Point(1010, 27); btnExportPayments.Size = new Size(105, 38); btnExportPayments.Click += btnExportPayments_Click;
        gridPayments.Location = new Point(0, 90); gridPayments.Size = new Size(1120, 535); gridPayments.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pagePayments.Controls.AddRange(new Control[] { lblPaymentSearch, txtPaymentSearch, lblPaymentDate, dtpPaymentDate, btnExportPayments, gridPayments });
        pageReports.Dock = DockStyle.Fill; pageReports.BackColor = contentPanel.BackColor;
        lblReportMembers.Text = "0"; lblReportMembers.Font = new Font("Segoe UI", 30, FontStyle.Bold); lblReportMembers.Location = new Point(30, 70); lblReportMembers.Size = new Size(250, 55);
        lblReportActive.Text = "0"; lblReportActive.Font = new Font("Segoe UI", 30, FontStyle.Bold); lblReportActive.Location = new Point(390, 70); lblReportActive.Size = new Size(250, 55);
        lblReportRevenue.Text = "₱0.00"; lblReportRevenue.Font = new Font("Segoe UI", 30, FontStyle.Bold); lblReportRevenue.Location = new Point(750, 70); lblReportRevenue.Size = new Size(300, 55);
        pageReports.Controls.AddRange(new Control[] { new Label { Text = "MEMBER SUMMARY", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(30, 35), Size = new Size(250, 25) }, lblReportMembers, new Label { Text = "ACTIVE MEMBERS", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(390, 35), Size = new Size(250, 25) }, lblReportActive, new Label { Text = "RECORDED REVENUE", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(750, 35), Size = new Size(280, 25) }, lblReportRevenue, new Label { Text = "Reports use the saved registration and payment records. Export CSV from Members or Payments to open data in Excel.", Font = new Font("Segoe UI", 11), ForeColor = Color.DimGray, Location = new Point(30, 170), Size = new Size(900, 60) } });

        // Payment history page - custom layout
        pagePaymentHistory.Dock = DockStyle.Fill; pagePaymentHistory.BackColor = contentPanel.BackColor; pagePaymentHistory.Visible = false; pagePaymentHistory.AutoScroll = true;
        var lblPaymentHistoryHeader = new Label { Text = "Payment history", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 12), AutoSize = true };
        var lblPaymentHistorySubtitle = new Label { Text = "Review all member payments and transactions.", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 42), AutoSize = true };
        var lblPaymentRecordsCount = new Label { Text = "4 recent records", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 82), AutoSize = true };
        gridPaymentHistory = MakeGrid(); gridPaymentHistory.Location = new Point(12, 110); gridPaymentHistory.Size = new Size(950, 350); gridPaymentHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridPaymentHistory.AutoGenerateColumns = false; gridPaymentHistory.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "REFERENCE", DataPropertyName = "Reference", Width = 220, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "MemberName", HeaderText = "MEMBER", DataPropertyName = "MemberName", Width = 240, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "PAYMENT DATE", DataPropertyName = "Date", Width = 180, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "METHOD", DataPropertyName = "Method", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentType", HeaderText = "TYPE", DataPropertyName = "PaymentType", Width = 160, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "CollectedBy", HeaderText = "COLLECTED BY", DataPropertyName = "CollectedBy", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "AMOUNT", DataPropertyName = "Amount", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", DataPropertyName = "Status", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        gridPaymentHistory.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "🗑", UseColumnTextForButtonValue = true, Width = 40, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        gridPaymentHistory.CellContentClick += (s, e) => gridPaymentHistory_CellContentClick(s, e);
        pagePaymentHistory.Controls.AddRange(new Control[] { lblPaymentHistoryHeader, lblPaymentHistorySubtitle, lblPaymentRecordsCount, gridPaymentHistory });

        // Income Reports page - custom layout
        pageIncomeReports.Dock = DockStyle.Fill; pageIncomeReports.BackColor = contentPanel.BackColor; pageIncomeReports.Visible = false; pageIncomeReports.AutoScroll = true;

        // Users page - custom layout
        pageStaffUsers.Dock = DockStyle.Fill; pageStaffUsers.BackColor = contentPanel.BackColor; pageStaffUsers.Visible = false; pageStaffUsers.AutoScroll = true;
        var lblStaffHeader = new Label { Text = "Users", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 12), AutoSize = true };
        var lblStaffSubtitle = new Label { Text = "Create and manage user accounts and access.", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 42), AutoSize = true };
        txtUserSearch = new TextBox { PlaceholderText = "Search name, email, phone, or role", Font = new Font("Segoe UI", 9), Location = new Point(12, 190), Size = new Size(340, 28), Anchor = AnchorStyles.Top | AnchorStyles.Left };
        txtUserSearch.TextChanged += (_, _) => LoadStaffUsersData();

        btnCreateNewStaff = ActionButton("+ New member");
        btnCreateNewStaff.Location = new Point(950, 15); btnCreateNewStaff.Size = new Size(160, 36);
        btnCreateNewStaff.Click += (s, e) => btnCreateNewStaff_Click(s, e);

        var cardTotalStaff = new Panel { BackColor = Color.White, Location = new Point(12, 78), Size = new Size(220, 104), BorderStyle = BorderStyle.FixedSingle };
        lblTotalStaff = new Label { Text = "0", Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(18, 12), AutoSize = true };
        var lblTotalStaffLabel = new Label { Text = "ADMIN", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(104, 112, 124), Location = new Point(18, 69), AutoSize = true };
        cardTotalStaff.Controls.AddRange(new Control[] { lblTotalStaff, lblTotalStaffLabel });

        var cardActiveStaff = new Panel { BackColor = Color.White, Location = new Point(246, 78), Size = new Size(220, 104), BorderStyle = BorderStyle.FixedSingle };
        lblActiveStaff = new Label { Text = "0", Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Color.FromArgb(34, 177, 76), Location = new Point(18, 12), AutoSize = true };
        var lblActiveStaffLabel = new Label { Text = "STAFF", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(104, 112, 124), Location = new Point(18, 69), AutoSize = true };
        cardActiveStaff.Controls.AddRange(new Control[] { lblActiveStaff, lblActiveStaffLabel });

        var cardTotalUsers = new Panel { BackColor = Color.White, Location = new Point(480, 78), Size = new Size(220, 104), BorderStyle = BorderStyle.FixedSingle };
        lblTotalUsers = new Label { Text = "0", Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Color.FromArgb(255, 132, 35), Location = new Point(18, 12), AutoSize = true };
        var lblTotalUsersLabel = new Label { Text = "TOTAL USERS", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(104, 112, 124), Location = new Point(18, 69), AutoSize = true };
        cardTotalUsers.Controls.AddRange(new Control[] { lblTotalUsers, lblTotalUsersLabel });

        btnCreateNewStaff.Tag = "summary";
        btnCreateNewStaff.Text = "+ Create new account";
        btnCreateNewStaff.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        btnCreateNewStaff.Location = new Point(720, 78); btnCreateNewStaff.Size = new Size(190, 40);
        btnCreateNewStaff.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnCreateNewStaff.FlatStyle = FlatStyle.Flat;
        btnCreateNewStaff.BackColor = Color.FromArgb(255, 132, 35);
        btnCreateNewStaff.ForeColor = Color.White;
        btnCreateNewStaff.FlatAppearance.BorderSize = 0;

        btnActivityHistory = ActionButton("View activity history");
        btnActivityHistory.Location = new Point(720, 124); btnActivityHistory.Size = new Size(190, 40);
        btnActivityHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnActivityHistory.Click += (s, e) => btnActivityHistory_Click(s, e);

        var lblDirectoryTitle = new Label { Text = "Users directory", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(380, 187), AutoSize = true };
        var lblDirectorySubtitle = new Label { Text = "Admin and staff accounts", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(380, 211), AutoSize = true };

         gridStaffDirectory = MakeGrid(); gridStaffDirectory.Location = new Point(12, 246); gridStaffDirectory.Size = new Size(1140, 330); gridStaffDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; gridStaffDirectory.AutoGenerateColumns = false; gridStaffDirectory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; gridStaffDirectory.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; gridStaffDirectory.RowTemplate.Height = 44; gridStaffDirectory.ScrollBars = ScrollBars.Vertical; gridStaffDirectory.GridColor = Color.FromArgb(220, 224, 230); gridStaffDirectory.DefaultCellStyle.WrapMode = DataGridViewTriState.True; gridStaffDirectory.DefaultCellStyle.Font = new Font("Segoe UI", 9); gridStaffDirectory.DefaultCellStyle.ForeColor = Color.FromArgb(29, 36, 52); gridStaffDirectory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 231, 207); gridStaffDirectory.DefaultCellStyle.SelectionForeColor = Color.FromArgb(29, 36, 52); gridStaffDirectory.DefaultCellStyle.Padding = new Padding(5, 4, 5, 4); gridStaffDirectory.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft; gridStaffDirectory.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False; gridStaffDirectory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 132, 35); gridStaffDirectory.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black; gridStaffDirectory.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 132, 35); gridStaffDirectory.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black; gridStaffDirectory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); gridStaffDirectory.ColumnHeadersHeight = 38; gridStaffDirectory.EnableHeadersVisualStyles = false;
        gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffInitials", HeaderText = "", DataPropertyName = "StaffInitials", Width = 42, MinimumWidth = 42, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.White, ForeColor = Color.FromArgb(35, 40, 48), Font = new Font("Segoe UI", 10, FontStyle.Bold), WrapMode = DataGridViewTriState.False } });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffName", HeaderText = "USER", DataPropertyName = "StaffName", Width = 180, MinimumWidth = 150, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(176, 92, 0), WrapMode = DataGridViewTriState.True } });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "ROLE", DataPropertyName = "Role", Width = 75, MinimumWidth = 75, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9, FontStyle.Bold), WrapMode = DataGridViewTriState.False } });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Contact", HeaderText = "CONTACT", DataPropertyName = "Contact", Width = 220, MinimumWidth = 180, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(176, 92, 0), WrapMode = DataGridViewTriState.True } });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Position", HeaderText = "POSITION", DataPropertyName = "Position", Width = 155, MinimumWidth = 140, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(176, 92, 0), WrapMode = DataGridViewTriState.True } });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Joined", HeaderText = "JOINED", DataPropertyName = "Joined", Width = 125, MinimumWidth = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(176, 92, 0), WrapMode = DataGridViewTriState.False } });
           gridStaffDirectory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", DataPropertyName = "Status", Width = 90, MinimumWidth = 85, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(33, 136, 72), WrapMode = DataGridViewTriState.False } });
          gridStaffDirectory.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "EDIT", Text = "✎", UseColumnTextForButtonValue = true, Width = 70, MinimumWidth = 70, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(80, 100, 130), Font = new Font("Segoe UI", 9, FontStyle.Bold) } });
          gridStaffDirectory.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "DELETE", Text = "🗑", UseColumnTextForButtonValue = true, Width = 80, MinimumWidth = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(90, 96, 105), Font = new Font("Segoe UI", 9) } });
        gridStaffDirectory.CellFormatting += gridStaffDirectory_CellFormatting;
        gridStaffDirectory.CellPainting += gridStaffDirectory_CellPainting;
        gridStaffDirectory.CellClick += (s, e) => gridStaffDirectory_CellContentClick(s, e);
         var lblNoStaff = new Label { Text = "", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 400), Size = new Size(950, 60), TextAlign = ContentAlignment.MiddleCenter };

          pageStaffUsers.Controls.AddRange(new Control[] { lblStaffHeader, lblStaffSubtitle, cardTotalStaff, cardActiveStaff, cardTotalUsers, txtUserSearch, btnCreateNewStaff, btnActivityHistory, lblDirectoryTitle, lblDirectorySubtitle, gridStaffDirectory, lblNoStaff });

        // Deleted Records page - custom layout
        pageDeletedRecords.Dock = DockStyle.Fill; pageDeletedRecords.BackColor = contentPanel.BackColor; pageDeletedRecords.Visible = false; pageDeletedRecords.AutoScroll = true;
        var lblDeletedHeader = new Label { Text = "Archive", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 12), AutoSize = true };
        var lblDeletedSubtitle = new Label { Text = "Restore archived records or permanently delete them.", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 42), AutoSize = true };

        var lblDeletedTableTitle = new Label { Text = "Archived records", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(29, 36, 52), Location = new Point(12, 82), AutoSize = true };
        var lblDeletedTableDesc = new Label { Text = "All archived staff, payments, members, and daily visits are listed together.", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(130, 136, 147), Location = new Point(12, 105), AutoSize = true };

        gridDeletedStaff = MakeGrid(); gridDeletedStaff.Location = new Point(12, 135); gridDeletedStaff.Size = new Size(1140, 520); gridDeletedStaff.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom; gridDeletedStaff.AutoGenerateColumns = false; gridDeletedStaff.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; gridDeletedStaff.ColumnHeadersHeight = 42; gridDeletedStaff.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True; gridDeletedStaff.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        gridDeletedStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecordId", HeaderText = "", DataPropertyName = "RecordId", Visible = false });
        gridDeletedStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "", DataPropertyName = "Reference", Visible = false });
        gridDeletedStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecordType", HeaderText = "TYPE", DataPropertyName = "RecordType", Width = 145, MinimumWidth = 120 });
        gridDeletedStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "Record", HeaderText = "RECORD", DataPropertyName = "Record", Width = 250, MinimumWidth = 180 });
        gridDeletedStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "DETAILS", DataPropertyName = "Details", Width = 280, MinimumWidth = 200 });
        gridDeletedStaff.Columns.Add(new DataGridViewTextBoxColumn { Name = "DeletedDate", HeaderText = "ARCHIVED ON", DataPropertyName = "DeletedDate", Width = 150, MinimumWidth = 125 });
        gridDeletedStaff.Columns.Add(new DataGridViewButtonColumn { Name = "Restore", HeaderText = "RESTORE", Text = "Restore", UseColumnTextForButtonValue = true, Width = 110, MinimumWidth = 95, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(34, 177, 76), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) } });
        gridDeletedStaff.Columns.Add(new DataGridViewButtonColumn { Name = "RemoveForever", HeaderText = "DELETE PERMANENTLY", Text = "Delete permanently", UseColumnTextForButtonValue = true, Width = 175, MinimumWidth = 150, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, BackColor = Color.FromArgb(220, 20, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), WrapMode = DataGridViewTriState.True } });

        pageDeletedRecords.Controls.AddRange(new Control[] { lblDeletedHeader, lblDeletedSubtitle, lblDeletedTableTitle, lblDeletedTableDesc, gridDeletedStaff });

        contentPanel.Controls.AddRange(new Control[] { pageDashboard, pageMembers, pagePayments, pageReports, pageActiveMemberships, pageExpiredMemberships, pageRenewalHistory, pagePaymentHistory, pageIncomeReports, pageStaffUsers, pageDeletedRecords });
        Controls.Add(contentPanel); Controls.Add(topbar); Controls.Add(sidebar); AutoScaleMode = AutoScaleMode.Font; ClientSize = new Size(1400, 820); MinimumSize = new Size(1120, 700); StartPosition = FormStartPosition.CenterScreen; Text = "UniversalGym | Gym Management"; BackColor = Color.FromArgb(246, 247, 250);
        sidebar.ResumeLayout(false); topbar.ResumeLayout(false); contentPanel.ResumeLayout(false); pageDashboard.ResumeLayout(false); pageMembers.ResumeLayout(false); pageMembers.PerformLayout(); pagePayments.ResumeLayout(false); pagePayments.PerformLayout(); pageReports.ResumeLayout(false); pageIncomeReports.ResumeLayout(false); pageStaffUsers.ResumeLayout(false); pageDeletedRecords.ResumeLayout(false); ResumeLayout(false);
    }
    private static Button NavButton(string text, int top) => new() { Text = text, Top = top, Height = 42 };
    private static Button ActionButton(string text) => new() { Text = text, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(255, 132, 35), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand };
    private static DataGridView MakeGrid() => new() { BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, AllowUserToResizeColumns = false, AllowUserToResizeRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 38, RowTemplate = { Height = 34 }, ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(238, 240, 245), ForeColor = Color.FromArgb(40, 47, 62), Font = new Font("Segoe UI", 9, FontStyle.Bold), WrapMode = DataGridViewTriState.False }, DefaultCellStyle = { Font = new Font("Segoe UI", 9), SelectionBackColor = Color.FromArgb(255, 224, 198), SelectionForeColor = Color.FromArgb(30, 30, 30), WrapMode = DataGridViewTriState.True } };
    private static void ConfigureMemberColumns(DataGridView grid)
    {
        grid.AutoGenerateColumns = false;
        grid.Columns.Clear();

        // Configure grid to not wrap text
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        // Fixed widths calculated to fit typical data without overflow and wrapping
        // Total width: ~1300px (fits within standard 1400px+ grid with scrollbar consideration)
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "Member ID", Width = 180, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FullName", HeaderText = "Full Name", Width = 240, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = "Phone", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Email", HeaderText = "Email", Width = 200, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Plan", HeaderText = "Plan", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Fee", HeaderText = "Fee", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, WrapMode = DataGridViewTriState.False } });

        var sd = new DataGridViewTextBoxColumn { DataPropertyName = "StartDate", HeaderText = "Start Date", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } };
        sd.DefaultCellStyle.Format = "yyyy-MM-dd";
        grid.Columns.Add(sd);

        var ed = new DataGridViewTextBoxColumn { DataPropertyName = "ExpiryDate", HeaderText = "Expiry Date", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } };
        ed.DefaultCellStyle.Format = "yyyy-MM-dd";
        grid.Columns.Add(ed);

        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False } });
    }
    private static void MakeMetric(Label label, string title, string value, int left) { label.Text = title + "\n" + value; label.Font = new Font("Segoe UI", 12, FontStyle.Bold); label.ForeColor = Color.FromArgb(30, 37, 54); label.BackColor = Color.White; label.Padding = new Padding(18, 13, 5, 5); label.Location = new Point(left, 45); label.Size = new Size(250, 115); }
}

using System.Drawing.Printing;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using QRCoder;
using GymRegistrationApp.Services;

namespace GymRegistrationApp.Forms;

public sealed class QRCodeManagementForm : Form
{
    public const string DefaultCloudUrl = "https://attendance-web-kappa-blond.vercel.app/attendance";

    private readonly string _username;
    private string _attendanceUrl = DefaultCloudUrl;

    private readonly PictureBox _preview = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.White,
        BorderStyle = BorderStyle.None
    };

    private readonly PrintDocument _printDocument = new();
    private byte[]? _png;

    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GymRegistrationApp",
        "attendance-settings.json");

    public QRCodeManagementForm(string username = "Admin")
    {
        _username = username;
        EmbeddedAttendanceServer.Start();

        LoadConfiguration();

        Text = "Universal Gym | Attendance QR";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(480, 520);
        Size = new Size(560, 620);
        BackColor = Color.FromArgb(245, 247, 250);

        BuildCleanLayout();
        GenerateCode();
        _printDocument.PrintPage += PrintDocument_PrintPage;
    }

    private void LoadConfiguration()
    {
        var settings = LoadSettings();
        _attendanceUrl = ResolveAttendanceUrl(settings.AttendanceUrl);
    }

    private void BuildCleanLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 4,
            ColumnCount = 1
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // 0: Header
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));     // 1: Clean QR Card
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));      // 2: Primary Print Button
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));      // 3: Secondary 3-Button Row

        // 0: Header
        var today = AttendanceService.ManilaNow();
        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Universal Gym • Attendance QR",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 28, 45),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 2)
        };
        var subtitle = new Label
        {
            Text = $"📅 {today:dddd, MMMM dd, yyyy}",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(90, 100, 120),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4)
        };
        headerPanel.Controls.Add(title, 0, 0);
        headerPanel.Controls.Add(subtitle, 0, 1);
        root.Controls.Add(headerPanel, 0, 0);

        // 1: Center Card with QR Code
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(14),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 10)
        };
        card.Controls.Add(_preview);
        root.Controls.Add(card, 0, 1);

        // 2: Primary Print Button (Large, prominent)
        var btnPrint = new Button
        {
            Text = "🖨️ Print Attendance QR",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 132, 35),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 6)
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) => Print();
        root.Controls.Add(btnPrint, 0, 2);

        // 3: Secondary 3 Actions in a Row (33% each)
        var actionGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 3,
            Margin = new Padding(0, 0, 0, 2)
        };
        actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        var btnView = ActionButton("🔍 Fullscreen");
        btnView.Click += (_, _) => ViewQrCode();

        var btnDownload = ActionButton("💾 Save PNG");
        btnDownload.Click += (_, _) => Download();

        var btnLog = ActionButton("📋 Attendance Log");
        btnLog.BackColor = Color.FromArgb(41, 128, 185);
        btnLog.ForeColor = Color.White;
        btnLog.Click += (_, _) =>
        {
            try
            {
                using var logForm = new AttendanceForm(_username);
                logForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Attendance Log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        actionGrid.Controls.Add(btnView, 0, 0);
        actionGrid.Controls.Add(btnDownload, 1, 0);
        actionGrid.Controls.Add(btnLog, 2, 0);
        root.Controls.Add(actionGrid, 0, 3);

        Controls.Add(root);
    }

    public string GetActiveDailyQrUrl()
    {
        var today = AttendanceService.ManilaNow().Date;
        var token = DailyTokenService.GenerateDailyToken(today);
        var baseUri = _attendanceUrl.Split('?')[0].TrimEnd('/');
        return $"{baseUri}?date={today:yyyy-MM-dd}&token={token}";
    }

    private void GenerateCode()
    {
        if (string.IsNullOrWhiteSpace(_attendanceUrl) || !Uri.TryCreate(_attendanceUrl, UriKind.Absolute, out _))
        {
            _png = null;
            _preview.Image?.Dispose();
            _preview.Image = null;
            return;
        }

        try
        {
            var dailyUrl = GetActiveDailyQrUrl();
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(dailyUrl, QRCodeGenerator.ECCLevel.Q);
            _png = new PngByteQRCode(data).GetGraphic(16);
            using var stream = new MemoryStream(_png);
            using var image = Image.FromStream(stream);
            _preview.Image?.Dispose();
            _preview.Image = new Bitmap(image);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating QR code: {ex.Message}");
        }
    }

    private void ViewQrCode()
    {
        GenerateCode();
        if (_png is null) return;

        try
        {
            var today = AttendanceService.ManilaNow();
            using var viewer = new Form
            {
                Text = $"Universal Gym Attendance — {today:MMM dd, yyyy}",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(540, 640),
                MinimumSize = new Size(380, 460),
                BackColor = Color.White
            };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 2, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label
            {
                Text = $"Universal Gym Attendance ({today:MMM dd})",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 36, 52)
            }, 0, 0);

            var imageBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };
            using (var stream = new MemoryStream(_png))
            using (var image = Image.FromStream(stream))
            {
                imageBox.Image = new Bitmap(image);
            }
            layout.Controls.Add(imageBox, 0, 1);

            viewer.Controls.Add(layout);
            viewer.FormClosed += (_, _) => imageBox.Image?.Dispose();
            viewer.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not display preview: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Download()
    {
        if (_png is null)
        {
            MessageBox.Show("QR code is not generated yet.", "Save PNG", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            using var dialog = new SaveFileDialog { Filter = "PNG Image (*.png)|*.png", FileName = $"UniversalGym-QR-{AttendanceService.ManilaNow():yyyy-MM-dd}.png" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllBytes(dialog.FileName, _png);
                MessageBox.Show("QR Code image saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save image: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Print()
    {
        try
        {
            using var dialog = new PrintDialog { Document = _printDocument };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _printDocument.Print();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not send poster to printer:\n{ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (_preview.Image is null || e.Graphics is null) return;
        try
        {
            var graphics = e.Graphics;
            var size = Math.Min(e.MarginBounds.Width, e.MarginBounds.Height - 100);
            var area = new Rectangle(e.MarginBounds.Left + (e.MarginBounds.Width - size) / 2, e.MarginBounds.Top + 24, size, size);

            var today = AttendanceService.ManilaNow();
            using var titleFont = new Font("Segoe UI", 22, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 13, FontStyle.Bold);
            using var noteFont = new Font("Segoe UI", 10f);

            graphics.DrawString("UNIVERSAL GYM", titleFont, Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Top - 36);
            graphics.DrawString($"DAILY ATTENDANCE • {today:dddd, MMMM dd, yyyy}", subFont, Brushes.DarkOrange, e.MarginBounds.Left, e.MarginBounds.Top - 6);

            graphics.DrawImage(_preview.Image, area);

            graphics.DrawString("📷 Scan using your phone camera to check in.", noteFont, Brushes.Black, area.Left, area.Bottom + 16);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Print page rendering error: {ex.Message}");
        }
    }

    private static Button ActionButton(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(40, 50, 70),
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Margin = new Padding(3),
        Cursor = Cursors.Hand
    };

    private static string ResolveAttendanceUrl(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && 
            Uri.TryCreate(configured, UriKind.Absolute, out var uri) && 
            !uri.Host.Contains("trycloudflare.com"))
        {
            return uri.ToString();
        }

        var envUrl = Environment.GetEnvironmentVariable("GYM_ATTENDANCE_URL")?.Trim();
        if (!string.IsNullOrWhiteSpace(envUrl) && 
            Uri.TryCreate(envUrl, UriKind.Absolute, out var envUri) && 
            !envUri.Host.Contains("trycloudflare.com"))
        {
            return envUri.ToString();
        }

        return DefaultCloudUrl;
    }

    public static string? LoadSavedAttendanceUrl()
    {
        return LoadSettings().AttendanceUrl;
    }

    public static AttendanceSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return new AttendanceSettings(DefaultCloudUrl);
            var json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<AttendanceSettings>(json);
            if (settings == null) return new AttendanceSettings(DefaultCloudUrl);

            if (string.IsNullOrWhiteSpace(settings.AttendanceUrl) || settings.AttendanceUrl.Contains("trycloudflare.com"))
            {
                settings.AttendanceUrl = DefaultCloudUrl;
            }
            return settings;
        }
        catch
        {
            return new AttendanceSettings(DefaultCloudUrl);
        }
    }

    public static void PersistSettings(AttendanceSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            Environment.SetEnvironmentVariable("GYM_ATTENDANCE_URL", settings.AttendanceUrl);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error persisting attendance settings: {ex.Message}");
        }
    }

    public sealed class AttendanceSettings
    {
        public string AttendanceUrl { get; set; } = DefaultCloudUrl;

        public AttendanceSettings() { }

        public AttendanceSettings(string url)
        {
            AttendanceUrl = url;
        }
    }
}

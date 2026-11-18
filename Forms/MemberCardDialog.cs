using System.Drawing.Printing;
using QRCoder;
using GymRegistrationApp.Models;

namespace GymRegistrationApp.Forms;

public sealed class MemberCardDialog : Form
{
    private readonly Member _member;
    private byte[]? _qrPng;
    private readonly PrintDocument _printDoc = new();

    public MemberCardDialog(Member member)
    {
        _member = member;
        Text = $"Member Pass - {member.FullName} ({member.Id})";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(460, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(245, 246, 249);
        BuildCard();
        _printDoc.PrintPage += PrintDoc_PrintPage;
    }

    private void BuildCard()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        // Card Container
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        card.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw header banner
            using var bannerBrush = new SolidBrush(Color.FromArgb(29, 36, 52));
            g.FillRectangle(bannerBrush, 0, 0, card.Width, 75);

            using var orangeBar = new SolidBrush(Color.FromArgb(255, 132, 35));
            g.FillRectangle(orangeBar, 0, 72, card.Width, 4);

            using var titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            g.DrawString("UNIVERSAL GYM", titleFont, Brushes.White, 20, 16);
            g.DrawString("OFFICIAL MEMBERSHIP PASS", subFont, Brushes.LightGray, 22, 44);

            // Member Info
            var y = 95;
            using var nameFont = new Font("Segoe UI", 14, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var valFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            using var darkBrush = new SolidBrush(Color.FromArgb(29, 36, 52));
            using var grayBrush = new SolidBrush(Color.FromArgb(120, 125, 135));

            var displayName = string.IsNullOrWhiteSpace(_member.FullName) 
                ? $"{_member.FirstName} {_member.LastName}".Trim() 
                : _member.FullName;

            g.DrawString(displayName, nameFont, darkBrush, 20, y);
            y += 32;

            g.DrawString("MEMBER ID", labelFont, grayBrush, 20, y);
            g.DrawString("MEMBERSHIP PLAN", labelFont, grayBrush, 220, y);
            y += 18;

            using var idFont = new Font("Segoe UI", 11, FontStyle.Bold);
            g.DrawString(_member.Id, idFont, new SolidBrush(Color.FromArgb(255, 132, 35)), 20, y);
            g.DrawString(_member.Plan, valFont, darkBrush, 220, y);
            y += 30;

            g.DrawString("VALID UNTIL", labelFont, grayBrush, 20, y);
            g.DrawString("STATUS", labelFont, grayBrush, 220, y);
            y += 18;

            g.DrawString(_member.ExpiryDate.ToString("MMMM dd, yyyy"), valFont, darkBrush, 20, y);
            var statusColor = string.Equals(_member.Status, "Active", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(43, 157, 105) : Color.Crimson;
            g.DrawString(_member.Status, new Font("Segoe UI", 10.5f, FontStyle.Bold), new SolidBrush(statusColor), 220, y);
            y += 35;

            // QR Code
            if (_qrPng != null)
            {
                using var stream = new MemoryStream(_qrPng);
                using var qrImg = Image.FromStream(stream);
                var qrSize = 150;
                var qrX = (card.Width - qrSize) / 2;
                g.DrawImage(qrImg, qrX, y, qrSize, qrSize);

                using var scanFont = new Font("Segoe UI", 8f);
                var scanText = "Scan Member ID at Front Desk";
                var textSize = g.MeasureString(scanText, scanFont);
                g.DrawString(scanText, scanFont, grayBrush, (card.Width - textSize.Width) / 2, y + qrSize + 8);
            }
        };

        // Generate QR code for member ID
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(_member.Id, QRCodeGenerator.ECCLevel.M);
            _qrPng = new PngByteQRCode(data).GetGraphic(10);
        }
        catch { }

        root.Controls.Add(card, 0, 0);

        // Buttons
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var btnClose = ActionButton("Close");
        btnClose.BackColor = Color.FromArgb(120, 125, 135);
        btnClose.Click += (_, _) => Close();

        var btnPrint = ActionButton("🖨️ Print Pass");
        btnPrint.Click += (_, _) => Print();

        var btnSave = ActionButton("💾 Save PNG");
        btnSave.Click += (_, _) => SaveAsPng(card);

        btnPanel.Controls.Add(btnClose);
        btnPanel.Controls.Add(btnPrint);
        btnPanel.Controls.Add(btnSave);
        root.Controls.Add(btnPanel, 0, 1);

        Controls.Add(root);
    }

    private void Print()
    {
        try
        {
            using var dialog = new PrintDialog { Document = _printDoc };
            if (dialog.ShowDialog(this) == DialogResult.OK) _printDoc.Print();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not send to printer:\n{ex.Message}", "Print Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PrintDoc_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (e.Graphics is null) return;
        try
        {
            var g = e.Graphics;
            var w = 320;
            var h = 440;
            var x = e.MarginBounds.Left + (e.MarginBounds.Width - w) / 2;
            var y = e.MarginBounds.Top + 20;

            using var bmp = new Bitmap(w, h);
            using (var bg = Graphics.FromImage(bmp))
            {
                bg.Clear(Color.White);
                using var borderPen = new Pen(Color.FromArgb(200, 205, 215), 2);
                bg.DrawRectangle(borderPen, 1, 1, w - 2, h - 2);

                using var bannerBrush = new SolidBrush(Color.FromArgb(29, 36, 52));
                bg.FillRectangle(bannerBrush, 0, 0, w, 60);
                using var orangeBar = new SolidBrush(Color.FromArgb(255, 132, 35));
                bg.FillRectangle(orangeBar, 0, 58, w, 3);

                bg.DrawString("UNIVERSAL GYM", new Font("Segoe UI", 14, FontStyle.Bold), Brushes.White, 16, 12);
                bg.DrawString("MEMBER PASS", new Font("Segoe UI", 8), Brushes.LightGray, 18, 36);

                var currY = 75;
                var name = string.IsNullOrWhiteSpace(_member.FullName) ? $"{_member.FirstName} {_member.LastName}" : _member.FullName;
                bg.DrawString(name, new Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, 16, currY);
                currY += 28;

                bg.DrawString($"ID: {_member.Id}  •  Plan: {_member.Plan}", new Font("Segoe UI", 9.5f, FontStyle.Bold), Brushes.DarkSlateGray, 16, currY);
                currY += 22;
                bg.DrawString($"Valid Until: {_member.ExpiryDate:MMM dd, yyyy}", new Font("Segoe UI", 9), Brushes.DimGray, 16, currY);
                currY += 25;

                if (_qrPng != null)
                {
                    using var stream = new MemoryStream(_qrPng);
                    using var qrImg = Image.FromStream(stream);
                    bg.DrawImage(qrImg, (w - 180) / 2, currY, 180, 180);
                }
            }

            g.DrawImage(bmp, x, y);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PrintDoc rendering error: {ex.Message}");
        }
    }

    private void SaveAsPng(Panel card)
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"MemberPass-{_member.Id}.png"
            };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                using var bmp = new Bitmap(card.Width, card.Height);
                card.DrawToBitmap(bmp, new Rectangle(0, 0, card.Width, card.Height));
                bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show("Member Pass saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save image:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Button ActionButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(255, 132, 35),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        Margin = new Padding(4),
        Padding = new Padding(8, 4, 8, 4),
        Cursor = Cursors.Hand
    };
}

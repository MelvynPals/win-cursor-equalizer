using System.Drawing.Drawing2D;

namespace CursorEqualizer;

internal sealed partial class MainForm
{
    private const string QrPhResourceName = "CursorEqualizer.DonationQrPh";

    private void ShowQrPh()
    {
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream(QrPhResourceName)
            ?? throw new InvalidOperationException("The receiving QR Ph image is unavailable.");
        using var source = Image.FromStream(stream);
        using var image = new Bitmap(source);
        using var dialog = new Form
        {
            Text = "Support with QR Ph", BackColor = Background, ForeColor = ForeColor,
            Font = Font, AutoScaleDimensions = new SizeF(96, 96), AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(480, 760), MinimumSize = new Size(360, 420),
            StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false,
            MinimizeBox = false, MaximizeBox = false
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(new Label
        {
            Text = $"Support {ProjectLinks.Author}", Font = cardFont, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true, Margin = Padding.Empty
        }, 0, 0);
        layout.Controls.Add(new DonationQrImage(image)
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 16),
            AccessibleName = "Receiving QR Ph code", AccessibleDescription = "Scan this code using your bank or wallet app."
        }, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = "Scan using a QR Ph-compatible bank or wallet. Confirm the recipient in your payment app before sending.",
            ForeColor = Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty
        }, 0, 2);
        var close = new DesignButton { Text = "Close", Primary = true, Dock = DockStyle.Fill, DialogResult = DialogResult.Cancel, Margin = Padding.Empty };
        layout.Controls.Add(close, 0, 3);
        dialog.Controls.Add(layout);
        dialog.CancelButton = close;
        dialog.Shown += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            var size = new Size(Math.Min(dialog.Width, area.Width), Math.Min(dialog.Height, area.Height));
            dialog.Bounds = new Rectangle(
                Math.Clamp(Left + (Width - size.Width) / 2, area.Left, area.Right - size.Width),
                Math.Clamp(Top + (Height - size.Height) / 2, area.Top, area.Bottom - size.Height),
                size.Width, size.Height);
        };
        dialog.ShowDialog(this);
    }

    private sealed class DonationQrImage(Image image) : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float scale = Math.Min(Width / (float)image.Width, Height / (float)image.Height);
            int width = Math.Max(1, (int)Math.Floor(image.Width * scale));
            int height = Math.Max(1, (int)Math.Floor(image.Height * scale));
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.DrawImage(image, new Rectangle((Width - width) / 2, (Height - height) / 2, width, height));
        }

        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); Invalidate(); }
    }
}

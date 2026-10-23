using System.Drawing.Drawing2D;

namespace CursorEqualizer;

internal static class DesignPalette
{
    internal static readonly Color Ground = Color.FromArgb(0, 0, 0);
    internal static readonly Color Surface = Color.FromArgb(17, 17, 17);
    internal static readonly Color Raised = Color.FromArgb(42, 42, 42);
    internal static readonly Color Outline = Color.FromArgb(68, 68, 68);
    internal static readonly Color Ink = Color.White;
    internal static readonly Color Muted = Color.FromArgb(180, 180, 180);
    internal static readonly Color Accent = Color.FromArgb(76, 179, 240);

    internal static GraphicsPath Rounded(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SettingsSurface : Panel
{
    internal SettingsSurface() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); BackColor = DesignPalette.Surface; }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? DesignPalette.Ground);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (Width <= 0 || Height <= 0) return;
        using var path = DesignPalette.Rounded(ClientRectangle, 20 * DeviceDpi / 96f);
        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillPath(fill, path);
    }
}

internal sealed class DesignButton : Button
{
    internal bool Primary { get; set; }
    internal bool DrawCloseGlyph { get; set; }
    private bool hovered;
    private bool pressed;
    internal DesignButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Cursor = Cursors.Hand;
    }
    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? DesignPalette.Ground);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new RectangleF(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2));
        using var path = DesignPalette.Rounded(bounds, Height / 2f);
        Color color = Primary ? (pressed ? Color.Silver : hovered ? Color.FromArgb(225, 225, 225) : Color.White)
            : pressed ? Color.FromArgb(58, 58, 58) : hovered ? Color.FromArgb(48, 48, 48) : DesignPalette.Raised;
        using var fill = new SolidBrush(color);
        e.Graphics.FillPath(fill, path);
        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(DesignPalette.Accent, Math.Max(1, DeviceDpi / 96f));
            e.Graphics.DrawPath(pen, path);
        }
        if (DrawCloseGlyph)
        {
            // A symmetric glyph has no font bearings or baseline offset.
            float scale = DeviceDpi / 96f;
            float centerX = Width / 2f, centerY = Height / 2f;
            float radius = 3.5f * scale;
            using var stroke = new Pen(Enabled ? DesignPalette.Ink : DesignPalette.Muted, 1.5f * scale)
            {
                StartCap = LineCap.Round, EndCap = LineCap.Round
            };
            e.Graphics.DrawLine(stroke, centerX - radius, centerY - radius, centerX + radius, centerY + radius);
            e.Graphics.DrawLine(stroke, centerX - radius, centerY + radius, centerX + radius, centerY - radius);
            return;
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
            !Enabled ? DesignPalette.Muted : Primary ? Color.Black : DesignPalette.Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

internal sealed class PixelSlider : UserControl
{
    internal const int Minimum = 16;
    internal const int Maximum = 128;
    private int value = 32;
    internal event EventHandler? ValueChanged;
    internal int Value
    {
        get => value;
        set
        {
            int next = Math.Clamp(value, Minimum, Maximum);
            if (this.value == next) return;
            this.value = next;
            AccessibleDescription = $"{next} pixels. Use arrow keys to adjust.";
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
        }
    }
    internal PixelSlider()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = true;
        AutoScaleMode = AutoScaleMode.None;
        AccessibleRole = AccessibleRole.Slider;
        BackColor = DesignPalette.Surface;
        Cursor = Cursors.Hand;
    }
    private float ThumbRadius => 7 * DeviceDpi / 96f;
    private float RailInset => 12 * DeviceDpi / 96f;
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float left = RailInset, right = Math.Max(left + 1, Width - RailInset);
        float y = Height / 2f;
        float x = left + (right - left) * (Value - Minimum) / (Maximum - Minimum);
        using var rail = new Pen(DesignPalette.Outline, 3 * DeviceDpi / 96f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var progress = new Pen(DesignPalette.Accent, rail.Width) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLine(rail, left, y, right, y);
        if (x > left) e.Graphics.DrawLine(progress, left, y, x, y);
        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(DesignPalette.Accent, Math.Max(1, DeviceDpi / 96f));
            float radius = ThumbRadius + 4 * DeviceDpi / 96f;
            e.Graphics.DrawEllipse(ring, x - radius, y - radius, radius * 2, radius * 2);
        }
        using var thumb = new SolidBrush(Enabled ? Color.White : DesignPalette.Muted);
        e.Graphics.FillEllipse(thumb, x - ThumbRadius, y - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
    }
    private void SetFromPoint(int x) => Value = Minimum + (int)Math.Round(
        Math.Clamp((x - RailInset) / Math.Max(1, Width - 2 * RailInset), 0, 1) * (Maximum - Minimum));
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        Capture = true;
        SetFromPoint(e.X);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (Capture && (e.Button & MouseButtons.Left) != 0) SetFromPoint(e.X);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Capture) { SetFromPoint(e.X); Capture = false; }
        base.OnMouseUp(e);
    }
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is
        Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        int? next = e.KeyCode switch
        {
            Keys.Left or Keys.Down => Value - 1, Keys.Right or Keys.Up => Value + 1,
            Keys.Home => Minimum, Keys.End => Maximum, Keys.PageUp => Value + 4, Keys.PageDown => Value - 4, _ => null
        };
        if (next is int pixels) { Value = Math.Clamp(pixels, Minimum, Maximum); e.Handled = true; e.SuppressKeyPress = true; }
        base.OnKeyDown(e);
    }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override AccessibleObject CreateAccessibilityInstance() => new SliderAccessibility(this);
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = AccessibilityObject;
    }
    private sealed class SliderAccessibility(PixelSlider owner) : ControlAccessibleObject(owner)
    {
        public override string? Name { get => owner.AccessibleName; set => owner.AccessibleName = value; }
        public override AccessibleRole Role => AccessibleRole.Slider;
        public override string? Value
        {
            get => owner.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            set { if (int.TryParse(value, out int pixels)) owner.Value = pixels; }
        }
    }
}

internal sealed class ModeSelector : Panel
{
    private readonly RadioButton[] buttons;
    private bool syncing;
    private SizingMode mode;
    internal event EventHandler? ModeChanged;
    internal SizingMode SelectedMode
    {
        get => mode;
        set
        {
            mode = value;
            syncing = true;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Checked = i == (value == SizingMode.Calibrated ? 0 : value == SizingMode.Manual ? 1 : 2);
                buttons[i].BackColor = buttons[i].Checked ? Color.FromArgb(65, 65, 65) : DesignPalette.Raised;
                buttons[i].ForeColor = buttons[i].Checked ? Color.White : DesignPalette.Muted;
            }
            syncing = false;
        }
    }
    internal ModeSelector(string displayName)
    {
        BackColor = DesignPalette.Raised;
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(3) };
        buttons = new RadioButton[3];
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            var button = new RadioButton
            {
                Text = new[] { "Calibrated", "Fixed", "Auto" }[i], Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Margin = Padding.Empty,
                TextAlign = ContentAlignment.MiddleCenter, AccessibleName = $"{displayName} {new[] { "Calibrated", "Fixed", "Auto" }[i]} mode"
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.CheckedBackColor = Color.FromArgb(65, 65, 65);
            button.CheckedChanged += (_, _) =>
            {
                if (syncing || !button.Checked) return;
                SelectedMode = index == 0 ? SizingMode.Calibrated : index == 1 ? SizingMode.Manual : SizingMode.Auto;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            };
            buttons[i] = button;
            grid.Controls.Add(button, i, 0);
        }
        Controls.Add(grid);
        SelectedMode = SizingMode.Calibrated;
    }
}

using System.Drawing.Drawing2D;

namespace CursorEqualizer;

// The content retains native scrolling; this control supplies its visible rail and input.
internal sealed class DesignScrollBar : UserControl
{
    private readonly ScrollableControl content;
    private bool hovered;
    private int? dragOffset;
    private float DpiScale => DeviceDpi / 96f;
    private int Maximum => Math.Max(0, content.DisplayRectangle.Height - content.ClientSize.Height);
    private int Position => Math.Clamp(-content.AutoScrollPosition.Y, 0, Maximum);
    private RectangleF Thumb
    {
        get
        {
            float track = Math.Max(1, Height - 12 * DpiScale);
            float height = Math.Min(track, Math.Max(32 * DpiScale,
                track * content.ClientSize.Height / Math.Max(1, content.DisplayRectangle.Height)));
            float top = 6 * DpiScale + (track - height) * Position / Math.Max(1, Maximum);
            return new RectangleF((Width - 6 * DpiScale) / 2, top, 6 * DpiScale, height);
        }
    }

    internal DesignScrollBar(ScrollableControl content)
    {
        this.content = content;
        AutoScaleMode = AutoScaleMode.Inherit;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.ScrollBar;
        AccessibleName = "Support sidebar scroll";
        AccessibleDescription = "Use arrow keys, Page Up, Page Down, Home or End to scroll.";
        content.Scroll += (_, _) => Invalidate();
        content.Layout += (_, _) => { TabStop = Maximum > 0; Invalidate(); };
        content.MouseWheel += (_, _) => Invalidate();
    }

    private void ScrollTo(int position)
    {
        content.AutoScrollPosition = new Point(0, Math.Clamp(position, 0, Maximum));
        Invalidate();
        AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new ScrollAccessibility(this);
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _ = AccessibilityObject; }
    private sealed class ScrollAccessibility(DesignScrollBar owner) : ControlAccessibleObject(owner)
    {
        public override string? Name { get => owner.AccessibleName; set => owner.AccessibleName = value; }
        public override AccessibleRole Role => AccessibleRole.ScrollBar;
        public override string? Value
        {
            get => owner.Position.ToString(System.Globalization.CultureInfo.InvariantCulture);
            set { if (int.TryParse(value, out int position)) owner.ScrollTo(position); }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (Maximum == 0) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var track = new Pen(DesignPalette.Raised, 4 * DpiScale) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLine(track, Width / 2f, 6 * DpiScale, Width / 2f, Math.Max(6 * DpiScale, Height - 6 * DpiScale));
        var thumb = Thumb;
        using var shape = DesignPalette.Rounded(thumb, thumb.Width / 2);
        using var fill = new SolidBrush(dragOffset.HasValue || hovered ? DesignPalette.Muted : Color.FromArgb(112, 112, 112));
        e.Graphics.FillPath(fill, shape);
        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(DesignPalette.Accent, DpiScale);
            e.Graphics.DrawPath(ring, shape);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || Maximum == 0) return;
        Focus();
        var thumb = Thumb;
        if (e.Y >= thumb.Top && e.Y <= thumb.Bottom)
        {
            dragOffset = e.Y - (int)thumb.Top;
            Capture = true;
        }
        else ScrollTo(Position + (e.Y < thumb.Top ? -1 : 1) * content.ClientSize.Height);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragOffset is int offset)
            ScrollTo((int)Math.Round((e.Y - offset - 6 * DpiScale) /
                Math.Max(1, Height - 12 * DpiScale - Thumb.Height) * Maximum));
    }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Capture = false; dragOffset = null; Invalidate(); }
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragOffset = null; Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        int lines = SystemInformation.MouseWheelScrollLines;
        int step = lines < 0 ? content.ClientSize.Height : (int)(lines * 20 * DpiScale);
        ScrollTo(Position - e.Delta / 120 * step);
    }
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is
        Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int? next = e.KeyCode switch
        {
            Keys.Up => Position - (int)(20 * DpiScale), Keys.Down => Position + (int)(20 * DpiScale),
            Keys.PageUp => Position - content.ClientSize.Height, Keys.PageDown => Position + content.ClientSize.Height,
            Keys.Home => 0, Keys.End => Maximum, _ => null
        };
        if (!next.HasValue) return;
        ScrollTo(next.Value);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}

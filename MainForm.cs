using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CursorEqualizer;

internal sealed class MainForm : Form
{
    private static readonly Color Background = DesignPalette.Ground;
    private static readonly Color Surface = DesignPalette.Surface;
    private static readonly Color Border = DesignPalette.Outline;
    private static readonly Color Muted = DesignPalette.Muted;
    private static readonly Color Accent = DesignPalette.Accent;
    private readonly CursorManager manager;
    private readonly SettingsManager settings;
    private readonly FlowLayoutPanel cards;
    private readonly Panel content;
    private readonly Label status;
    private readonly Button pauseButton;
    private readonly NotifyIcon tray;
    private readonly ContextMenuStrip trayMenu;
    private readonly ToolStripMenuItem pauseItem;
    private readonly Icon appIcon;
    private readonly Bitmap brandImage;
    private readonly System.Windows.Forms.Timer saveTimer = new() { Interval = 250 };
    private readonly Font headingFont = new("Segoe UI", 17, FontStyle.Bold);
    private readonly Font cardFont = new("Segoe UI", 12, FontStyle.Bold);
    private readonly Font pixelsFont = new("Segoe UI", 23, FontStyle.Regular);
    private readonly Font smallFont = new("Segoe UI", 9);
    private readonly List<Action> synchronizeCards = new();
    private bool dirty;
    private bool exiting;
    private bool sizingCards;

    internal event EventHandler? ExitRequested;

    internal MainForm(CursorManager manager, SettingsManager settings)
    {
        this.manager = manager;
        this.settings = settings;
        SuspendLayout();
        Text = "CursorEqualizer";
        BackColor = Background;
        ForeColor = Color.FromArgb(235, 240, 248);
        Font = new Font("Segoe UI", 10);
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(760, 710);
        MinimumSize = new Size(520, 480);
        StartPosition = FormStartPosition.CenterScreen;
        appIcon = CreateAppIcon();
        Icon = appIcon;
        brandImage = appIcon.ToBitmap();

        var header = new Panel { Dock = DockStyle.Top, Width = ClientSize.Width, Height = 142, Padding = new Padding(22) };
        header.Controls.Add(new PictureBox { Image = brandImage, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(22, 19, 26, 26), TabStop = false });
        header.Controls.Add(new Label { Text = "CursorEqualizer", Font = cardFont, AutoSize = true, Location = new Point(56, 20) });
        var title = new Label { Text = "Display calibration", Font = headingFont, AutoSize = true, Location = new Point(22, 71) };
        var subtitle = new Label
        {
            Text = "Match the cursor size as you move between screens.", ForeColor = Muted, AutoSize = true,
            Location = new Point(23, 108)
        };
        pauseButton = MakeButton("Pause equalizer", (_, _) => TogglePause());
        pauseButton.Dock = DockStyle.None;
        pauseButton.Bounds = new Rectangle(ClientSize.Width - 218, 16, 150, 36);
        pauseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        ((DesignButton)pauseButton).Primary = true;
        var moreButton = new DesignButton
        {
            Text = "•••", AccessibleName = "More actions", Bounds = new Rectangle(ClientSize.Width - 58, 16, 36, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        header.Controls.AddRange([title, subtitle, pauseButton, moreButton]);

        cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(22, 4, 22, 8), BackColor = Background
        };
        cards.SizeChanged += (_, _) => SizeCards();
        cards.Layout += (_, _) => SizeCards();

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(22, 10, 22, 16),
            ColumnCount = 2, RowCount = 1, BackColor = Background
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        status = new Label { Text = "All changes saved", ForeColor = Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        var hideButton = MakeButton("Hide to tray", (_, _) => Hide());
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(hideButton, 1, 0);
        content = new Panel { Size = ClientSize, BackColor = Background };
        content.Controls.Add(cards);
        content.Controls.Add(footer);
        content.Controls.Add(header);
        Controls.Add(content);
        SizeChanged += (_, _) => LayoutContent();

        trayMenu = new ContextMenuStrip
        {
            BackColor = Surface, ForeColor = ForeColor,
            Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors())
        };
        trayMenu.Items.Add("Open Settings", null, (_, _) => OpenSettings());
        pauseItem = new ToolStripMenuItem("Pause Equalizer", null, (_, _) => TogglePause());
        trayMenu.Items.Add(pauseItem);
        trayMenu.Items.Add("Reload Cursors from Registry", null, (_, _) => RunAction(() =>
        {
            manager.ReloadCursors();
            SetStatus("Cursor scheme reloaded", false);
        }));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit & Restore Default Cursors", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        moreButton.Click += (_, _) => trayMenu.Show(moreButton, new Point(0, moreButton.Height));
        tray = new NotifyIcon { Icon = appIcon, Text = "CursorEqualizer · Active", ContextMenuStrip = trayMenu, Visible = true };
        tray.DoubleClick += (_, _) => OpenSettings();
        saveTimer.Tick += (_, _) => FlushSettings();
        manager.MonitorsChanged += OnMonitorsChanged;
        FormClosing += (_, e) =>
        {
            FlushSettings();
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
        Shown += (_, _) => { LayoutContent(); RebuildCards(); FitToDisplay(); };
        DpiChanged += (_, _) => BeginInvoke(new Action(() => { FitToDisplay(); LayoutContent(); RebuildCards(); }));
        ResumeLayout(true);
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var button = new DesignButton
        {
            Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat,
            BackColor = DesignPalette.Raised, ForeColor = ForeColor, Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.Click += click;
        return button;
    }

    internal void OpenSettings()
    {
        Show();
        RebuildCards();
        WindowState = FormWindowState.Normal;
        FitToDisplay();
        Activate();
    }

    private void FitToDisplay()
    {
        if (WindowState == FormWindowState.Maximized) return;
        Rectangle work = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(Width, work.Width - 24), Math.Min(Height, work.Height - 24));
        Location = new Point(Math.Clamp(Left, work.Left, Math.Max(work.Left, work.Right - Width)),
            Math.Clamp(Top, work.Top, Math.Max(work.Top, work.Bottom - Height)));
    }

    private void LayoutContent()
    {
        int width = Math.Min(ClientSize.Width, (int)Math.Round(960 * DeviceDpi / 96.0));
        content.Bounds = new Rectangle((ClientSize.Width - width) / 2, 0, width, ClientSize.Height);
    }

    private void OnMonitorsChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed && IsHandleCreated)
            BeginInvoke(new Action(() => { if (!IsDisposed) RebuildCards(); }));
    }

    private void RebuildCards()
    {
        cards.SuspendLayout();
        synchronizeCards.Clear();
        foreach (Control control in cards.Controls.Cast<Control>().ToArray()) control.Dispose();
        cards.Controls.Clear();
        foreach (var monitor in manager.GetMonitorSnapshots()) cards.Controls.Add(BuildCard(monitor));
        SizeCards();
        cards.ResumeLayout(true);
    }

    private void SizeCards()
    {
        if (sizingCards) return;
        sizingCards = true;
        try
        {
            int width = Math.Max(100, cards.ClientSize.Width - cards.Padding.Horizontal -
                (cards.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
            foreach (Control card in cards.Controls) card.Width = width;
        }
        finally { sizingCards = false; }
    }

    private Control BuildCard(MonitorSnapshot monitor)
    {
        int Px(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
        string name = monitor.Name.Replace(@"\\.\DISPLAY", "Display ", StringComparison.OrdinalIgnoreCase);
        // 144px identity + 20px gutter + the remaining control surface share the
        // same outer edges as the header and footer; narrow windows retain both columns.
        var row = new TableLayoutPanel
        {
            Height = Px(224), ColumnCount = 2, RowCount = 1, BackColor = Background,
            Margin = new Padding(0, 0, 0, Px(14)), Padding = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(164)));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var identity = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, Px(16), Px(20), 0) };
        identity.Controls.Add(new Label { Text = name, Font = cardFont, AutoSize = true, Location = Point.Empty });
        identity.Controls.Add(new Label
        {
            Text = monitor.Primary ? "Primary display" : "Secondary display", ForeColor = Muted,
            AutoSize = true, Font = smallFont, Location = new Point(0, Px(29))
        });
        identity.Controls.Add(new Label
        {
            Text = $"{monitor.Width} × {monitor.Height}", ForeColor = Muted,
            AutoSize = true, Font = smallFont, Location = new Point(0, Px(66))
        });
        identity.Controls.Add(new Label
        {
            Text = $"{monitor.Dpi / 96.0:P0} Windows scaling", ForeColor = Muted,
            AutoSize = true, Font = smallFont, Location = new Point(0, Px(88))
        });
        row.Controls.Add(identity, 0, 0);

        var surface = new SettingsSurface { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(Px(20), Px(14), Px(20), Px(14)) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Surface, ColumnCount = 1, RowCount = 6,
            Margin = Padding.Empty, Padding = Padding.Empty
        };
        foreach (int height in new[] { 44, 32, 16, 28, 36, 26 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(height)));
        var sizeRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(78)));
        sizeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(34)));
        sizeRow.Controls.Add(new Label { Text = "Cursor size", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var pixelsLabel = new Label
        {
            Text = monitor.TargetPixels.ToString(), Font = pixelsFont, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight, AccessibleName = $"{name} current cursor size", Margin = Padding.Empty
        };
        sizeRow.Controls.Add(pixelsLabel, 1, 0);
        sizeRow.Controls.Add(new Label { Text = "px", ForeColor = Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Margin = Padding.Empty, Padding = new Padding(0, Px(11), 0, 0) }, 2, 0);
        layout.Controls.Add(sizeRow, 0, 0);
        var slider = new PixelSlider { Value = monitor.TargetPixels, Dock = DockStyle.Fill, Margin = Padding.Empty, Text = $"{name} cursor pixels", AccessibleName = $"{name} cursor pixels" };
        layout.Controls.Add(slider, 0, 1);
        var range = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(Px(12), 0, Px(12), 0) };
        range.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        range.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        range.Controls.Add(new Label { Text = "16 px", ForeColor = Muted, Font = smallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 0);
        range.Controls.Add(new Label { Text = "128 px", ForeColor = Muted, Font = smallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Margin = Padding.Empty }, 1, 0);
        layout.Controls.Add(range, 0, 2);

        var fields = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill, Margin = Padding.Empty };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(120)));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(28)));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(36)));
        fields.Controls.Add(new Label { Text = "Size behavior", ForeColor = Muted, Font = smallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, Px(6)) }, 0, 0);
        fields.Controls.Add(new Label { Text = "Screen size (in)", ForeColor = Muted, Font = smallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(Px(14), 0, 0, Px(6)) }, 1, 0);
        var mode = new ModeSelector(name) { SelectedMode = monitor.Mode, Dock = DockStyle.Fill, Margin = Padding.Empty };
        fields.Controls.Add(mode, 0, 1);
        var input = new Panel { BackColor = DesignPalette.Raised, Dock = DockStyle.Fill, Margin = new Padding(Px(14), 0, 0, 0), Padding = new Padding(Px(12), Px(8), Px(8), 0) };
        var diagonal = new TextBox
        {
            Text = monitor.DiagonalInches?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) ?? "",
            BorderStyle = BorderStyle.None, BackColor = DesignPalette.Raised, ForeColor = Color.White,
            Dock = DockStyle.Fill, AccessibleName = $"{name} diagonal in inches", MaxLength = 8
        };
        input.Controls.Add(diagonal);
        fields.Controls.Add(input, 1, 1);
        layout.Controls.Add(fields, 0, 3);
        layout.SetRowSpan(fields, 2);
        var hint = new Label { ForeColor = Muted, Font = smallFont, Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.BottomLeft, Margin = Padding.Empty };
        layout.Controls.Add(hint, 0, 5);
        surface.Controls.Add(layout);
        row.Controls.Add(surface, 1, 0);
        bool? stacked = null;
        row.SizeChanged += (_, _) =>
        {
            bool next = row.Width < Px(660);
            if (stacked == next) return;
            stacked = next;
            row.SuspendLayout();
            row.ColumnStyles.Clear();
            row.RowStyles.Clear();
            row.ColumnCount = next ? 1 : 2;
            row.RowCount = next ? 2 : 1;
            row.ColumnStyles.Add(new ColumnStyle(next ? SizeType.Percent : SizeType.Absolute, next ? 100 : Px(164)));
            if (!next) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (next) row.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(72)));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            row.SetCellPosition(identity, new TableLayoutPanelCellPosition(0, 0));
            row.SetCellPosition(surface, new TableLayoutPanelCellPosition(next ? 0 : 1, next ? 1 : 0));
            identity.Margin = next ? new Padding(0, Px(6), 0, Px(10)) : new Padding(0, Px(16), Px(20), 0);
            identity.Controls[2].Location = new Point(next ? Px(220) : 0, next ? 0 : Px(66));
            identity.Controls[3].Location = new Point(next ? Px(220) : 0, next ? Px(29) : Px(88));
            row.Height = Px(next ? 296 : 224);
            row.ResumeLayout(true);
        };
        bool syncing = false;
        void SyncControls()
        {
            syncing = true;
            try
            {
                var current = manager.GetMonitorSnapshots().Single(m => m.Id == monitor.Id);
                mode.SelectedMode = current.Mode;
                slider.Value = current.TargetPixels;
                pixelsLabel.Text = current.TargetPixels.ToString();
                hint.Text = current.Mode switch
                {
                    SizingMode.Calibrated => "Adapts to changes in Windows scaling.",
                    SizingMode.Manual => "Stays fixed when Windows scaling changes.",
                    _ => "Uses screen sizes. Move the slider to calibrate instead."
                };
            }
            finally { syncing = false; }
        }
        slider.ValueChanged += (_, _) =>
        {
            if (syncing) return;
            RunAction(() =>
            {
                var saved = settings.ForMonitor(monitor.Id);
                if (saved.Mode == SizingMode.Manual) saved.ManualPixels = slider.Value;
                else manager.CalibrateMonitor(monitor.Id, slider.Value);
                SettingsEdited();
            });
        };
        mode.ModeChanged += (_, _) =>
        {
            if (syncing) return;
            RunAction(() =>
            {
                var saved = settings.ForMonitor(monitor.Id);
                var target = manager.GetMonitorSnapshots().Single(m => m.Id == monitor.Id).TargetPixels;
                if (mode.SelectedMode == SizingMode.Calibrated) manager.CalibrateMonitor(monitor.Id, target);
                else if (mode.SelectedMode == SizingMode.Manual) { saved.ManualPixels = target; saved.Mode = SizingMode.Manual; }
                else saved.Mode = SizingMode.Auto;
                SettingsEdited();
            });
        };
        void CommitDiagonal()
        {
            if (syncing) return;
            if (!string.IsNullOrWhiteSpace(diagonal.Text) &&
                (!double.TryParse(diagonal.Text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsed) ||
                 !double.IsFinite(parsed) || parsed < 0 || parsed > 200))
            {
                SetStatus("Enter a screen size from 0 to 200 inches. Use a decimal point.", true);
                diagonal.ForeColor = Color.Salmon;
                return;
            }
            diagonal.ForeColor = Color.White;
            double? inches = string.IsNullOrWhiteSpace(diagonal.Text) ? null : double.Parse(diagonal.Text, System.Globalization.CultureInfo.InvariantCulture);
            if (inches == 0) inches = null;
            if (settings.ForMonitor(monitor.Id).DiagonalInches == inches) return;
            RunAction(() => { settings.ForMonitor(monitor.Id).DiagonalInches = inches; SettingsEdited(); });
        }
        diagonal.Leave += (_, _) => CommitDiagonal();
        diagonal.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { CommitDiagonal(); e.SuppressKeyPress = true; } };
        SyncControls();
        synchronizeCards.Add(SyncControls);
        return row;
    }


    private void SettingsEdited()
    {
        manager.NotifySettingsChanged();
        foreach (var synchronize in synchronizeCards) synchronize();
        dirty = true;
        SetStatus("Saving changes…", false);
        saveTimer.Stop();
        saveTimer.Start();
    }

    private void FlushSettings()
    {
        saveTimer.Stop();
        if (!dirty) return;
        try
        {
            settings.Save();
            dirty = false;
            SetStatus("All changes saved", false);
        }
        catch (Exception ex) { SetStatus($"Could not save: {ex.Message}", true); }
    }

    private void TogglePause() => RunAction(() =>
    {
        manager.SetPaused(!manager.IsPaused);
        pauseButton.Text = manager.IsPaused ? "Resume equalizer" : "Pause equalizer";
        pauseItem.Text = manager.IsPaused ? "Resume Equalizer" : "Pause Equalizer";
        tray.Text = manager.IsPaused ? "CursorEqualizer · Paused" : "CursorEqualizer · Active";
        SetStatus(manager.IsPaused ? "Equalizer paused. Registry cursors restored." : "Equalizer active. All changes saved.", false);
    });

    private void SetStatus(string message, bool error)
    {
        status.Text = message;
        status.ForeColor = error ? Color.Salmon : Muted;
    }

    private void RunAction(Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            SetStatus(ex.Message, true);
            MessageBox.Show(this, ex.Message, "CursorEqualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal void PrepareForExit()
    {
        exiting = true;
        FlushSettings();
        tray.Visible = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            manager.MonitorsChanged -= OnMonitorsChanged;
            saveTimer.Dispose();
            tray.Dispose();
            trayMenu.Dispose();
            appIcon.Dispose();
            brandImage.Dispose();
            headingFont.Dispose();
            cardFont.Dispose();
            pixelsFont.Dispose();
            smallFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Icon CreateAppIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var background = new SolidBrush(Background);
            using var accent = new SolidBrush(Accent);
            graphics.FillEllipse(background, 1, 1, 62, 62);
            graphics.FillRectangle(accent, 17, 27, 7, 22);
            graphics.FillRectangle(accent, 29, 15, 7, 34);
            graphics.FillRectangle(accent, 41, 22, 7, 27);
        }
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        using var ico = new MemoryStream();
        using (var writer = new BinaryWriter(ico, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)1);
            writer.Write((byte)64); writer.Write((byte)64); writer.Write((byte)0); writer.Write((byte)0);
            writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write((uint)png.Length); writer.Write((uint)22); writer.Write(png.ToArray());
        }
        ico.Position = 0;
        using var loaded = new Icon(ico);
        return (Icon)loaded.Clone();
    }

    private sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color MenuItemSelected => Border;
        public override Color MenuItemBorder => Border;
        public override Color MenuBorder => Border;
    }
}

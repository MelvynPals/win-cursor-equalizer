using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CursorEqualizer;

internal sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(17, 21, 28);
    private static readonly Color Surface = Color.FromArgb(27, 33, 43);
    private static readonly Color Border = Color.FromArgb(46, 56, 71);
    private static readonly Color Muted = Color.FromArgb(157, 172, 192);
    private static readonly Color Accent = Color.FromArgb(108, 231, 193);
    private readonly CursorManager manager;
    private readonly SettingsManager settings;
    private readonly FlowLayoutPanel cards;
    private readonly Label status;
    private readonly Button pauseButton;
    private readonly NotifyIcon tray;
    private readonly ContextMenuStrip trayMenu;
    private readonly ToolStripMenuItem pauseItem;
    private readonly Icon appIcon;
    private readonly System.Windows.Forms.Timer saveTimer = new() { Interval = 250 };
    private readonly Font headingFont = new("Segoe UI", 20, FontStyle.Bold);
    private readonly Font cardFont = new("Segoe UI", 12, FontStyle.Bold);
    private readonly List<Action> synchronizeCards = new();
    private bool dirty;
    private bool exiting;

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
        ClientSize = new Size(720, 690);
        MinimumSize = new Size(660, 480);
        StartPosition = FormStartPosition.CenterScreen;
        appIcon = CreateAppIcon();
        Icon = appIcon;

        var header = new Panel { Dock = DockStyle.Top, Height = 116, Padding = new Padding(24, 18, 24, 8) };
        var title = new Label { Text = "CursorEqualizer", Font = headingFont, AutoSize = true, Location = new Point(24, 14) };
        var subtitle = new Label
        {
            Text = "One cursor size, across your displays.", ForeColor = Muted, AutoSize = true,
            Location = new Point(26, 53)
        };
        var previewHint = new Label
        {
            Text = "Move this window onto the display you're adjusting to preview its cursor live.",
            ForeColor = Muted, AutoSize = true, Location = new Point(26, 81)
        };
        header.Controls.AddRange([title, subtitle, previewHint]);

        cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(24, 6, 24, 12), BackColor = Background
        };
        cards.SizeChanged += (_, _) => SizeCards();

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 10, 24, 12),
            ColumnCount = 3, RowCount = 1, BackColor = Background
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        status = new Label { Text = "Active · Settings saved", ForeColor = Accent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        pauseButton = MakeButton("Pause", (_, _) => TogglePause());
        var hideButton = MakeButton("Hide to tray", (_, _) => Hide());
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(pauseButton, 1, 0);
        footer.Controls.Add(hideButton, 2, 0);
        Controls.Add(cards);
        Controls.Add(footer);
        Controls.Add(header);

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
        Shown += (_, _) => { RebuildCards(); FitToDisplay(); };
        DpiChanged += (_, _) => BeginInvoke(new Action(() => { FitToDisplay(); SizeCards(); }));
        ResumeLayout(true);
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var button = new Button
        {
            Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat,
            BackColor = Surface, ForeColor = ForeColor, Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Border;
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
        Rectangle work = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(Width, work.Width - 24), Math.Min(Height, work.Height - 24));
        Location = new Point(Math.Clamp(Left, work.Left, Math.Max(work.Left, work.Right - Width)),
            Math.Clamp(Top, work.Top, Math.Max(work.Top, work.Bottom - Height)));
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
        int width = Math.Max(100, cards.ClientSize.Width - cards.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        foreach (Control card in cards.Controls) card.Width = width;
    }

    private Control BuildCard(MonitorSnapshot monitor)
    {
        // Cards are also created after startup/hotplug, so scale their explicit
        // dimensions to the window's current DPI rather than assuming 96 DPI.
        int Px(int value) => (int)Math.Round(value * DeviceDpi / 96.0);
        var card = new Panel { Height = Px(218), BackColor = Border, Padding = new Padding(Px(1)), Margin = new Padding(0, 0, 0, Px(16)) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(Px(18), Px(12), Px(18), Px(10)),
            ColumnCount = 1, RowCount = 5
        };
        foreach (int height in new[] { 28, 26, 48, 48, 30 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(height)));
        string name = monitor.Name.Replace(@"\\.\DISPLAY", "Display ", StringComparison.OrdinalIgnoreCase);
        layout.Controls.Add(new Label { Text = name, Font = cardFont, AutoSize = true }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = $"{monitor.Width} × {monitor.Height}   ·   {monitor.Dpi / 96.0:P0} scaling{(monitor.Primary ? "   ·   Primary" : "")}",
            ForeColor = Muted, AutoSize = true
        }, 0, 1);
        var inputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(55)));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(106)));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(92)));
        inputs.Controls.Add(new Label { Text = "Mode", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted }, 0, 0);
        var mode = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
            BackColor = Background, ForeColor = ForeColor, Dock = DockStyle.Fill,
            Margin = new Padding(0, Px(8), Px(12), 0), AccessibleName = $"{name} sizing mode"
        };
        mode.Items.AddRange(["Calibrated", "Fixed pixels", "Automatic"]);
        mode.SelectedIndex = ModeIndex(monitor.Mode);
        inputs.Controls.Add(mode, 1, 0);
        inputs.Controls.Add(new Label { Text = "Diagonal (in)", Dock = DockStyle.Fill, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        var diagonal = new NumericUpDown
        {
            Minimum = 0, Maximum = 200, DecimalPlaces = 1, Increment = 0.1m,
            Value = (decimal)(monitor.DiagonalInches ?? 0), Dock = DockStyle.Fill,
            BackColor = Background, ForeColor = ForeColor, BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, Px(8), 0, 0), AccessibleName = $"{name} diagonal in inches"
        };
        inputs.Controls.Add(diagonal, 3, 0);
        layout.Controls.Add(inputs, 0, 2);

        var sizing = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        sizing.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sizing.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(74)));
        var slider = new TrackBar
        {
            Minimum = 16, Maximum = 128, Value = monitor.TargetPixels, TickStyle = TickStyle.None,
            SmallChange = 1, LargeChange = 4, Dock = DockStyle.Fill, AutoSize = false,
            BackColor = Surface, Margin = new Padding(0, Px(5), Px(4), 0), AccessibleName = $"{name} cursor pixels"
        };
        var pixelsLabel = new Label
        {
            Text = $"{slider.Value} px", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Accent, AccessibleName = $"{name} current cursor size"
        };
        sizing.Controls.Add(slider, 0, 0);
        sizing.Controls.Add(pixelsLabel, 1, 0);
        layout.Controls.Add(sizing, 0, 3);
        var hint = new Label { ForeColor = Muted, Dock = DockStyle.Fill, AutoEllipsis = true };
        layout.Controls.Add(hint, 0, 4);
        card.Controls.Add(layout);
        bool syncing = false;
        void SyncControls()
        {
            syncing = true;
            try
            {
                var current = manager.GetMonitorSnapshots().Single(m => m.Id == monitor.Id);
                mode.SelectedIndex = ModeIndex(current.Mode);
                slider.Value = current.TargetPixels;
                pixelsLabel.Text = $"{slider.Value} px";
                hint.Text = current.Mode switch
                {
                    SizingMode.Calibrated => "Adapts your calibrated size when Windows scaling changes.",
                    SizingMode.Manual => "Keeps this pixel size fixed when Windows scaling changes.",
                    _ => "Uses screen dimensions. Adjusting the slider switches to Calibrated."
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
                SyncControls();
            });
        };
        mode.SelectedIndexChanged += (_, _) =>
        {
            if (syncing) return;
            RunAction(() =>
            {
                var saved = settings.ForMonitor(monitor.Id);
                var target = manager.GetMonitorSnapshots().Single(m => m.Id == monitor.Id).TargetPixels;
                if (mode.SelectedIndex == 0) manager.CalibrateMonitor(monitor.Id, target);
                else if (mode.SelectedIndex == 1) { saved.ManualPixels = target; saved.Mode = SizingMode.Manual; }
                else saved.Mode = SizingMode.Auto;
                SettingsEdited();
                SyncControls();
            });
        };
        diagonal.ValueChanged += (_, _) =>
        {
            if (syncing) return;
            RunAction(() =>
            {
                settings.ForMonitor(monitor.Id).DiagonalInches = diagonal.Value == 0 ? null : (double)diagonal.Value;
                SettingsEdited();
                SyncControls();
            });
        };
        SyncControls();
        synchronizeCards.Add(SyncControls);
        return card;
    }

    private static int ModeIndex(SizingMode mode) => mode switch { SizingMode.Calibrated => 0, SizingMode.Manual => 1, _ => 2 };

    private void SettingsEdited()
    {
        manager.NotifySettingsChanged();
        foreach (var synchronize in synchronizeCards) synchronize();
        dirty = true;
        SetStatus("Saving settings…", false);
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
            SetStatus("Settings saved", false);
        }
        catch (Exception ex) { SetStatus($"Could not save: {ex.Message}", true); }
    }

    private void TogglePause() => RunAction(() =>
    {
        manager.SetPaused(!manager.IsPaused);
        pauseButton.Text = manager.IsPaused ? "Resume" : "Pause";
        pauseItem.Text = manager.IsPaused ? "Resume Equalizer" : "Pause Equalizer";
        tray.Text = manager.IsPaused ? "CursorEqualizer · Paused" : "CursorEqualizer · Active";
        SetStatus(manager.IsPaused ? "Registry cursors restored" : "Settings saved", false);
    });

    private void SetStatus(string message, bool error)
    {
        status.Text = $"{(manager.IsPaused ? "Paused" : "Active")} · {message}";
        status.ForeColor = error ? Color.Salmon : manager.IsPaused ? Muted : Accent;
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
            headingFont.Dispose();
            cardFont.Dispose();
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

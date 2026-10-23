namespace CursorEqualizer;

internal sealed partial class MainForm
{
    private Panel supportSidebar = null!;
    private FlowLayoutPanel supportBody = null!;
    private Button supportToggle = null!;
    private bool supportOpen;

    private void CreateSupportSidebar()
    {
        supportSidebar = new SettingsSurface
        {
            Visible = false, Padding = new Padding(20), AccessibleName = "Support and about"
        };
        var top = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Surface, TabIndex = 0 };
        top.Controls.Add(new Label
        {
            Text = "Support & about", Font = cardFont, AutoSize = true, Location = new Point(0, 8), UseMnemonic = false
        });
        var close = new DesignButton
        {
            Text = "\u00d7", DrawCloseGlyph = true, AccessibleName = "Close support sidebar", Size = new Size(32, 32),
            Location = new Point(228, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        top.Width = 260;
        top.Controls.Add(close);
        close.Click += (_, _) => SetSupportOpen(false);
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = Surface, Margin = Padding.Empty, TabIndex = 1
        };
        supportBody = body;
        var identity = new Panel { Height = 86, Margin = new Padding(0, 8, 0, 14), BackColor = Surface };
        identity.Controls.Add(new PictureBox
        {
            Image = brandImage, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(0, 0, 68, 68), TabStop = false
        });
        identity.Controls.Add(new Label
        {
            Text = "CursorEqualizer", Font = cardFont, AutoSize = true, Location = new Point(78, 10)
        });
        var tagline = new Label
        {
            Text = "For your multi-monitor setup", Font = smallFont, ForeColor = Muted,
            AutoSize = true, MaximumSize = new Size(180, 40), Location = new Point(78, 37)
        };
        identity.Controls.Add(tagline);
        identity.SizeChanged += (_, _) => tagline.MaximumSize = new Size(Math.Max(1, identity.ClientSize.Width - tagline.Left), 0);
        body.Controls.Add(identity);
        var author = new LinkLabel
        {
            Text = $"Built by {ProjectLinks.Author}", AutoSize = true, Font = Font,
            LinkArea = new LinkArea(9, ProjectLinks.Author.Length), LinkColor = DesignPalette.Ink,
            ActiveLinkColor = Accent, VisitedLinkColor = DesignPalette.Ink, ForeColor = Muted,
            LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, 0, 0, 30),
            AccessibleName = $"Built by {ProjectLinks.Author}, open GitHub profile"
        };
        author.LinkClicked += (_, _) => RunAction(() => ProjectLinks.Open(ProjectLinks.Profile));
        body.Controls.Add(author);

        void TextRow(string text, bool heading, int bottom = 12)
        {
            body.Controls.Add(new Label
            {
                Text = text, AutoSize = true, Font = heading ? cardFont : Font,
                ForeColor = heading ? DesignPalette.Ink : Muted, Margin = new Padding(0, 0, 0, bottom),
                UseMnemonic = false
            });
        }
        Button LinkButton(string text, Uri destination, bool primary)
        {
            var button = new DesignButton
            {
                Text = text, Height = 40, Primary = primary, Margin = new Padding(0, 0, 0, 10),
                AccessibleDescription = "Opens your web browser."
            };
            button.Click += (_, _) => RunAction(() => ProjectLinks.Open(destination));
            body.Controls.Add(button);
            return button;
        }
        if (ProjectLinks.Donation is Uri donation)
        {
            TextRow("Keep it going", true);
            TextRow("If CursorEqualizer makes your setup easier, you can support its development.", false, 16);
            LinkButton("Buy me a coffee", donation, true);
            TextRow("Opens Ko-fi in your browser.", false, 30);
        }
        TextRow("Have an idea or a bug?", true);
        TextRow("Share what happened or suggest an improvement.", false, 16);
        LinkButton("Send feedback", ProjectLinks.Feedback, false);
        TextRow("Opens GitHub issues. A GitHub account is required.", false, 24);

        bool sizing = false;
        body.Layout += (_, _) =>
        {
            if (sizing) return;
            sizing = true;
            try
            {
                int width = Math.Max(1, body.ClientSize.Width -
                    (body.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
                foreach (Control control in body.Controls)
                {
                    if (control is Label) control.MaximumSize = new Size(width, 0);
                    else control.Width = width;
                }
            }
            finally { sizing = false; }
        };
        body.SizeChanged += (_, _) => body.PerformLayout();
        supportSidebar.Controls.Add(body);
        supportSidebar.Controls.Add(top);
        Controls.Add(supportSidebar);
    }

    private void SetSupportOpen(bool visible)
    {
        supportOpen = visible;
        supportSidebar.Visible = visible;
        LayoutContent();
        if (visible) supportSidebar.SelectNextControl(null, true, true, true, false);
        else supportToggle.Focus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && supportOpen) { SetSupportOpen(false); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (supportOpen && ClientSize.Width < (int)Math.Round(1040 * DeviceDpi / 96.0) &&
            (keyData & Keys.KeyCode) == Keys.Tab && (keyData & (Keys.Alt | Keys.Control)) == 0)
            return supportSidebar.SelectNextControl(ActiveControl, (keyData & Keys.Shift) == 0, true, true, true);
        return base.ProcessDialogKey(keyData);
    }
}

using Microsoft.Win32;

namespace CursorEqualizer;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly CursorManager manager;
    private readonly MainForm form;
    private readonly System.Windows.Forms.Timer pollTimer = new() { Interval = 15 };
    private readonly System.Windows.Forms.Timer smokeTimer = new() { Interval = 2000 };
    private readonly EventHandler displayChanged;
    private bool cleanedUp;
    internal int ExitCode { get; private set; }

    internal static int Run(string[] args, CursorManager manager, CancellationToken cancellation)
    {
        if (args.Any(arg => arg is not ("--minimized" or "--ui-smoke-test")))
            throw new ArgumentException("Use --minimized or --ui-smoke-test separately from console configuration options.");
        // DPI awareness has already been initialized as the first call in Main.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var settings = new SettingsManager();
        manager.Initialize(settings);
        settings.Save();
        using var context = new TrayAppContext(manager, settings, cancellation,
            args.Contains("--minimized"), args.Contains("--ui-smoke-test"));
        Application.Run(context);
        return context.ExitCode;
    }

    private TrayAppContext(CursorManager manager, SettingsManager settings, CancellationToken cancellation,
        bool minimized, bool smokeTest)
    {
        this.manager = manager;
        form = new MainForm(manager, settings);
        form.HandleCreated += (_, _) =>
        {
            int dark = 1;
            // A cosmetic request; unsupported Windows versions keep their system frame.
            NativeMethods.DwmSetWindowAttribute(form.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        };
        form.ExitRequested += (_, _) => ExitThread();
        displayChanged = (_, _) => manager.RequestDisplayRefresh();
        SystemEvents.DisplaySettingsChanged += displayChanged;
        pollTimer.Tick += (_, _) =>
        {
            if (cancellation.IsCancellationRequested) { ExitThread(); return; }
            try { manager.Poll(); }
            catch (Exception ex)
            {
                ExitCode = 1;
                manager.Restore();
                MessageBox.Show(form, $"The equalizer stopped and restored your cursors.\n\n{ex.Message}",
                    "CursorEqualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitThread();
            }
        };
        smokeTimer.Tick += (_, _) => ExitThread();
        pollTimer.Start();
        if (smokeTest) smokeTimer.Start();
        if (!minimized) form.Show();
        Console.WriteLine("Phase 3 tray started. Closing Settings hides it; use the tray menu to exit and restore.");
    }

    private void Cleanup()
    {
        if (cleanedUp) return;
        cleanedUp = true;
        pollTimer.Stop();
        smokeTimer.Stop();
        SystemEvents.DisplaySettingsChanged -= displayChanged;
        form.PrepareForExit();
        if (!manager.Restore()) ExitCode = 1;
        form.Close();
        form.Dispose();
        pollTimer.Dispose();
        smokeTimer.Dispose();
    }

    protected override void ExitThreadCore()
    {
        Cleanup();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Cleanup();
        base.Dispose(disposing);
    }
}

using System.Globalization;
using Microsoft.Win32;

namespace CursorEqualizer;

// Console host for Phase 2; Program retains only DPI/instance/lifecycle concerns.
internal static class ConsoleApp
{
    internal static int Run(string[] args, CursorManager manager, CancellationToken cancellation)
    {
        if (args.Length == 1 && args[0] == "--restore") return manager.Restore() ? 0 : 1;
        bool smokeTest = false, listOnly = false, inverseDpi = false;
        int? secondaryTestPixels = null;
        var changes = new List<Action<SettingsManager, CursorManager>>();

        string Next(ref int index) => ++index < args.Length ? args[index]
            : throw new ArgumentException("Missing option value. Use --help for commands.");
        int Pixels(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            && value is >= 16 and <= 128 ? value : throw new ArgumentException("Pixels must be an integer from 16 to 128.");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help": PrintHelp(); return 0;
                case "--smoke-test": smokeTest = true; break;
                case "--list": listOnly = true; break;
                case "--inverse-dpi": inverseDpi = true; break;
                case "--test-secondary-px": secondaryTestPixels = Pixels(Next(ref i)); break;
                case "--base-px":
                {
                    int pixels = Pixels(Next(ref i));
                    changes.Add((settings, _) => settings.Settings.BasePixels = pixels);
                    break;
                }
                case "--set-manual":
                {
                    string selector = Next(ref i);
                    int pixels = Pixels(Next(ref i));
                    changes.Add((settings, engine) =>
                    {
                        var monitor = settings.ForMonitor(engine.ResolveMonitorId(selector));
                        monitor.Mode = SizingMode.Manual;
                        monitor.ManualPixels = pixels;
                    });
                    break;
                }
                case "--set-auto":
                {
                    string selector = Next(ref i);
                    changes.Add((settings, engine) => settings.ForMonitor(engine.ResolveMonitorId(selector)).Mode = SizingMode.Auto);
                    break;
                }
                case "--set-calibrated":
                {
                    string selector = Next(ref i);
                    int pixels = Pixels(Next(ref i));
                    changes.Add((_, engine) => engine.CalibrateMonitor(selector, pixels));
                    break;
                }
                case "--set-diagonal":
                {
                    string selector = Next(ref i);
                    if (!double.TryParse(Next(ref i), NumberStyles.Float, CultureInfo.InvariantCulture, out double inches)
                        || !double.IsFinite(inches) || inches <= 0 || inches > 200)
                        throw new ArgumentException("Diagonal must be greater than 0 and at most 200 inches (use a decimal point).");
                    changes.Add((settings, engine) => settings.ForMonitor(engine.ResolveMonitorId(selector)).DiagonalInches = inches);
                    break;
                }
                default: throw new ArgumentException($"Unknown option '{args[i]}'. Use --help for commands.");
            }
        }
        if ((inverseDpi && secondaryTestPixels is not null) ||
            (changes.Count != 0 && (inverseDpi || secondaryTestPixels is not null)))
            throw new ArgumentException("Run temporary Phase 1 diagnostics separately from saved settings changes.");

        var settingsManager = new SettingsManager();
        manager.Initialize(settingsManager, inverseDpi, secondaryTestPixels);
        foreach (var change in changes) change(settingsManager, manager);
        settingsManager.Save();
        Console.WriteLine($"Settings: {settingsManager.FilePath}");
        manager.PrintMonitors();
        if (listOnly) return 0;
        Console.WriteLine("Phase 2: saved Auto/Manual/Calibrated settings. Physical equality needs visual verification.");
        Console.WriteLine("Move between monitors. R refreshes displays; Q, Escape, or Ctrl+C exits and restores cursors.");
        EventHandler onDisplayChanged = (_, _) => manager.RequestDisplayRefresh();
        SystemEvents.DisplaySettingsChanged += onDisplayChanged;
        try
        {
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (!cancellation.IsCancellationRequested)
            {
                manager.Poll();
                if (smokeTest && elapsed.Elapsed >= TimeSpan.FromSeconds(2)) break;
                if (!Console.IsInputRedirected && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key is ConsoleKey.Q or ConsoleKey.Escape) break;
                    if (key == ConsoleKey.R) manager.RequestDisplayRefresh();
                }
                cancellation.WaitHandle.WaitOne(15);
            }
        }
        finally
        {
            SystemEvents.DisplaySettingsChanged -= onDisplayChanged;
        }
        return 0;
    }

    private static void PrintHelp() => Console.WriteLine("""
        dotnet run -- [options]
          --list                             Print current monitors, IDs, modes, and sizes; exit
          --set-manual DISPLAY2 23            Save fixed 16-128 px override for this monitor
          --set-auto DISPLAY2                 Save automatic mode for this monitor
          --set-calibrated DISPLAY2 23         Save 23 px at this monitor's current DPI; compensate on DPI changes
          --set-diagonal DISPLAY1 24.5        Save physical diagonal in inches
          --base-px 32                        Save automatic reference size (16-128 px)
          --smoke-test                        Run for two seconds, restore, and exit
          --restore                           Restore the registry cursor scheme and exit
          --inverse-dpi                       Temporary Phase 1 comparison (not saved)
          --test-secondary-px 23              Temporary Phase 1 comparison (not saved)
        Selectors accept DISPLAY1/2, full display names, or the printed monitor interface ID.
        Combine saved-setting options; add --list to configure without running the engine.
        R requests a display refresh while running. Manual sizes stay fixed across DPI changes.
        """);
}

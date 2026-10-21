using System.ComponentModel;
using Microsoft.Win32;

namespace CursorEqualizer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (!NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
        {
            Console.Error.WriteLine($"Cannot enable Per Monitor V2 awareness: {new Win32Exception().Message}");
            return 1;
        }

        using var instance = new Mutex(true, @"Local\CursorEqualizer", out bool ownsInstance);
        if (!ownsInstance)
        {
            Console.Error.WriteLine("CursorEqualizer is already running. Exit that instance first.");
            return 1;
        }

        bool inverseDpi = args.Contains("--inverse-dpi");
        var manager = new CursorManager(inverseDpi);
        var cancellation = new CancellationTokenSource();
        void Shutdown()
        {
            cancellation.Cancel();
            manager.Restore();
        }
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; Shutdown(); };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => Shutdown();
        Application.ApplicationExit += (_, _) => Shutdown();
        SystemEvents.SessionEnding += (_, _) => Shutdown();
        Application.ThreadException += (_, e) =>
        {
            Console.Error.WriteLine(e.Exception);
            Shutdown();
        };

        int result = 0;
        try
        {
            if (args.Length == 1 && args[0] == "--restore") return manager.Restore() ? 0 : 1;
            bool smokeTest = args.Contains("--smoke-test");
            int? secondaryTestPixels = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is "--smoke-test" or "--inverse-dpi") continue;
                if (args[i] == "--test-secondary-px" && secondaryTestPixels is null &&
                    i + 1 < args.Length && int.TryParse(args[++i], out int pixels) &&
                    pixels is >= 16 and <= 128)
                {
                    secondaryTestPixels = pixels;
                    continue;
                }
                throw new ArgumentException("Usage: dotnet run -- [--restore | [--smoke-test] [--inverse-dpi | --test-secondary-px 16-128]]");
            }
            if (inverseDpi && secondaryTestPixels is not null)
                throw new ArgumentException("Choose --inverse-dpi or --test-secondary-px, not both.");
            manager.Initialize(secondaryTestPixels);
            Console.WriteLine(secondaryTestPixels is int testPixels
                ? $"Phase 1 temporary diagnostic: primary=32px, other monitors={testPixels}px. No settings saved."
                : inverseDpi
                ? "Phase 1 diagnostic: inverse-DPI trial (32px * primary DPI / current DPI)."
                : "Phase 1 diagnostic: fixed 32px baseline. Use --inverse-dpi to compare compensation.");
            Console.WriteLine("Loaded bitmap dimensions are not a measurement of the final displayed cursor.");
            Console.WriteLine("Move between monitors. Press Ctrl+C, Q, or Escape to exit and restore cursors.");
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (!cancellation.IsCancellationRequested)
            {
                manager.Poll();
                if (smokeTest && elapsed.Elapsed >= TimeSpan.FromSeconds(2)) break;
                if (!Console.IsInputRedirected && Console.KeyAvailable &&
                    Console.ReadKey(true).Key is ConsoleKey.Q or ConsoleKey.Escape) break;
                cancellation.Token.WaitHandle.WaitOne(15);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            result = 1;
        }
        finally
        {
            if (!manager.Restore()) result = 1;
            instance.ReleaseMutex();
        }
        return result;
    }
}

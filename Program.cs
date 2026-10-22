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

        bool graphical = args.Length == 0 || args.Contains("--minimized") || args.Contains("--ui-smoke-test");
        using var instance = new Mutex(true, @"Local\CursorEqualizer", out bool ownsInstance);
        if (!ownsInstance)
        {
            Console.Error.WriteLine("CursorEqualizer is already running. Exit that instance first.");
            if (graphical) MessageBox.Show("CursorEqualizer is already running. Open Settings from its tray icon.", "CursorEqualizer");
            return 1;
        }

        var manager = new CursorManager();
        var cancellation = new CancellationTokenSource();
        bool uiFaulted = false;
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
            uiFaulted = true;
            Shutdown();
        };

        int result = 0;
        try
        {
            result = graphical ? TrayAppContext.Run(args, manager, cancellation.Token)
                : ConsoleApp.Run(args, manager, cancellation.Token);
            if (uiFaulted) result = 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            if (graphical)
            {
                manager.Restore();
                MessageBox.Show(ex.Message, "CursorEqualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

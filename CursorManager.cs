using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CursorEqualizer;

internal sealed record MonitorSnapshot(string Id, string Name, int Width, int Height,
    uint Dpi, bool Primary, int TargetPixels, SizingMode Mode, double? DiagonalInches);

internal sealed class CursorManager
{
    private const int BasePixels = 32;
    private bool inverseDpi;
    private SettingsManager? settings;
    private long refreshAfter;
    private int refreshRequested;
    private long nextDpiProbe;
    private int? secondaryTestPixels;
    private uint primaryDpi;
    private readonly object gate = new();
    private readonly Dictionary<IntPtr, MonitorDetails> monitors = new();
    private readonly List<CursorSource> sources = new();
    private IntPtr lastMonitor;
    private bool stopping;
    private bool restored;
    private bool paused;

    internal event EventHandler? MonitorsChanged;
    internal bool IsPaused { get { lock (gate) return paused; } }

    internal IReadOnlyList<MonitorSnapshot> GetMonitorSnapshots()
    {
        lock (gate)
            return monitors.Values.OrderBy(m => m.Name).Select(m => new MonitorSnapshot(
                m.Id, m.Name, m.Bounds.Right - m.Bounds.Left, m.Bounds.Bottom - m.Bounds.Top,
                m.DpiX, m.Primary, TargetPixels(m), settings!.ForMonitor(m.Id).Mode,
                settings.ForMonitor(m.Id).DiagonalInches)).ToArray();
    }

    internal void NotifySettingsChanged()
    {
        lock (gate) lastMonitor = IntPtr.Zero;
    }

    internal void SetPaused(bool value)
    {
        lock (gate)
        {
            if (stopping || paused == value) return;
            if (value && !RestoreRegistryScheme())
                throw new InvalidOperationException("Could not restore the cursor scheme when pausing.");
            paused = value;
            lastMonitor = IntPtr.Zero;
        }
    }

    internal void ReloadCursors()
    {
        lock (gate)
        {
            ReadCursorSources();
            if (!RestoreRegistryScheme())
                throw new InvalidOperationException("Could not reload the registry cursor scheme.");
            lastMonitor = IntPtr.Zero;
        }
    }

    private sealed record MonitorDetails(IntPtr Handle, string Name, string Id, NativeMethods.RECT Bounds,
        uint DpiX, uint DpiY, bool Primary);
    private sealed record CursorSource(string Role, uint Id, string Path);

    private static readonly (string Role, uint Id)[] Roles =
    [
        ("Arrow", NativeMethods.OCR_NORMAL), ("IBeam", NativeMethods.OCR_IBEAM),
        ("Wait", NativeMethods.OCR_WAIT), ("Crosshair", NativeMethods.OCR_CROSS),
        ("UpArrow", NativeMethods.OCR_UP), ("SizeNWSE", NativeMethods.OCR_SIZENWSE),
        ("SizeNESW", NativeMethods.OCR_SIZENESW), ("SizeWE", NativeMethods.OCR_SIZEWE),
        ("SizeNS", NativeMethods.OCR_SIZENS), ("SizeAll", NativeMethods.OCR_SIZEALL),
        ("No", NativeMethods.OCR_NO), ("Hand", NativeMethods.OCR_HAND),
        ("AppStarting", NativeMethods.OCR_APPSTARTING)
    ];

    internal void Initialize(SettingsManager settings, bool inverseDpi = false, int? secondaryTestPixels = null)
    {
        if (secondaryTestPixels is < 16 or > 128)
            throw new ArgumentOutOfRangeException(nameof(secondaryTestPixels), "Diagnostic size must be 16-128 px.");
        this.secondaryTestPixels = secondaryTestPixels;
        this.inverseDpi = inverseDpi;
        this.settings = settings;
        RefreshMonitors();
        ReadCursorSources();
    }

    internal void RequestDisplayRefresh()
    {
        Interlocked.Exchange(ref refreshAfter, Environment.TickCount64 + 250);
        Interlocked.Exchange(ref refreshRequested, 1);
    }

    private static string MonitorId(string name)
    {
        for (uint index = 0; ; index++)
        {
            var device = new NativeMethods.DISPLAY_DEVICE { Size = (uint)Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
            if (!NativeMethods.EnumDisplayDevicesW(name, index, ref device, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME)) break;
            if ((device.StateFlags & NativeMethods.DISPLAY_DEVICE_ACTIVE) != 0 && !string.IsNullOrWhiteSpace(device.DeviceId))
                return device.DeviceId;
        }
        Console.WriteLine($"  Warning: no monitor interface ID for {name}; using its display name (may change after reconnect).");
        return name;
    }

    private void RefreshMonitors()
    {
        var discovered = new Dictionary<IntPtr, MonitorDetails>();
        Exception? callbackError = null;
        bool Enumerate(IntPtr handle, IntPtr hdc, ref NativeMethods.RECT bounds, IntPtr data)
        {
            try
            {
                var info = new NativeMethods.MONITORINFOEX
                {
                    Size = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>(),
                    DeviceName = string.Empty
                };
                if (!NativeMethods.GetMonitorInfoW(handle, ref info))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "GetMonitorInfoW failed.");
                Marshal.ThrowExceptionForHR(NativeMethods.GetDpiForMonitor(handle,
                    NativeMethods.MDT_EFFECTIVE_DPI, out uint x, out uint y));
                if (x == 0 || y == 0) throw new InvalidOperationException("Monitor reported zero DPI.");
                discovered.Add(handle, new(handle, info.DeviceName, MonitorId(info.DeviceName), info.Monitor, x, y,
                    (info.Flags & NativeMethods.MONITORINFOF_PRIMARY) != 0));
                return true;
            }
            catch (Exception ex)
            {
                callbackError = ex;
                return false;
            }
        }

        bool enumerated = NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Enumerate, IntPtr.Zero);
        if (callbackError is not null) throw callbackError;
        if (!enumerated) throw new Win32Exception(Marshal.GetLastWin32Error(), "EnumDisplayMonitors failed.");
        if (discovered.Count == 0) throw new InvalidOperationException("No connected monitors found.");
        if (discovered.Values.Count(m => m.Primary) != 1)
            throw new InvalidOperationException("Waiting for Windows to identify one primary monitor.");
        monitors.Clear();
        foreach (var (handle, monitor) in discovered)
        {
            monitors.Add(handle, monitor);
            settings!.ForMonitor(monitor.Id);
        }
        primaryDpi = monitors.Values.Single(m => m.Primary).DpiX;
        // A new display snapshot invalidates the last applied monitor state.
        lastMonitor = IntPtr.Zero;
        MonitorsChanged?.Invoke(this, EventArgs.Empty);
    }

    internal string ResolveMonitorId(string selector)
    {
        var matches = monitors.Values.Where(m =>
            m.Id.Equals(selector, StringComparison.OrdinalIgnoreCase) ||
            m.Name.Equals(selector, StringComparison.OrdinalIgnoreCase) ||
            m.Name.Equals(@"\\.\" + selector, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length == 1 ? matches[0].Id
            : throw new ArgumentException($"Monitor '{selector}' is missing or ambiguous. Use --list to see connected monitor IDs.");
    }

    internal void CalibrateMonitor(string selector, int pixels)
    {
        if (pixels is < 16 or > 128) throw new ArgumentOutOfRangeException(nameof(pixels));
        string id = ResolveMonitorId(selector);
        var monitor = monitors.Values.Single(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var calibration = settings!.ForMonitor(id);
        calibration.Mode = SizingMode.Calibrated;
        calibration.CalibrationPixels = pixels;
        calibration.CalibrationDpi = monitor.DpiX;
    }

    internal void PrintMonitors()
    {
        Console.WriteLine("Connected monitors:");
        foreach (var m in monitors.Values)
        {
            Console.WriteLine($"  {m.Name} handle=0x{m.Handle.ToInt64():X} " +
                $"{m.Bounds.Right - m.Bounds.Left}x{m.Bounds.Bottom - m.Bounds.Top} " +
                $"at ({m.Bounds.Left},{m.Bounds.Top}) DPI={m.DpiX}x{m.DpiY} " +
                $"scale={m.DpiX / 96.0:P0}{(m.Primary ? " [primary]" : "")} target={TargetPixels(m)}px ({SizingDescription(m)})");
            Console.WriteLine($"    ID: {m.Id}");
        }
    }

    private void ReadCursorSources()
    {
        var discovered = new List<CursorSource>();
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors")
            ?? throw new InvalidOperationException("The current user's cursor registry key is missing.");
        Console.WriteLine("Registry cursor sources:");
        foreach (var (role, id) in Roles)
        {
            var raw = key.GetValue(role, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                Console.WriteLine($"  {role}: skipped (no custom file configured)");
                continue;
            }
            string path = Environment.ExpandEnvironmentVariables(raw);
            string extension = Path.GetExtension(path);
            if ((!extension.Equals(".cur", StringComparison.OrdinalIgnoreCase) &&
                 !extension.Equals(".ani", StringComparison.OrdinalIgnoreCase)) || !File.Exists(path))
            {
                Console.WriteLine($"  {role}: skipped (missing/unsupported file): {path}");
                continue;
            }
            discovered.Add(new(role, id, path));
            Console.WriteLine($"  {role} ({id}): {path}");
        }
        if (discovered.Count == 0)
            throw new InvalidOperationException("No usable custom .cur/.ani files found. Select a custom cursor scheme first.");
        sources.Clear();
        sources.AddRange(discovered);
    }

    private int TargetPixels(MonitorDetails monitor)
    {
        if (!monitor.Primary && secondaryTestPixels is int pixels) return pixels;
        if (secondaryTestPixels is not null) return BasePixels;
        if (inverseDpi) return Math.Clamp((int)Math.Round(BasePixels * (double)primaryDpi / monitor.DpiX), 16, 128);
        var primary = monitors.Values.Single(m => m.Primary);
        return CalculateTargetPixels(settings!.Settings.BasePixels, primaryDpi, monitor.DpiX,
            settings.ForMonitor(primary.Id), settings.ForMonitor(monitor.Id));
    }

    internal static int CalculateTargetPixels(int basePixels, uint primaryDpi, uint currentDpi,
        MonitorSettings primary, MonitorSettings current)
    {
        if (current.Mode == SizingMode.Manual) return current.ManualPixels;
        if (current.Mode == SizingMode.Calibrated)
        {
            uint referenceDpi = current.CalibrationDpi
                ?? throw new InvalidDataException("Calibrated mode requires a reference DPI.");
            return (int)Math.Clamp(Math.Round(current.CalibrationPixels * ((double)referenceDpi / currentDpi)), 16, 128);
        }
        if (primary.DiagonalInches is not double primaryInches || current.DiagonalInches is not double currentInches)
            return basePixels;
        return (int)Math.Clamp(Math.Round(basePixels * ((double)primaryDpi / currentDpi) *
            (currentInches / primaryInches)), 16, 128);
    }

    private string SizingDescription(MonitorDetails monitor)
    {
        if (secondaryTestPixels is not null || inverseDpi) return "temporary Phase 1 diagnostic; saved sizing bypassed";
        var calibration = settings!.ForMonitor(monitor.Id);
        if (calibration.Mode == SizingMode.Manual) return "Manual; fixed pixels across DPI changes";
        if (calibration.Mode == SizingMode.Calibrated)
            return $"Calibrated; reference={calibration.CalibrationPixels}px at {calibration.CalibrationDpi} DPI";
        var primary = monitors.Values.Single(m => m.Primary);
        return calibration.DiagonalInches is null || settings.ForMonitor(primary.Id).DiagonalInches is null
            ? "Auto awaiting diagonal sizes; base pixels used"
            : $"Auto; diagonal={calibration.DiagonalInches:0.##}in; formula clamped to 16-128px";
    }

    // GetIconInfo allocates bitmap copies; release them independently of the HCURSOR.
    private static string DescribeCursor(IntPtr cursor)
    {
        if (!NativeMethods.GetIconInfo(cursor, out var info))
            return $"unavailable (GetIconInfo error {Marshal.GetLastWin32Error()})";
        try
        {
            IntPtr bitmap = info.Color != IntPtr.Zero ? info.Color : info.Mask;
            if (bitmap == IntPtr.Zero || NativeMethods.GetObjectW(bitmap,
                Marshal.SizeOf<NativeMethods.BITMAP>(), out var details) == 0)
                return "unavailable (no bitmap dimensions)";
            int height = info.Color != IntPtr.Zero ? details.Height : details.Height / 2;
            return $"{details.Width}x{height}px, hotspot=({info.HotspotX},{info.HotspotY})";
        }
        finally
        {
            if (info.Mask != IntPtr.Zero) NativeMethods.DeleteObject(info.Mask);
            if (info.Color != IntPtr.Zero) NativeMethods.DeleteObject(info.Color);
        }
    }

    internal void Poll()
    {
        lock (gate)
        {
            if (stopping) return;
            // Some scaling changes do not deliver DisplaySettingsChanged. A cheap
            // once-per-second DPI probe queues the same refresh without replacing cursors here.
            if (Environment.TickCount64 >= nextDpiProbe && Volatile.Read(ref refreshRequested) == 0)
            {
                nextDpiProbe = Environment.TickCount64 + 1000;
                foreach (var known in monitors.Values)
                {
                    if (NativeMethods.GetDpiForMonitor(known.Handle, NativeMethods.MDT_EFFECTIVE_DPI,
                        out uint x, out uint y) >= 0 && (x != known.DpiX || y != known.DpiY))
                    {
                        Console.WriteLine("DPI change detected; display refresh queued.");
                        RequestDisplayRefresh();
                        break;
                    }
                }
            }
            if (Volatile.Read(ref refreshRequested) != 0 && Environment.TickCount64 >= Interlocked.Read(ref refreshAfter))
            {
                Interlocked.Exchange(ref refreshRequested, 0);
                try
                {
                    RefreshMonitors();
                    settings!.Save();
                    Console.WriteLine("Display settings refreshed; monitor handles and DPI re-read.");
                    PrintMonitors();
                }
                catch (Exception ex) when (ex is Win32Exception or ExternalException or InvalidOperationException)
                {
                    Console.Error.WriteLine($"Display transition not ready: {ex.Message} Retrying in 1 second.");
                    Interlocked.Exchange(ref refreshAfter, Environment.TickCount64 + 1000);
                    Interlocked.Exchange(ref refreshRequested, 1);
                    return;
                }
            }
            if (paused) return;
            // Secure desktop transitions can temporarily make position unavailable.
            if (!NativeMethods.GetCursorPos(out var point)) return;
            IntPtr handle = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (handle == IntPtr.Zero || handle == lastMonitor) return;
            if (!monitors.TryGetValue(handle, out var monitor))
            {
                if (Volatile.Read(ref refreshRequested) == 0) RequestDisplayRefresh();
                return;
            }
            int pixels = TargetPixels(monitor);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Enter {monitor.Name}: requesting {pixels}px for {sources.Count} roles.");
            foreach (var source in sources)
            {
                IntPtr cursor = NativeMethods.LoadImageW(IntPtr.Zero, source.Path,
                    NativeMethods.IMAGE_CURSOR, pixels, pixels, NativeMethods.LR_LOADFROMFILE);
                if (cursor == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"LoadImageW failed for {source.Role}: {source.Path}");
                string loaded = DescribeCursor(cursor);
                // Pausing/reloading can restore the scheme; any new replacement
                // must mark it dirty again so final exit still restores it.
                restored = false;
                // Ownership transfers to Windows; never reuse or destroy this handle after the call.
                if (!NativeMethods.SetSystemCursor(cursor, source.Id))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"SetSystemCursor failed for {source.Role}.");
                Console.WriteLine($"  {source.Role}: requested={pixels}px loaded={loaded}; SetSystemCursor succeeded");
            }
            lastMonitor = handle;
        }
    }

    internal bool Restore()
    {
        lock (gate)
        {
            stopping = true;
            return restored || RestoreRegistryScheme();
        }
    }

    private bool RestoreRegistryScheme()
    {
        restored = NativeMethods.SystemParametersInfoW(NativeMethods.SPI_SETCURSORS, 0, IntPtr.Zero, 0);
        if (restored) Console.WriteLine("Registry cursor scheme restored.");
        else Console.Error.WriteLine($"Cursor restoration FAILED (Win32 {Marshal.GetLastWin32Error()}). Run dotnet run -- --restore.");
        return restored;
    }
}

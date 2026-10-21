using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CursorEqualizer;

internal sealed class CursorManager
{
    private const int BasePixels = 32;
    private readonly bool inverseDpi;
    private int? secondaryTestPixels;
    private uint primaryDpi;
    private readonly object gate = new();
    private readonly Dictionary<IntPtr, MonitorDetails> monitors = new();
    private readonly List<CursorSource> sources = new();
    private IntPtr lastMonitor;
    private bool stopping;
    private bool restored;

    internal CursorManager(bool inverseDpi = false) => this.inverseDpi = inverseDpi;

    private sealed record MonitorDetails(IntPtr Handle, string Name, NativeMethods.RECT Bounds,
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

    internal void Initialize(int? secondaryTestPixels = null)
    {
        if (secondaryTestPixels is < 16 or > 128)
            throw new ArgumentOutOfRangeException(nameof(secondaryTestPixels), "Diagnostic size must be 16-128 px.");
        this.secondaryTestPixels = secondaryTestPixels;
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
                monitors.Add(handle, new(handle, info.DeviceName, info.Monitor, x, y,
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
        if (monitors.Count == 0) throw new InvalidOperationException("No connected monitors found.");
        primaryDpi = monitors.Values.Single(m => m.Primary).DpiX;
        Console.WriteLine("Connected monitors:");
        foreach (var m in monitors.Values)
        {
            Console.WriteLine($"  {m.Name} handle=0x{m.Handle.ToInt64():X} " +
                $"{m.Bounds.Right - m.Bounds.Left}x{m.Bounds.Bottom - m.Bounds.Top} " +
                $"at ({m.Bounds.Left},{m.Bounds.Top}) DPI={m.DpiX}x{m.DpiY} " +
                $"scale={m.DpiX / 96.0:P0}{(m.Primary ? " [primary]" : "")} target={TargetPixels(m)}px");
        }

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
            sources.Add(new(role, id, path));
            Console.WriteLine($"  {role} ({id}): {path}");
        }
        if (sources.Count == 0)
            throw new InvalidOperationException("No usable custom .cur/.ani files found. Select a custom cursor scheme first.");
    }

    private int TargetPixels(MonitorDetails monitor)
    {
        if (!monitor.Primary && secondaryTestPixels is int pixels) return pixels;
        return inverseDpi
            ? Math.Clamp((int)Math.Round(BasePixels * (double)primaryDpi / monitor.DpiX), 16, 128)
            : BasePixels;
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
            // Secure desktop transitions can temporarily make position unavailable.
            if (!NativeMethods.GetCursorPos(out var point)) return;
            IntPtr handle = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (handle == IntPtr.Zero || handle == lastMonitor) return;
            if (!monitors.TryGetValue(handle, out var monitor))
                throw new InvalidOperationException("Monitor topology changed. Restart this Phase 1 prototype.");
            int pixels = TargetPixels(monitor);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Enter {monitor.Name}: requesting {pixels}px for {sources.Count} roles.");
            foreach (var source in sources)
            {
                IntPtr cursor = NativeMethods.LoadImageW(IntPtr.Zero, source.Path,
                    NativeMethods.IMAGE_CURSOR, pixels, pixels, NativeMethods.LR_LOADFROMFILE);
                if (cursor == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"LoadImageW failed for {source.Role}: {source.Path}");
                string loaded = DescribeCursor(cursor);
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
            if (restored) return true;
            restored = NativeMethods.SystemParametersInfoW(NativeMethods.SPI_SETCURSORS, 0, IntPtr.Zero, 0);
            if (restored) Console.WriteLine("Registry cursor scheme restored.");
            else Console.Error.WriteLine($"Cursor restoration FAILED (Win32 {Marshal.GetLastWin32Error()}). Run dotnet run -- --restore.");
            return restored;
        }
    }
}

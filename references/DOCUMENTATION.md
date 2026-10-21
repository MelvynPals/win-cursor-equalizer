# CursorEqualizer Documentation

## Scope and structure
Phase 1: .NET 8 Windows console prototype. Program.cs owns first-call DPI awareness, single-instance guard, and lifecycle hooks. NativeMethods.cs centralizes all Win32 declarations and constants. CursorManager.cs owns monitors, registry sources, transition polling, sizing, and restoration.

## Engine
Read HKCU\Control Panel\Cursors and expand environment variables. Validate .cur/.ani files for the 13 standard roles in build-notes.md. Skip and report empty/missing sources; never substitute shared cursor handles.

Every 15 ms, call GetCursorPos and MonitorFromPoint. Only initial detection or a different monitor triggers replacements. The current Phase 1 diagnostic requests 32 pixels on every monitor to establish actual Windows rendering behavior. The initial inverse-DPI trial requested 32 px at 125% and 27 px at 150%; the user reported that the second monitor cursor became smaller than both the primary monitor cursor and its original appearance. That trial failed visual verification and was removed. Equal requested pixel sizes are a diagnostic baseline, not a verified physical-size match. Physical diagonal inputs and calibrated sizing belong to Phase 2. Actual appearance and animated cursor sizing need physical testing.

LoadImageW creates a fresh handle using IMAGE_CURSOR=2 and LR_LOADFROMFILE=0x0010; SetSystemCursor consumes it. SPI_SETCURSORS=0x0057 reloads the registry scheme. Cleanup covers normal exit, Ctrl+C, ProcessExit, unhandled exceptions, ApplicationExit, and SessionEnding. Managed cleanup cannot run after force kill or power loss; the --restore command provides recovery.

NativeMethods declares user32.dll: SetProcessDpiAwarenessContext (Per Monitor V2=-4), EnumDisplayMonitors, GetMonitorInfoW (MONITORINFOEX), GetCursorPos (POINT), MonitorFromPoint (MONITOR_DEFAULTTONEAREST=2), LoadImageW, SetSystemCursor, SystemParametersInfoW. shcore.dll: GetDpiForMonitor (effective DPI=0).

## Commands
```powershell
dotnet build
dotnet run
dotnet run -- --restore
dotnet run -- --smoke-test
```
Move between monitors and observe console transitions. Q, Escape, or Ctrl+C exits and restores cursors. Registry values are read only.

`--smoke-test` runs the real engine for two seconds, then restores and exits. The prototype uses a session-local mutex to prevent competing instances. Phase 1 exits with an explanatory error if an unknown monitor handle appears; automatic topology refresh is Phase 2.

## Pending 200% visual test (Phase 1)
1. Exit the app with Ctrl+C and confirm the registry scheme is restored.
2. Set the secondary monitor to 200% in Windows Display settings, leaving the primary at its usual scale. Do this before starting the app: Phase 1 takes its monitor/DPI snapshot at startup.
3. With the app off, compare the arrow on both monitors over the same application (for example, the desktop).
4. Run `dotnet run`. Confirm the secondary monitor reports 192 DPI / 200%. Compare the arrow on both monitors again, then check the animated text/wait cursors separately.
5. Press Ctrl+C and compare the restored behavior. Report whether the app reduced the size difference and which monitor's cursor remains larger.

The log's 32 px is the LoadImageW request, not a measurement of the rendered pointer. This test establishes whether the fixed-size baseline equalizes rendering at a clearly different Windows scaling level; it does not certify physical-size calibration.

## Native API references
- [Microsoft cursor API overview and ownership](https://learn.microsoft.com/en-us/windows/win32/menurc/cursors)
- [Microsoft DPI awareness](https://learn.microsoft.com/en-us/windows/win32/api/windef/ne-windef-dpi_awareness)

Future Phase 4 publish:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

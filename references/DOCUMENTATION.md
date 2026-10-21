# CursorEqualizer Documentation

## Scope and structure
Phase 1: .NET 8 Windows console prototype. Program.cs owns first-call DPI awareness, single-instance guard, and lifecycle hooks. NativeMethods.cs centralizes all Win32 declarations and constants. CursorManager.cs owns monitors, registry sources, transition polling, sizing, and restoration.

## Engine
Read HKCU\Control Panel\Cursors and expand environment variables. Validate .cur/.ani files for the 13 standard roles in build-notes.md. Skip and report empty/missing sources; never substitute shared cursor handles.

Every 15 ms, call GetCursorPos and MonitorFromPoint. Only initial detection or a different monitor triggers replacements. The default Phase 1 diagnostic requests 32 pixels on every monitor. The user reports this does not reduce the difference at 200%; Display 2 remains larger. Use `--inverse-dpi` for an explicit comparison requesting `round(32 * primaryDpi / monitorDpi)`, clamped to 16-128 (32 px at primary 125%, 20 px at secondary 200%). The earlier inverse-DPI trial at 150% made Display 2 too small; neither mode is a verified general solution. Physical diagonal inputs and calibrated sizing belong to Phase 2.

On each transition, log requested dimensions, loaded bitmap dimensions/hotspots, and successful replacement per cursor role. GetIconInfo and gdi32.dll GetObjectW inspect bitmap dimensions; DeleteObject frees both bitmap copies returned by GetIconInfo. Monochrome mask height is divided by two. These are loaded resource dimensions, not the final cursor dimensions after Windows rendering.

LoadImageW creates a fresh handle using IMAGE_CURSOR=2 and LR_LOADFROMFILE=0x0010; SetSystemCursor consumes it. SPI_SETCURSORS=0x0057 reloads the registry scheme. Cleanup covers normal exit, Ctrl+C, ProcessExit, unhandled exceptions, ApplicationExit, and SessionEnding. Managed cleanup cannot run after force kill or power loss; the --restore command provides recovery.

NativeMethods declares user32.dll: SetProcessDpiAwarenessContext (Per Monitor V2=-4), EnumDisplayMonitors, GetMonitorInfoW (MONITORINFOEX), GetCursorPos (POINT), MonitorFromPoint (MONITOR_DEFAULTTONEAREST=2), LoadImageW, SetSystemCursor, SystemParametersInfoW. shcore.dll: GetDpiForMonitor (effective DPI=0).

## Commands
```powershell
dotnet build
dotnet run
dotnet run -- --restore
dotnet run -- --smoke-test
dotnet run -- --inverse-dpi
dotnet run -- --smoke-test --inverse-dpi
dotnet run -- --test-secondary-px 26
```
Move between monitors and observe console transitions. Q, Escape, or Ctrl+C exits and restores cursors. Registry values are read only.

`--test-secondary-px N` is a temporary Phase 1 experiment (16-128 px, not combinable with --inverse-dpi). It keeps the primary at 32 px and requests N px for every non-primary monitor. It saves no configuration. At 200%, the user confirmed loaded=20x20 on all 13 Display 2 roles and reported a cursor smaller than Display 1; fixed 32 px looked larger. At 26 px, Display 2 still looked larger than Display 1 but smaller than with the app off. The next trial is 23 px, a midpoint for comparison, not a derived automatic formula. Per-monitor persisted calibration remains Phase 2.

`--smoke-test` runs the real engine for two seconds, then restores and exits. The prototype uses a session-local mutex to prevent competing instances. Phase 1 exits with an explanatory error if an unknown monitor handle appears; automatic topology refresh is Phase 2.

## Pending 200% visual test (Phase 1)
1. Exit the app with Ctrl+C and confirm the registry scheme is restored.
2. Set the secondary monitor to 200% in Windows Display settings, leaving the primary at its usual scale. Do this before starting the app: Phase 1 takes its monitor/DPI snapshot at startup.
3. With the app off, compare the arrow on both monitors over the same application (for example, the desktop).
4. Run `dotnet run -- --test-secondary-px 23`. Confirm the secondary monitor reports 192 DPI / 200% and target=23px. Move onto that monitor and inspect its Arrow line for requested=23px and the actual loaded bitmap size. Compare the arrow on both monitors, then check the animated text/wait cursors separately. Completed comparison trials: 32 px and 26 px (Display 2 too large), 20 px (Display 2 too small).
5. Press Ctrl+C and compare the restored behavior. Report whether the app reduced the size difference and which monitor's cursor remains larger.

The requested and loaded sizes are not measurements of the rendered pointer. This test compares explicit baseline and inverse-DPI modes at a clearly different Windows scaling level; it does not certify physical-size calibration.

Latest visual result: primary 32 px / secondary 23 px looks approximately matched, but the user is uncertain of exact equality. The user subsequently clarified that Display 2 is back at 150%; the precise scale during the 26 px and 23 px visual trials is unconfirmed. Treat 23 px as provisional and repeat at a confirmed 200% before using it as a 200% calibration. Keep the scale fixed throughout each comparison and test 150% separately afterwards. Restart the app after scale changes to refresh its startup DPI snapshot.

For a physical comparison, measure the visible arrow height on each monitor with a ruler over the same desktop background. Neither the logged bitmap canvas sizes nor screenshot pixel dimensions alone establish equal physical dimensions across monitors with different pixel densities.

## Native API references
- [Microsoft cursor API overview and ownership](https://learn.microsoft.com/en-us/windows/win32/menurc/cursors)
- [Microsoft DPI awareness](https://learn.microsoft.com/en-us/windows/win32/api/windef/ne-windef-dpi_awareness)
- [Microsoft: reading cursor bitmap dimensions and freeing bitmap copies](https://devblogs.microsoft.com/oldnewthing/20101020-00/?p=12493)

Future Phase 4 publish:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

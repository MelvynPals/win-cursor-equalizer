# CursorEqualizer Documentation

## Current scope and files
Phase 3 is implemented as a .NET 8 (`net8.0-windows`, WinForms, WinExe) tray utility and the user confirmed its four requested UI/tray checks on 2026-10-23. Phase 2 calibrated sizing passed the user's 150%/200% visual comparison. The responsive redesign is committed as e6b63a1. Phase 4 standard Windows startup is implemented and the user confirmed it survived a restart; elevated startup and publishing remain pending.

- `Program.cs`: first-call Per Monitor V2 DPI awareness, session-local single-instance mutex, exit/crash/session hooks.
- `TrayAppContext.cs`: WinForms message loop, 15 ms UI-thread poll timer, display-event subscription, GUI lifecycle and cleanup.
- `MainForm.cs`: dark calibration window, per-monitor controls, tray icon/menu, autosave feedback. No P/Invoke calls or interop imports.
- `DesignControls.cs`: shared visual palette, rounded setting surfaces, painted buttons, a fully painted mouse/keyboard slider, and native radio-button mode selector.
- `ConsoleApp.cs`: explicit diagnostic/configuration options and console polling mode, retained for testing/recovery.
- `NativeMethods.cs`: all user32.dll, shcore.dll, gdi32.dll, and dwmapi.dll declarations, constants, and structures.
- `CursorManager.cs`: monitor identities/DPI, registry cursor sources, sizing math, refresh requests, transition-only cursor replacement, restoration.
- `SettingsManager.cs`: validated JSON loading/atomic save in `%AppData%\CursorEqualizer\settings.json`, plus per-user Registry Run startup registration and command validation.
- `tests/Phase2Checks/`: dependency-free focused sizing and settings checks.

## Design experiment setup
The `new-design` branch starts from the verified Phase 3 implementation (`583896e`). The existing, visually verified Phase 3 window capture is preserved as [design-before.png](design-before.png), 918 x 909 pixels at 125% window DPI. It shows D1 at 125%, D2 at 200%, and the established 32/23 px calibration. This is the baseline for a later design comparison; application source was not changed during setup.

Three project-local skills were installed into `.agents/skills` using the Codex skill-installer helper:

| Installed name | Upstream source |
| --- | --- |
| `frontend-design` | [anthropics/skills: skills/frontend-design](https://github.com/anthropics/skills/tree/main/skills/frontend-design) |
| `emil-design-eng` | [emilkowalski/skills: skills/emil-design-eng](https://github.com/emilkowalski/skills/tree/main/skills/emil-design-eng) |
| `design-taste-frontend` | [Leonxlnx/taste-skill: skills/taste-skill](https://github.com/Leonxlnx/taste-skill/tree/main/skills/taste-skill) |

The supplied `emilkowalski/skill` URL redirects to the plural `skills` repository; its main design-engineering skill was selected. Taste's declared skill name is `design-taste-frontend`. Installed skill files and bundled resources are retained in the repository for this branch. Phase 4 remains deferred during this design experiment.

## Design experiment implementation
The user authorized applying the design on 2026-10-23. Taste app-screen direction: `true-black-flat`, seed `1165514722`, settings/devtools, dark, with Segoe UI retained (`--keep type`). Black ground, #111111 surfaces, white text, #B4B4B4 secondary text, and display-inspired #4CB3F0 accent. No decorative animation or additional dependencies. Frontend-design guides hierarchy and composition; Emil guides immediate feedback, focus, and native control behavior.

| Before | After | Why |
| --- | --- | --- |
| Outlined stacked cards | Display identity beside a filled tuning surface | Separate monitor context from controls |
| Small pixel labels | Large right-aligned pixel readouts | Compare saved targets quickly |
| Default bright slider styling | One painted slider owns its rail, thumb and input | Keep dragging and rendering aligned |
| Dropdown mode and numeric spinner | Calibrated / Fixed / Auto radio segments and decimal input | Show available behavior without opening a menu |
| Light system frame | Dark frame via DwmSetWindowAttribute | Match the app surfaces |

The header exposes Pause/Resume and a More actions button sharing the tray menu. Mode hints explain scaling behavior. Diagonal edits validate on Enter or focus loss (blank/0 clears the diagonal); invalid values show an error without saving. Slider edits remain live with 250 ms autosave. Arrow keys change one pixel; Page keys four; Home/End select 16/128. DPI transitions rebuild monitor rows using the new window DPI. Smaller work areas scroll vertically; resizing on return to a larger display can reveal both full rows.

`TrayAppContext` requests `DWMWA_USE_IMMERSIVE_DARK_MODE` (20) through the centralized NativeMethods declaration when the form handle is created. Unsupported Windows versions keep their default frame. MainForm makes no native calls.

Comparison captures: [before at 125%](design-before.png), [after at 125%](design-after.png), [after at 200%](design-after-200.png). The baseline is 918 x 909 px; the redesigned primary capture is 968 x 929 px. The 200% capture fits the work area and uses vertical scrolling. The engine and calibrated references remain D1=32@120 DPI and D2=23@192 DPI.

Verification: clean build (zero warnings/errors), 29 existing engine/settings checks passed, final two-second GUI smoke exited 0 and logged cursor restoration. Native UI Automation verified Pause/Resume and keyboard slider increment/autosave/decrement with exact original JSON restored. Screenshots were inspected at 125% and 200%; the final layout fixes include header anchoring, label widths, rebuilding rows at the new DPI, and resizing rows during scroll layout changes. Physical mouse dragging, edited diagonal/mode interaction, and the redesigned tray flow still await user confirmation.

Taste's browser inspector cannot inspect native WinForms. Its static linter was attempted and reported no supported source files for C#; no browser or static-lint pass is claimed. Native screenshots and UI Automation provide the verification for this experiment. The user explicitly authorized the local redesign commit after the responsive and slider corrections.

## Responsive layout and slider correction
The user's maximized-window and drag screenshots exposed defects in the first design pass. The native TrackBar and custom painter had separate thumb/input geometry and overlapping drawing. PixelSlider is now a double-buffered UserControl with one pixel-to-position mapping for painting, clicking, dragging, and mouse capture. It clears the full background before every frame; invalidation follows every value change. Release outside the control clamps to 16-128. Arrow/Page/Home/End keyboard input, accessible naming, MSAA slider role/value, and autosave remain supported. Native UI Automation resolves the slider by its display-specific name.

The entire settings content (header, monitor rows, footer) is centered and capped at 960 logical pixels. Below 660 logical pixels of row width, each monitor stacks its identity above the controls; resolution and scaling move beside the identity. The minimum window width is 520 logical pixels. Narrow or short work areas scroll vertically. Rounded surfaces repaint on resize, preventing stale corners/rectangles. Maximized windows retain the maximized state during display fitting.

Additional comparison captures: [maximized](design-responsive-wide.png), [narrow stacked](design-responsive-narrow.png), [after repeated dragging](design-slider-drag.png). The drag capture intentionally shows a temporary 72 px target; the saved setup was restored to D1=32@120 and D2=23@192 afterward.

Verification of the corrected implementation: final build zero warnings/errors; all 29 engine/settings checks pass; final GUI smoke exits 0 and logs registry restoration. Actual pointer dragging at 125% crossed multiple positions and saved midpoint=72 px, then keyboard restored the exact original JSON. At 200%, pointer capture outside both ends saved 16 and 128 px, midpoint saved 72 px, and keyboard restored the exact original JSON. Native screenshots were inspected for maximized, narrow, standard, and 200% layouts. The original keyboard-only test did not detect the mouse defect; this verification includes real mouse input. The user subsequently requested the local commit. Remaining physical interaction checks are retained in TODO.md without being marked as tested.

## Settings and identity
JSON version 1 contains `BasePixels` (16-128, default 32) and `Monitors`, a case-insensitive dictionary keyed by monitor device interface ID. Each entry has `Mode` (`Auto`, `Manual`, or `Calibrated`), nullable `DiagonalInches` (>0 to 200), `ManualPixels` (16-128), `CalibrationPixels` (16-128), and nullable positive `CalibrationDpi`. Calibrated mode requires a reference DPI. Existing Auto/Manual settings without the new fields continue to load.

EnumDisplayDevicesW with EDD_GET_DEVICE_INTERFACE_NAME=1 obtains active monitor interface IDs. DISPLAY1/DISPLAY2 are console selectors resolved to those IDs, not persistence keys. Interface IDs generally survive re-enumeration on the same connection but can change with ports/drivers. If Windows provides no interface ID, the display name is used with a warning. Disconnected monitor entries are retained.

Settings are written to a unique temporary file in the same directory, then moved over settings.json. Invalid JSON, unsupported versions, invalid modes, and out-of-range values fail without silently overwriting the file. Fix or rename a malformed settings file before restarting. Use configuration commands while the app is stopped; external edits are loaded on the next launch.

Saved test setup on 2026-10-23: D1 diagonal=24.5 inches, Calibrated=32 px at 120 DPI; D2 diagonal=16.0 inches (provisional: user said approximately 16), Calibrated=23 px at 192 DPI. These anchor values persist independently of subsequent DPI changes. The user confirmed approximately matched appearance at 150% and 200%. The exact D2 diagonal and physical equality remain unmeasured.

## Sizing
Manual mode uses the saved pixel value unchanged across DPI changes. A per-monitor manual target is not a separate calibration for every Windows scaling percentage.

Calibrated mode preserves a visual reference with `clamp(round(CalibrationPixels * CalibrationDpi/currentDpi), 16, 128)`. `--set-calibrated DISPLAY2 23` captures that monitor's current DPI with the given pixel size. Only run this command at the scale at which the given size was visually matched. With the saved 23 px / 192 DPI anchor, D2 requests 31 px at 150% (144 DPI) and returns to 23 px at 200%. Refresh/restart does not rebase the anchor, avoiding cumulative rounding. This mode addresses the user's report that fixed 23 px matched at 200% but became much smaller at 150%; its physical result remains a live test, not a guarantee.

Auto mode implements the requested formula exactly:
```text
targetPx = clamp(round(BasePixels * primaryEffectiveDpi/currentEffectiveDpi
                       * currentDiagonalInches/primaryDiagonalInches), 16, 128)
```
If either diagonal is missing, use BasePixels and report that diagonal inputs are required. With D1=24.5 inches / 120 DPI and provisional D2=16 inches / 192 DPI, the formula yields about 13.06 px, clamped to 16. This is below the 20 px already reported too small. The formula is implemented and tested, but its physical-size assumptions are not validated. Calibrated sizing is the current visual test mode. Do not present Auto as a proven equalization model.

Phase 1 comparisons remain available: `--inverse-dpi` uses the old DPI-only formula and `--test-secondary-px N` fixes primary=32 and all non-primary monitors=N. These temporary options bypass saved sizing, cannot be combined with saved-setting changes, and do not overwrite the saved sizing modes.

## Refresh and cursor lifecycle
Poll GetCursorPos + MonitorFromPoint every approximately 15 ms. LoadImageW and SetSystemCursor run only when the monitor differs from the last applied monitor. Each replacement passes a fresh, unshared handle; Windows consumes it. Registry cursor sources at `HKCU\Control Panel\Cursors` are read only, expanded, and validated for all 13 roles in build-notes.md.

DisplaySettingsChanged handlers only enqueue a refresh. The polling thread debounces events for 250 ms, re-enumerates monitor handles/resolutions/effective DPI/IDs, persists newly seen monitors, and invalidates the previous applied-monitor snapshot. This permits one reapplication on the current monitor after a display change. Unknown monitor handles also queue a refresh. Transient enumeration failures retry after one second. R uses this same refresh path manually. Real scale changes, hotplug, and primary-display changes still require live user verification.

A once-per-second GetDpiForMonitor probe detects DPI changes even when Windows does not deliver DisplaySettingsChanged. It only queues that same refresh when the detected DPI differs; it does not load or replace cursors on the probe/timer tick. Event delivery versus fallback detection should be distinguished in testing logs.

GUI changes, resume, and registry reload explicitly invalidate the applied-monitor snapshot for one update on the next poll, enabling live preview without replacing cursors on normal unchanged ticks. Pause reloads the registry scheme while keeping display detection alive. Resume reapplies the current monitor's size. Any new SetSystemCursor call marks the scheme dirty so exit restores again even after an earlier pause/reload restoration. Reload first validates a fresh cursor-source list, then restores the registry scheme and reapplies custom roles when active; missing roles use the restored scheme.

GetIconInfo + GetObjectW log loaded cursor bitmap dimensions and hotspots. DeleteObject releases the copied bitmaps. Bitmap canvas dimensions do not measure the final rendered cursor or physical visible arrow size.

SPI_SETCURSORS restoration covers normal exit, Ctrl+C, ProcessExit, unhandled exceptions, ApplicationExit, and SessionEnding. Force kill/power loss cannot execute managed cleanup. `--restore` works without loading JSON settings as a recovery command.

## Commands (repository root)
```powershell
dotnet build
dotnet run
dotnet run -- --minimized
dotnet run -- --ui-smoke-test
dotnet run -- --list
dotnet run -- --set-manual DISPLAY2 23 --list
dotnet run -- --set-calibrated DISPLAY2 23 --list
dotnet run -- --set-auto DISPLAY2 --list
dotnet run -- --set-diagonal DISPLAY1 24.5 --set-diagonal DISPLAY2 16 --list
dotnet run -- --base-px 32 --list
dotnet run -- --smoke-test
dotnet run -- --restore
dotnet run -- --help
dotnet run --project tests/Phase2Checks/Phase2Checks.csproj
```
`dotnet run` opens the tray app and Settings window. `--minimized` starts only the tray icon. Enable automatic sign-in launch through Start with Windows in More actions or the tray menu. `--ui-smoke-test` opens the real UI, runs for two seconds, and exits through normal restoration/cleanup. Explicit configuration/diagnostic options retain the console host: without --list they save settings then run the engine. --smoke-test tests that console engine for two seconds and restores. Q, Escape, Ctrl+C, and R apply to the console host.

## Calibration UI and tray
- Each card shows display name, resolution, Windows scaling, mode, diagonal inches, and a live 16-128 px slider. Cards refresh after display changes and use the window's current DPI for layout.
- Calibrated mode remains the recommended test setup. Slider edits capture the selected display's current DPI with the new reference size. Fixed pixels corresponds to Manual; Automatic uses the original diagonal formula. Moving the slider in Automatic switches to Calibrated.
- Diagonal 0 means unknown. Changes preview through CursorManager immediately and save after a 250 ms debounce. Settings flush when closing the window or exiting. Save failures appear in the status line.
- For immediate visual preview, move Settings onto the display being calibrated, then adjust its slider. Otherwise the new size appears when the pointer next enters that display.
- Closing Settings or clicking Hide to tray leaves the equalizer running. Double-click the tray icon, or right-click and select Open Settings, to reopen it. Windows may put the icon in the hidden-icons area.
- Tray actions: Open Settings, Pause/Resume Equalizer, Reload Cursors from Registry, Exit & Restore Default Cursors. Pause is also available in Settings.
- Exit stops timers, unsubscribes display events, flushes settings, restores registry cursors, closes the form, and disposes the tray icon. Startup failure and UI-thread error handlers also restore cursors.
- Standard Windows startup is available in the shared menu; elevated startup and single-file publishing remain pending in Phase 4.

## Phase 4: standard Windows startup
The user authorized this step on 2026-10-24. More actions and the tray menu share a Start with Windows check item. SettingsManager writes only the `CursorEqualizer` REG_SZ value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, using `"<full executable path>" --minimized`. Disabling deletes only that value; a missing key/value is harmless. The registry entry is the source of truth and is reread when opening the menu. Monitor JSON is unchanged, avoiding a conflicting startup flag.

The command uses Environment.ProcessPath, never the working directory, and validates an absolute .exe path with no embedded quotes/control characters. Running under dotnet.exe directly is rejected; use the apphost executable or dotnet run. Run commands over 260 characters are rejected before writing, per [Microsoft Run/RunOnce documentation](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys). This is per-user startup at sign-in and does not request administrator privileges. OS startup policies or a disabled Windows Startup apps entry can prevent launch; the menu check reflects Run registration.

For this development test the registered executable is the project's `bin\Debug\net8.0-windows\CursorEqualizer.exe`. Keep its companion DLL/runtime files and the project location intact. If moving the app, disable startup before moving it and enable again from the new executable. Registration will be updated to the published executable when packaging is verified in a later step.

Verification: build zero warnings/errors; 37 focused checks (the original 29 plus startup command quoting and invalid-path cases). UI Automation enabled startup, verified the exact quoted command, disabled and verified removal, then enabled it for the user's sign-in test. The app exited normally and logged registry cursor restoration. Launching the registered executable with --minimized from TEMP stayed alive, kept Settings hidden, and logged successful cursor replacements. That initial check simulated the command. The user subsequently restarted Windows and confirmed the app survived, verifying automatic startup at sign-in. Startup remains enabled.

User test: sign out and sign in again, or restart and sign in. Check for the CursorEqualizer tray icon (including hidden icons), with no Settings window opening automatically. Open Settings from the tray, confirm saved calibration and cross-monitor behavior, and verify tray Exit restores registry cursors. The user reported a successful restart test. Post-restart calibration details and tray exit restoration were not separately reported. No sign-out/restart was triggered automatically. The standard-startup milestone is being committed on the current branch, main; elevated startup and publishing remain pending.

## Phase 3 live test
1. The Settings window is launched for review. Inspect both display cards and move the window between 125% and 200% displays to check readability, resizing, and scrolling.
2. Move the window to each display, adjust its slider by a few pixels, and verify live cursor resizing. Leave the desired calibration saved; no explicit Save button is required.
3. Toggle Pause/Resume from the window and tray. Pause should restore the registry scheme; resume should reapply the equalizer.
4. Close Settings with X, confirm the app continues in the tray, then reopen it from the tray menu/double-click.
5. Test Reload Cursors from Registry, then exit through Exit & Restore Default Cursors. Relaunch with `dotnet run` and confirm settings persist.
6. Repeat the 150%/200% scale-change comparison with the UI open to confirm slider labels/DPI and calibrated size update correctly.

Phase 3 verification: clean build; all 29 Phase 2 checks pass; two-second GUI smoke test exited 0 and restored the scheme; layout inspected at 125%; automated Pause/Resume passed; keyboard adjustment of D2's slider to 24 px persisted, then it was restored precisely to 23 px. The generic UI Automation Value pattern exposes slider percentage, so exact-pixel verification used the keyboard path. The user confirmed all four requested checks: live sliders, moving the window between displays, closing/reopening from the tray, and tray reload/pause/resume/exit restoration.

## Phase 2 live test
1. Start at primary 125%, D2 200%, then run `dotnet run`. Confirm logged DPI 120/192 and saved Calibrated targets 32/23, with reference DPI 120/192.
2. Compare cursors and animations over the desktop; 23 px is provisional, not an exact-size claim.
3. Change D2 to 150% while running. Look for `Display settings refreshed`, D2 DPI=144, and target=31px; change back to 200% and verify DPI=192 / target=23px. Allow about 1.5 seconds for fallback DPI detection if no Windows display event arrives. Compare actual appearance at both scales. If automatic refresh does not arrive, press R and report that distinction.
4. Exit and run again to verify the same settings reload. Verify restoration on Ctrl+C.
5. Test unplug/reconnect or a resolution change when convenient. Confirm settings follow the monitor ID, with no crash or replacement on every polling tick.

Current verification: clean build, 29 focused checks (including calibrated 23-to-31-to-23 calculation and reference persistence), cross-process settings reload, Auto/Manual/Calibrated selection, native smoke/restoration, and R refresh all passed. Fixed Manual sizing became too small at 150%; the user then confirmed Calibrated mode looks about the same at both 150% and 200%. The visual result establishes approximate consistency on this setup. Logs were not supplied to distinguish Windows events from fallback DPI detection. Physical hotplug/resolution/primary-display regression scenarios remain untested.

## API references
- [EnumDisplayDevicesW and per-monitor interface names](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesw)
- [DisplaySettingsChanged](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.systemevents.displaysettingschanged)
- [Cursor API ownership](https://learn.microsoft.com/en-us/windows/win32/menurc/cursors)
- [Reading cursor bitmap dimensions](https://devblogs.microsoft.com/oldnewthing/20101020-00/?p=12493)

Phase 4 publish command (not implemented/tested yet):
```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

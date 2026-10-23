# CursorEqualizer Documentation

## Current scope and files
Phase 3 is implemented as a .NET 8 (`net8.0-windows`, WinForms, WinExe) tray utility and the user confirmed its four requested UI/tray checks on 2026-10-23. Phase 2 calibrated sizing passed the user's 150%/200% visual comparison. The responsive redesign is committed as e6b63a1. Phase 4 standard Windows startup is implemented and the user confirmed it survived a restart; elevated startup also has user-confirmed restart verification, while single-file publishing is verified and user-confirmed.

- `Program.cs`: first-call Per Monitor V2 DPI awareness, startup-configuration helper dispatch before the mutex/engine, session-local single-instance mutex, exit/crash/session hooks.
- `TrayAppContext.cs`: WinForms message loop, 15 ms UI-thread poll timer, display-event subscription, GUI lifecycle and cleanup.
- `MainForm.cs`: dark calibration window, per-monitor controls, tray icon/menu, autosave feedback. No P/Invoke calls or interop imports.
- `MainForm.Support.cs`: partial MainForm for the closable support/about drawer, creator credit and outbound actions.
- `MainForm.Donations.cs`: modal receiving QR viewer with aspect-preserving rendering and working-area fitting.
- `ProjectLinks.cs`: maintainer name and HTTPS destinations; opens the default browser only after a user clicks.
- `Assets/AppIcon.ico`, `Assets/AppLogo.png`: embedded branding and executable icon, derived from references/icon.svg.
- `tools/Export-Icon.ps1`: optional developer SVG-to-PNG/ICO export using an isolated headless Edge profile and System.Drawing; not a runtime dependency.
- `DesignControls.cs`: shared visual palette, rounded setting surfaces, painted buttons, a fully painted mouse/keyboard slider, and native radio-button mode selector.
- `DesignScrollBar.cs`: dark rounded scrollbar with mouse capture, track paging, wheel and keyboard input for the support drawer.
- `ConsoleApp.cs`: explicit diagnostic/configuration options and console polling mode, retained for testing/recovery.
- `NativeMethods.cs`: all user32.dll, shcore.dll, gdi32.dll, and dwmapi.dll declarations, constants, and structures.
- `CursorManager.cs`: monitor identities/DPI, registry cursor sources, sizing math, refresh requests, transition-only cursor replacement, restoration.
- `SettingsManager.cs`: validated JSON loading/atomic save in `%AppData%\CursorEqualizer\settings.json`, plus per-user Registry Run startup registration and command validation.
- `SettingsManager.Startup.cs`: partial SettingsManager for startup modes, Task Scheduler COM/XML, UAC helper launch, same-account checks and rollback.
- `tests/Phase2Checks/`: dependency-free focused sizing and settings checks.
- `Properties/PublishProfiles/WinX64.pubxml`: framework-dependent Windows x64 single-file publish profile with embedded debug symbols and trimming disabled.

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

## Support sidebar and application branding
The user requested this feature after Phase 4 and supplied references/icon.svg. The same true-black-flat direction (seed 1165514722), Segoe UI and existing palette are retained. The supplied cursor/equalizer artwork is the product identity; the supplied original is preserved as references/icon-original.svg and the final optically centered source is references/icon.svg. No new runtime packages or backend services are added.

Support & about in the footer opens a right-side drawer. At a client width of 1040 logical pixels or more it docks next to calibration; the combined frame is capped at 1284 = 960 + 24 + 300 logical pixels. Below that width, the 300px drawer overlays the existing calibration content with 16px outer insets. Close and Escape dismiss it and restore focus to its opener. Tab/Shift+Tab stay inside the narrow overlay; the wide layout retains normal navigation through both regions. The drawer is closed at startup and uses vertical scrolling on short work areas. Opening/closing does not alter calibration or startup registration.

The drawer shows the logo, Built by MelvynPals linking to https://github.com/MelvynPals, Buy me a coffee linking to the supplied https://ko-fi.com/emjay0303, and Send feedback opening this repository's GitHub issue composer with a generic issue/suggestion template. It does not collect/send diagnostics or submit an issue automatically. GitHub sign-in is identified in the UI. Resend is not used because the owner chose GitHub issues; no API key is embedded. The owner confirms they will make the repository public. Its public API currently returns 404, so public-user feedback access must be rechecked before distribution. Automated Ko-fi fetches returned 403; the actual destination is supplied by the owner and still needs a normal-browser check. No donation or feedback message was submitted during testing.

Brand assets are embedded as CursorEqualizer.AppIcon and CursorEqualizer.AppLogo. ApplicationIcon also compiles Assets/AppIcon.ico into the executable's Windows icon resources. The ICO includes 16, 20, 24, 32, 40, 48, 64, 128 and 256px PNG frames. The header, tray and form use this same artwork. Resources load from assembly streams and work in a single-file copy without Assets or references beside it. To regenerate after editing the SVG:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Export-Icon.ps1
```
The export uses a fresh .local Edge profile, saves a 1024px references/icon.png beside the final SVG, and resamples it into the embedded PNG and ICO frames. Generated assets are committed, so building/running the app does not require Edge or this script.

| Before | After | Why |
| --- | --- | --- |
| Generated equalizer bars | Supplied cursor/equalizer logo | Consistent app and executable branding |
| No support destination | Closable responsive drawer | Keep calibration central and support discoverable |
| No maintainer actions | Ko-fi donation, GitHub feedback and profile credit | Explicit destinations chosen by the owner |

Verification: build and single-file publish succeed with zero warnings/errors; 46 existing engine/settings/startup checks pass. Native screenshots at 125%, maximized, narrow and 200% are saved as support-sidebar-normal.png, support-sidebar-wide.png, support-sidebar-narrow.png and support-sidebar-200.png. Close/reopen, Escape, focus restoration and the four-action Tab cycle pass. Pause/Resume still works. A discovered DPI-transition overflow was fixed by resetting WinForms scroll state after scaling and wrapping the identity caption; native style inspection confirms no horizontal scroll containers at 200% or narrow size. The final exe-only GUI smoke exits 0 and logs registry cursor restoration, normal published menu exit returns 0, the real executable icon extracts correctly, and the source ICO has nine resolutions. Current single-file output is 347,268 bytes. The user explicitly authorized committing the support sidebar, branding and centering changes locally.

The browser-only Taste inspector cannot inspect WinForms; verification uses native UI Automation, screenshots and window styles. No browser audit pass is claimed. No placeholder payment destination, sample account or submitted feedback is shipped. The user authorized the current implementation commit. Public feedback access and normal-browser payment-destination checks remain explicit release checks in TODO.md.

## Optical icon corrections and donation alternatives
The owner reported that the close glyph sat off center and the cursor logo looked left-heavy. The close button now draws two symmetric rounded strokes about the circle center, independent of font metrics. Its accessible name, focus ring, click and Escape behavior are unchanged. Actual captures at 125% and 200% put the white-stroke center within 0.1px of the button's visual center.

The cursor's long left stem concentrates its visible weight left of the bounding-box center. The final SVG applies translate(64 -16) to the artwork while retaining the full black canvas. As an optical aid, the 256px export's luminance-weighted visible centroid moves from approximately (110.96,132.48) to (126.97,128.47), followed by visual inspection. This deliberately changes geometric margins to balance perceived weight. Original: references/icon-original.svg. Final deliverables: references/icon.svg and references/icon.png (1024 x 1024). Assets/AppLogo.png and Assets/AppIcon.ico are regenerated from the final SVG and embedded into the rebuilt/published exe.

Verification: build and single-file publish succeed without warnings/errors. Native close-button captures at both display scales and close/reopen/Escape focus restoration pass. Normal published exit logs restoration; a final exe-only GUI smoke exits 0 and logs registry cursor restoration. The user subsequently authorized the local commit after reviewing the centering changes. The existing 46 engine checks passed during the sidebar feature; no engine or settings behavior was changed by this visual correction.

The owner reports their Ko-fi checkout only offers PayPal and asks for broader options. Buy Me a Coffee is recommended for major cards, Apple Pay and Google Pay in supported regions, with one-time support requiring no donor account; its platform fee is 5% plus processing fees. Its payout-country list includes the Philippines. For GCash/Maya, PayMongo's reusable Payment Pages are another option. The owner subsequently reports they are not eligible for Stripe, so Buy Me a Coffee is ruled out for their account despite country-level availability. Future provider selection must account for their payout country and personal versus registered-business status. The configured Ko-fi destination is retained until the owner supplies an actual replacement page URL. No payment account or transaction was created.
Sources: [Buy Me a Coffee payment methods](https://help.buymeacoffee.com/en/articles/10182730-what-is-buy-me-a-coffee-and-how-does-it-work), [payout countries](https://help.buymeacoffee.com/en/articles/6258038-supported-countries-for-payouts-on-buy-me-a-coffee), [PayMongo Payment Pages](https://www.paymongo.com/products/accept-payments/payment-pages).

## PayPal and QR Ph support
The owner selected PayPal plus QR Ph after reporting Stripe ineligibility, then supplied https://paypal.me/emjaypals and a GoTyme receiving QR PNG. These replace the earlier Ko-fi action: Support with PayPal opens that exact HTTPS destination and Show QR Ph opens the embedded receiving image. Assets/DonationQrPh.png is byte-for-byte identical to the supplied image (SHA-256 52336884566656926B67780A069DC155F54012EFC0ADA3415146AB12E500990A); no QR payload is invented or regenerated. The exported image includes the recipient name and masked account number and is bundled into the executable for display.

MainForm.Donations.cs loads CursorEqualizer.DonationQrPh from the assembly, preserves its aspect ratio with nearest-neighbor rendering, and provides Close/Escape behavior. The modal is fitted to the parent monitor's working area after DPI scaling. Its nested WinForms message loop keeps the cursor polling timer running. Payment completion stays in the sender's bank or wallet app; the caption asks them to confirm the recipient there. No API key, payment submission or payment confirmation service is added.

Verification: build and single-file publish succeed with zero warnings/errors; GUI smoke exits 0 with registry cursor restoration. Native action names are present; actual mouse input opens the QR modal and keyboard checks pass Close/reopen/Escape. The published QR screenshot is references/support-qr-ph.png, inspected with the full image and controls visible. UI Automation Invoke was unreliable for the custom button; actual mouse/keyboard input was used. The running published app is left with the QR modal open for owner scan verification. The owner still needs to check recipient recognition, the PayPal browser destination and QR dialog fitting on the 200% display; no payment was submitted.

## Sidebar scrollbar polish
The owner requested a darker scrollbar aligned to the close icon and explicitly authorized the local commit. The support body retains native FlowLayoutPanel scrolling inside a clipped viewport; its native scrollbar sits beyond the viewport edge. A 32 logical px custom scrollbar column shares the close button's centerline. The visible rail is 4 logical px and the rounded thumb is 6 logical px with a 32 logical px minimum length, hover/drag feedback and keyboard focus outline. The rail is omitted when content fits. Native wheel and focused-child scrolling remain available; the custom control also supports dragging with mouse capture, track page clicks, wheel input, Up/Down, Page Up/Down, Home and End. It exposes an MSAA scrollbar role/value using the existing custom-slider accessibility pattern.

| Before | After | Why |
| --- | --- | --- |
| Bright native track and arrow buttons | Dark rail and rounded thumb | Match the existing surfaces |
| Scrollbar offset from close icon | Shared centerline in a 32px column | Align the right-side controls |
| Native chrome reappeared during 200% transition | Refit viewport and scrollbar width after layout/DPI changes | Keep clipping correct across monitors |

Verification: clean build and single-file publish have zero warnings/errors. Actual mouse thumb dragging and keyboard End/Home move content and return it to the original position; wheel input was exercised. Native captures references/support-scrollbar.png and references/support-scrollbar-200.png were inspected after moving between 125% and 200% displays. Accessibility-name lookup was unreliable, so input checks used actual mouse and keyboard events. The calibration engine, persistence and startup registration are unchanged.

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
`dotnet run` opens the tray app and Settings window. `--minimized` starts only the tray icon. Choose Off, Standard or Elevated (administrator) under Start with Windows in More actions or the tray menu. `--ui-smoke-test` opens the real UI, runs for two seconds, and exits through normal restoration/cleanup. Explicit configuration/diagnostic options retain the console host: without --list they save settings then run the engine. --smoke-test tests that console engine for two seconds and restores. Q, Escape, Ctrl+C, and R apply to the console host.

## Calibration UI and tray
- Each card shows display name, resolution, Windows scaling, mode, diagonal inches, and a live 16-128 px slider. Cards refresh after display changes and use the window's current DPI for layout.
- Calibrated mode remains the recommended test setup. Slider edits capture the selected display's current DPI with the new reference size. Fixed pixels corresponds to Manual; Automatic uses the original diagonal formula. Moving the slider in Automatic switches to Calibrated.
- Diagonal 0 means unknown. Changes preview through CursorManager immediately and save after a 250 ms debounce. Settings flush when closing the window or exiting. Save failures appear in the status line.
- For immediate visual preview, move Settings onto the display being calibrated, then adjust its slider. Otherwise the new size appears when the pointer next enters that display.
- Hide to tray uses the shared primary button style (white fill, black text, matching hover/pressed states); Support & about remains secondary. Closing Settings or clicking Hide to tray leaves the equalizer running. Double-click the tray icon, or right-click and select Open Settings, to reopen it. Windows may put the icon in the hidden-icons area.
- Tray actions: Open Settings, Pause/Resume Equalizer, Reload Cursors from Registry, Exit & Restore Default Cursors. Pause is also available in Settings.
- Exit stops timers, unsubscribes display events, flushes settings, restores registry cursors, closes the form, and disposes the tray icon. Startup failure and UI-thread error handlers also restore cursors.
- Standard and elevated Windows startup modes are available in the shared menu. Elevated startup has user-confirmed restart verification; single-file publishing is verified and user-confirmed.

## Phase 4: standard Windows startup
The user authorized this step on 2026-10-24. More actions and the tray menu share a Start with Windows check item. SettingsManager writes only the `CursorEqualizer` REG_SZ value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, using `"<full executable path>" --minimized`. Disabling deletes only that value; a missing key/value is harmless. The registry entry is the source of truth and is reread when opening the menu. Monitor JSON is unchanged, avoiding a conflicting startup flag.

The command uses Environment.ProcessPath, never the working directory, and validates an absolute .exe path with no embedded quotes/control characters. Running under dotnet.exe directly is rejected; use the apphost executable or dotnet run. Run commands over 260 characters are rejected before writing, per [Microsoft Run/RunOnce documentation](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys). This is per-user startup at sign-in and does not request administrator privileges. OS startup policies or a disabled Windows Startup apps entry can prevent launch; the menu check reflects Run registration.

The initial startup tests registered `bin\Debug\net8.0-windows\CursorEqualizer.exe`. Packaging verification subsequently migrated the elevated task to `bin\Release\net8.0-windows\win-x64\publish\CursorEqualizer.exe`; Debug companion files are no longer needed for startup. Keep the published executable in its registered location. If moving it, open the new copy and reselect the desired startup mode to update registration.

Verification: build zero warnings/errors; 37 focused checks (the original 29 plus startup command quoting and invalid-path cases). UI Automation enabled startup, verified the exact quoted command, disabled and verified removal, then enabled it for the user's sign-in test. The app exited normally and logged registry cursor restoration. Launching the registered executable with --minimized from TEMP stayed alive, kept Settings hidden, and logged successful cursor replacements. That initial check simulated the command. The user subsequently restarted Windows and confirmed the app survived, verifying automatic startup at sign-in. Startup remains enabled.

User test: sign out and sign in again, or restart and sign in. Check for the CursorEqualizer tray icon (including hidden icons), with no Settings window opening automatically. Open Settings from the tray, confirm saved calibration and cross-monitor behavior, and verify tray Exit restores registry cursors. The user reported a successful restart test. Post-restart calibration details and tray exit restoration were not separately reported. No sign-out/restart was triggered automatically. The standard-startup milestone is committed as 4f55f22 on main; elevated startup also has user-confirmed restart verification, while single-file publishing is verified and user-confirmed.

## Phase 4: elevated startup
SettingsManager is split across SettingsManager.cs and SettingsManager.Startup.cs. The shared Start with Windows submenu offers Off, Standard, and Elevated (administrator), refreshing its checks from the actual task/registry state. Standard uses the existing per-user Run value. Elevated uses a Task Scheduler task named `CursorEqualizer-<current-user-SID>` and removes the Run value after registration succeeds. Switching to Standard restores Run and deletes the task; Off removes both. Task deletion changes future startup and does not stop a running app. Changing startup never changes the current process's privilege level.

The task XML uses a logon trigger for that user, an InteractiveToken principal with HighestAvailable run level, and an Exec action with the raw executable path, --minimized argument, and explicit executable directory. XML construction escapes path content. The task runs on battery, does not stop on battery transition, ignores duplicate task instances, has ExecutionTimeLimit=PT0S, and disables scheduler hard termination. Existing app mutex and cursor restoration hooks remain active. No user password is stored. Task Scheduler COM objects are released explicitly; no new P/Invoke declarations or UI-native calls are introduced.

Enabling or removing an elevated task invokes the executable through ShellExecute runas when the app is not already elevated. A background operation keeps the UI/engine timers running while Windows requests approval; repeated startup clicks are disabled until completion. Cancelling UAC leaves configuration unchanged. The helper command `--configure-startup <StartupMode> <original-user-SID>` runs before the single-instance mutex and cursor engine, validates its mode and identity, requires administrator privileges, changes startup, and exits. Alternate administrator credentials for a different account are rejected so another account's HKCU/task is never configured. Errors appear in the helper's error dialog and parent status. After mutation failures, the configuration code attempts to restore prior app Run/task state and reports rollback failure if restoration also fails.

The menu check reads enabled-task existence first, then Run registration. Disabled tasks do not count as enabled elevated startup, but are removed when switching modes. Only the current account's app task and CursorEqualizer Run value are modified. Task Scheduler is queried through Schedule.Service; unavailable service/access errors are reported without stopping cursor polling.

Verification: clean build zero warnings/errors, 46 focused checks, GUI smoke exits 0 with registry cursor restoration. Tests cover task path/XML escaping, explicit working directory, minimized arguments, matching principal/trigger SID, interactive highest privileges, battery behavior, unlimited lifetime, no hard termination and duplicate-instance policy. Real UI/UAC testing switched Standard -> Elevated -> Standard -> Elevated. Task Scheduler confirmed the enabled task, same-user principal, InteractiveToken/HighestAvailable, and no Run duplicate. Returning to Standard deleted the task and restored the quoted Run command. The user subsequently restarted with elevated mode enabled and confirmed the app is active and working, verifying startup through this configuration. The process token was not separately reported from Task Manager. UAC cancellation and injected mutation/rollback failure were not exercised and remain separate checks.

Restart test: restart/sign in, check for one tray icon and no automatic Settings window, open Settings, test calibration, then exit through the tray to restore registry cursors. Task Manager's Details view can show the Elevated column to confirm the new process is elevated. The user confirmed the restart test succeeded; the elevated-startup milestone is being committed. Post-restart tray Exit restoration and a separate token inspection remain explicit regression checks. Single-file publishing is now technically verified and user-confirmed.

Task Scheduler references: [security contexts and highest privileges](https://learn.microsoft.com/en-us/windows/win32/taskschd/security-contexts-for-running-tasks), [Task Scheduler XML schema](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-schema), [task settings](https://learn.microsoft.com/en-us/windows/win32/taskschd/tasksettings).

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

Phase 4 single-file publishing:
```powershell
dotnet publish -c Release -p:PublishProfile=WinX64
```
Output: `bin\Release\net8.0-windows\win-x64\publish\CursorEqualizer.exe`.

The profile sets RuntimeIdentifier=win-x64, SelfContained=false, PublishSingleFile=true, PublishTrimmed=false and DebugType=embedded. Embedding symbols keeps the publish folder to one executable. Trimming stays disabled for WinForms, reflection and Task Scheduler COM. Normal dotnet run/build behavior is unchanged. Settings remain in AppData and startup uses Environment.ProcessPath, which resolves the actual published exe. See [Microsoft single-file deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

This is a framework-dependent executable: target PCs need the .NET 8 Windows Desktop Runtime x64. The runtime is not bundled; this machine has Microsoft.WindowsDesktop.App 8.0.32 installed. The first publish produced one 260,228-byte executable and all 46 focused checks passed. An exe-only copy in a fresh isolated .local folder launched from TEMP passed --list, --smoke-test and --ui-smoke-test with exit 0; both smoke tests logged registry cursor restoration. GUI checks passed Pause/Resume, real pointer midpoint=72px, keyboard increment=73px, autosave and exact original JSON restoration, then normal menu Exit with cursor restoration. A duplicate published instance exited 1 with the existing already-running diagnostic and no extra UI. The canonical published exe then updated elevated startup through its own configuration helper. Task Scheduler verification confirmed the canonical path, --minimized and highest privileges with no duplicate Run entry. After normal cleanup of the interactive published process, invoking the real scheduled task launched that exact exe. A native diagnostic queried the actual process image and TokenElevation: Elevated=1. Settings remained hidden/minimized, and original calibration JSON matched byte-for-byte. The user subsequently confirmed the scheduled published app works after the requested physical-display check. A new Windows restart after path migration has not been performed; the real task action has been executed and verified directly.

Using the release: open the existing tray icon for Settings, or launch the published exe directly when the app is stopped. --minimized and --restore remain available. Before republishing over the same file, exit the running app normally so Windows releases its executable. Build outputs and isolated test copies remain ignored by Git; the publish profile and verification notes are source-controlled. The user confirmed the release works, completing the Phase 4 implementation and authorizing its milestone commit under the repository workflow. Separate regression checks remain in TODO.md.

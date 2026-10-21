# CursorEqualizer — Build Rules (read every session)

C# (.NET 8) Windows Forms System Tray Utility + Win32 P/Invoke (`user32.dll`, `shcore.dll`). Follow the existing patterns already in the repo and extend them — don't reinvent structure that's already there.

---

## Working conventions

- **Emphasis uses single quotes.** When emphasizing a word or phrase in prose, wrap it in 'single quotes' rather than bolding or double-quoting it.
- **Never one-shot across phases.** Build, compile (`dotnet build`), and run (`dotnet run`) iteratively. Verify the current phase works on the physical monitors and wait for user confirmation before moving to the next phase.

---

## Read these at the start of EVERY session

Read both before touching code — don't work from memory of a past session:

1. **`references/DOCUMENTATION.md`** — stack, file layout, Win32 P/Invoke signatures, registry paths, cursor scaling formula, and build/publish commands. Check here before inventing a pattern or hardcoding a Win32 constant.
2. **`references/TODO.md`** — current development phase, what's done, what's left, and open bugs/questions. Check items off as they land and add new ones as they surface; this is the source of truth for 'what's next'.

Keep `DOCUMENTATION.md` and `TODO.md` current as part of the work, not as an afterthought — if a change makes either one wrong, fix it in the same pass. If these files do not exist yet on first run, create them immediately before writing application code.

---

## Project architecture & code structure (follow strictly)

Keep responsibilities cleanly separated so Win32 API calls, business logic, and UI never bleed into each other:

1. **`Program.cs` is thin** — it only initializes `SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)` before any UI creation, enforces a single-instance `Mutex`, hooks global exit/crash handlers, and launches the app context.
2. **`NativeMethods.cs` holds ALL P/Invoke declarations** — every `[DllImport("user32.dll")]` and `[DllImport("shcore.dll")]` signature, Win32 struct (`POINT`, `MONITORINFOEX`), and constant (`OCR_*`, `SPI_SETCURSORS`, `LR_LOADFROMFILE`) lives here. Never scatter `[DllImport]` attributes across other classes.
3. **`CursorManager.cs` owns the cursor & monitor engine** — reads active custom `.cur`/`.ani` paths from `HKCU\Control Panel\Cursors`, polls monitor transitions (`GetCursorPos` + `MonitorFromPoint`), calculates target pixel sizes, calls `LoadImageW` + `SetSystemCursor`, and handles cursor restoration.
4. **`SettingsManager.cs` owns persistence & startup** — manages `%AppData%\CursorEqualizer\settings.json`, Registry auto-start (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), and Task Scheduler elevated startup.
5. **`MainForm.cs` owns the UI & Tray Icon** — system tray `NotifyIcon`, context menu, and dark-mode per-monitor calibration sliders. UI components call into `CursorManager` and `SettingsManager` — never directly into `NativeMethods`.

**Smell test:** `MainForm.cs` should never import `System.Runtime.InteropServices` or call Win32 APIs directly.

---

## Critical Win32 & performance guardrails

Modifying system cursors globally in Windows requires strict adherence to these rules:

- **Cursor Restoration Safety (Non-Negotiable):** `SetSystemCursor` replaces system cursors globally for the current session. You 'must' guarantee that `SystemParametersInfoW(SPI_SETCURSORS, 0, IntPtr.Zero, 0)` (`0x0057`) is called on normal exit, Ctrl+C, `Application.ApplicationExit`, `SystemEvents.SessionEnding`, and unhandled exceptions so the user's default registry cursors are always restored.
- **State-Change Trigger Only:** Poll `GetCursorPos` + `MonitorFromPoint` every ~15ms (66Hz), but 'only' invoke `LoadImageW` and `SetSystemCursor` when `currentMonitorHandle != lastMonitorHandle`. Never call `SetSystemCursor` on every timer tick.
- **Handle Ownership with `SetSystemCursor`:** Windows destroys the `HCURSOR` handle passed to `SetSystemCursor`. Always pass a fresh handle created via `LoadImageW(..., LR_LOADFROMFILE)` or cloned via `CopyImage` on each monitor switch.
- **Per-Monitor DPI Awareness V2:** Must be the very first call in `Main()`. Without it, Windows virtualizes coordinates and `GetDpiForMonitor` returns inaccurate values.

---

## Phased development workflow

Do not jump ahead

# CursorEqualizer — Build Rules (read every session)

C# (.NET 8) Windows Forms System Tray Utility + Win32 P/Invoke (`user32.dll`, `shcore.dll`). Follow the existing patterns already in the repo and extend them — don't reinvent structure that's already there.

---

## Working conventions

- **Commit per completed feature/phase/TODO, NEVER push.** Whenever a feature, TODO item, or development phase is completed, compiled, and verified working, stage the changes (including updated `references/TODO.md` and `references/DOCUMENTATION.md`) and run `git commit` with a clean, descriptive commit message. **Never run `git push`** — pushing to the remote repository is strictly reserved for the user.
- **Emphasis uses single quotes.** When emphasizing a word or phrase in prose, wrap it in 'single quotes' rather than bolding or double-quoting it[cite: 1].
- **Never one-shot across phases.** Build, compile (`dotnet build`), and run (`dotnet run`) iteratively. Verify the current phase works on the physical monitors, wait for user confirmation, commit the completed milestone locally, and only then move to the next phase.

---

## Read these at the start of EVERY session

Read both before touching code — don't work from memory of a past session[cite: 1]:

1. **`references/DOCUMENTATION.md`** — stack, file layout, Win32 P/Invoke signatures, registry paths, cursor scaling formula, and build/publish commands. Check here before inventing a pattern or hardcoding a Win32 constant[cite: 1].
2. **`references/TODO.md`** — current development phase, what's done, what's left, and open bugs/questions. Check items off as they land and add new ones as they surface; this is the source of truth for 'what's next'[cite: 1].

Keep `DOCUMENTATION.md` and `TODO.md` current as part of the work, not as an afterthought — if a change makes either one wrong, fix it in the same pass before creating your local commit[cite: 1]. If these files do not exist yet on first run, create them immediately before writing application code.

---

## Project architecture & code structure (follow strictly)

Keep responsibilities cleanly separated so Win32 API calls, business logic, and UI never bleed into each other:

1. **`Program.cs` is thin** — it only initializes `SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)` before any UI creation, enforces a single-instance `Mutex`, hooks global exit/crash handlers, and launches the app context.
2. **`NativeMethods.cs` holds ALL P/Invoke declarations** — every `[DllImport("user32.dll")]` and `[DllImport("shcore.dll")]` signature, Win32 struct (`POINT`, `MONITORINFOEX`), and constant (`OCR_*`, `SPI_SETCURSORS`, `LR_LOADFROMFILE`) lives here. Never scatter `[DllImport]` attributes across other classes.
3. **`CursorManager.cs` owns the cursor & monitor engine** — reads active custom `.cur`/`.ani` paths from `HKCU\Control Panel\Cursors`, polls monitor transitions (`GetCursorPos` + `MonitorFromPoint`), calculates target pixel sizes, calls `LoadImageW` + `SetSystemCursor`, and handles cursor restoration.
4. **`SettingsManager.cs` owns persistence & startup** — manages `%AppData%\CursorEqualizer\settings.json`, Registry auto-start (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), and Task Scheduler elevated startup.
5. **`MainForm.cs` owns the UI & Tray Icon** — system tray `NotifyIcon`, context menu, and dark-mode per-monitor calibration sliders. UI components call into `CursorManager` and `SettingsManager` — never directly into `NativeMethods`.

**Smell test:** `MainForm.cs` should never import `System.Runtime.InteropServices` or call Win32 APIs directly.

---

## Critical Win32 & performance guardrails

Modifying system cursors globally in Windows requires strict adherence to these rules:

- **Cursor Restoration Safety (Non-Negotiable):** `SetSystemCursor` replaces system cursors globally for the current session. You 'must' guarantee that `SystemParametersInfoW(SPI_SETCURSORS, 0, IntPtr.Zero, 0)` (`0x0057`) is called on normal exit, Ctrl+C, `Application.ApplicationExit`, `SystemEvents.SessionEnding`, and unhandled exceptions so the user's default registry cursors are always restored.
- **State-Change Trigger Only:** Poll `GetCursorPos` + `MonitorFromPoint` every ~15ms (66Hz), but 'only' invoke `LoadImageW` and `SetSystemCursor` when `currentMonitorHandle != lastMonitorHandle`. Never call `SetSystemCursor` on every timer tick.
- **Handle Ownership with `SetSystemCursor`:** Windows destroys the `HCURSOR` handle passed to `SetSystemCursor`. Always pass a fresh handle created via `LoadImageW(..., LR_LOADFROMFILE)` or cloned via `CopyImage` on each monitor switch.
- **Per-Monitor DPI Awareness V2:** Must be the very first call in `Main()`. Without it, Windows virtualizes coordinates and `GetDpiForMonitor` returns inaccurate values.

---

## Phased development & Git commit workflow

Do not jump ahead. For every completed feature, TODO item, or phase:
1. Build and test via `dotnet build` / `dotnet run`.
2. Update `references/TODO.md` (check off completed items) and `references/DOCUMENTATION.md` (if architecture/APIs changed)[cite: 1].
3. Stage and commit locally (`git add -A && git commit -m "<type>: <concise summary>"`), **without** running `git push`.

### Phases:
1. **Phase 1 (Core Win32 CLI Proof-of-Concept):** Console loop that detects monitors/DPI, reads custom cursors from Registry, swaps cursor pixel sizes on monitor boundary crossing, and restores cursors on Ctrl+C. Run, verify in terminal, update `TODO.md`[cite: 1], and commit locally.
2. **Phase 2 (Calibration Math, JSON Persistence & Display Events):** Implement `SettingsManager.cs`, auto-formula vs. manual pixel override (`16px`–`128px`), and `SystemEvents.DisplaySettingsChanged` listener. Verify, update `TODO.md`[cite: 1], and commit locally.
3. **Phase 3 (System Tray & Dark-Mode Calibration UI):** Convert to `WinExe`, build tray icon/menu and dark-mode calibration window with live pixel sliders per monitor. Verify with user, update `TODO.md`[cite: 1], and commit locally.
4. **Phase 4 (Auto-Startup & Single-File Publish):** Add Registry and Task Scheduler startup toggles, verify standalone `.exe` build via:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

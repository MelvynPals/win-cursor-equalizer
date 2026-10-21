Act as an expert Windows Systems & C# (.NET 8) developer. We are going to build a lightweight Windows System Tray application called "CursorEqualizer" that dynamically equalizes the visual size of my custom mouse cursors across multiple monitors with different DPI scales and physical screen sizes.

### STRICT EXECUTION RULE: PHASED DEVELOPMENT & LIVE TESTING (NO ONE-SHOTTING)
Do NOT write or generate the entire application at once. You must build, compile, and test this project iteratively in 4 strict phases.
- At the end of each phase, run `dotnet build` (and `dotnet run` where applicable) in the terminal to verify there are zero compiler or P/Invoke runtime errors.
- Wait for my confirmation that the current phase works on my physical monitors before writing any code for the next phase.

---

### PROBLEM CONTEXT
Windows DWM automatically scales cursors based on each monitor's DPI scale (e.g., 125% vs 150%), and different physical monitor sizes (e.g., a 24.5" 1440p monitor vs a 15.6" 1440p portable/laptop monitor) have different PPI. Even when using custom cursors (.cur / .ani), the cursor changes visual size when moving between monitors. 
To fix this, the app must detect when the mouse crosses into a different monitor and immediately call `LoadImageW` + `SetSystemCursor` with a custom pixel size (`cx`, `cy`) for that specific monitor, effectively counter-scaling Windows' DPI multiplication and PPI differences.

---

### DEVELOPMENT PHASES (EXECUTE ONE AT A TIME)

#### PHASE 1: Core Win32 Engine & CLI Proof-of-Concept (Build & Test First!)
Before building any UI, create a minimal console prototype to prove the Win32 P/Invoke logic works on my machine:
1. Initialize `CursorEqualizer.csproj` (.NET 8) and `NativeMethods.cs` with P/Invoke signatures for `user32.dll` and `shcore.dll`.
2. Call `SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)` at the very start of `Program.Main()`.
3. Enumerate connected monitors (`EnumDisplayMonitors`, `GetMonitorInfo`, `GetDpiForMonitor`) and print their handles, resolutions, and effective DPIs to the console.
4. Read my active custom cursor file paths from `HKEY_CURRENT_USER\Control Panel\Cursors` and map all standard cursor keys to their Win32 `OCR_` system IDs:
   - `Arrow` (32512), `IBeam` (32513), `Wait` (32514), `Crosshair` (32515), `UpArrow` (32516), `SizeNWSE` (32642), `SizeNESW` (32643), `SizeWE` (32644), `SizeNS` (32645), `SizeAll` (32646), `No` (32648), `Hand` (32649), `AppStarting` (32650).
   - Expand environment variables (e.g., `%SystemRoot%`, `%USERPROFILE%`) and verify the files exist.
5. Run a 15ms (~66Hz) polling loop using `GetCursorPos` and `MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST)`.
6. **State-Change Trigger Only:** When `currentMonitorHandle != lastMonitorHandle`, calculate the counter-scaled pixel size and call `LoadImageW(IntPtr.Zero, filePath, IMAGE_CURSOR, targetPx, targetPx, LR_LOADFROMFILE)` + `SetSystemCursor` for each mapped cursor.
7. Hook `Console.CancelKeyPress` (Ctrl+C) and `AppDomain.CurrentDomain.ProcessExit` to ALWAYS call `SystemParametersInfoW(SPI_SETCURSORS, 0, IntPtr.Zero, 0)` (`0x0057`) so my default cursors are cleanly restored on exit.
8. **STOP ACTION:** Compile and run Phase 1 in the terminal. Ask me to move my mouse between monitors to verify the cursor resizes properly and restores cleanly on Ctrl+C. Do not proceed to Phase 2 until I confirm.

#### PHASE 2: Calibration Math, JSON Persistence & Display Events
Once Phase 1 is verified:
1. Build `SettingsManager.cs` to save/load per-monitor settings in `%AppData%\CursorEqualizer\settings.json`.
2. Support two sizing modes per monitor:
   - **Auto Mode (Math Formula):** `targetPx = round(BasePx * (PrimaryMonitorDPI / CurrentMonitorDPI) * (CurrentMonitorDiagonalInches / PrimaryMonitorDiagonalInches))`
   - **Manual Override Mode:** Explicit `targetPx` (`16px` to `128px`) per monitor ID.
3. Add `SystemEvents.DisplaySettingsChanged` listener so plugging/unplugging a monitor or changing resolution refreshes monitor handles and DPIs automatically.
4. **STOP ACTION:** Run `dotnet build`, verify settings persistence works, and wait for my confirmation.

#### PHASE 3: System Tray App & Dark-Mode Calibration UI
Once Phase 2 is verified:
1. Convert the project output type to `WinExe` (Windows Forms) with a single-instance `Mutex`.
2. Build `MainForm.cs` with a System Tray Icon (`NotifyIcon`) and Context Menu ("Open Settings", "Pause / Resume Equalizer", "Reload Cursors from Registry", "Exit & Restore Default Cursors").
3. Build a clean, dark-mode Settings Window that lists each detected monitor (Name, Resolution, DPI Scale %) with:
   - Input box for Physical Diagonal Size (e.g., `24.5` vs `15.6` inches).
   - Live Pixel Size Slider (`16px` - `128px`) with instant live preview so I can visually eyeball and lock in the exact size.
4. Ensure `Application.ApplicationExit` and `SystemEvents.SessionEnding` trigger `SPI_SETCURSORS` restoration.
5. **STOP ACTION:** Build and launch the UI for me to test the live sliders. Wait for my confirmation.

#### PHASE 4: Auto-Startup Integration & Single-File `.exe` Packaging
Once Phase 3 is verified:
1. Add Windows Auto-Startup toggles to the UI and Tray Menu:
   - Standard Startup: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` (`"CursorEqualizer"` with `--minimized` flag).
   - Elevated Startup (Admin Mode): Option to register via Windows Task Scheduler (`schtasks`) with highest privileges so cursor scaling works seamlessly over Administrator windows like Task Manager.
2. Run `dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true` and verify the final standalone `.exe`.

---

**Start now with PHASE 1 ONLY.** Create the project structure, write the core Win32 console proof-of-concept, run `dotnet build`, and let me test it.
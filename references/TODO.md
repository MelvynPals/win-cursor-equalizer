---

### Starter Templates for `references/DOCUMENTATION.md` and `references/TODO.md`

To make sure Claude has the exact context files it expects on Session 1[cite: 1], drop these two files into a `references/` folder in your project root:

#### 1. `references/DOCUMENTATION.md`
```markdown
# CursorEqualizer — Documentation

## Overview
A lightweight C# (.NET 8) Windows System Tray utility that equalizes the visual size of custom mouse cursors (`.cur` / `.ani`) across multiple monitors with different DPI scaling (e.g., 125% vs 150%) and physical dimensions (e.g., 24.5" 1440p vs 15.6" 1440p).

## Tech Stack
- **Framework:** .NET 8.0 (`net8.0-windows`, WinForms for Tray & Calibration UI)
- **System APIs:** Win32 `user32.dll` and `shcore.dll` via P/Invoke
- **Config Storage:** `%AppData%\CursorEqualizer\settings.json`

## Directory Layout
```text
/
├── CLAUDE.md
├── references/
│   ├── DOCUMENTATION.md
│   └── TODO.md
└── src/ (or root)
    ├── CursorEqualizer.csproj
    ├── Program.cs          # DPI Awareness V2 init, Mutex, crash/exit hooks
    ├── NativeMethods.cs    # P/Invoke declarations & Win32 constants
    ├── CursorManager.cs    # Registry cursor reader, monitor polling, SetSystemCursor engine
    ├── SettingsManager.cs  # JSON config, Registry Run key, Task Scheduler admin task
    └── MainForm.cs         # System Tray icon & per-monitor calibration UI

#### 2. `references/TODO.md`
```markdown
# CursorEqualizer — TODO & Phase Tracker

## Phase 1: Core Win32 Engine & CLI Proof-of-Concept
- [ ] Initialize `CursorEqualizer.csproj` (.NET 8) and `NativeMethods.cs`
- [ ] Set `DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2` on startup
- [ ] Enumerate monitors and print handles, resolutions, and effective DPIs
- [ ] Read active custom cursor paths from `HKCU\Control Panel\Cursors`
- [ ] Implement 15ms monitor transition polling (`GetCursorPos` + `MonitorFromPoint`)
- [ ] Apply counter-scaled cursors via `LoadImageW` + `SetSystemCursor` on monitor change
- [ ] Hook `Ctrl+C` and `ProcessExit` to restore cursors via `SPI_SETCURSORS` (`0x0057`)
- [ ] **Milestone Check:** Run and verify cursor scaling across monitors in terminal

## Phase 2: Calibration Math, JSON Persistence & Display Events
- [ ] Create `SettingsManager.cs` (`%AppData%\CursorEqualizer\settings.json`)
- [ ] Implement Auto Formula Mode vs. Manual Pixel Override (`16px`–`128px`)
- [ ] Hook `SystemEvents.DisplaySettingsChanged` to refresh monitor topology dynamically
- [ ] **Milestone Check:** Verify config saving/loading and display change handling

## Phase 3: System Tray App & Dark-Mode Calibration UI
- [ ] Switch project to `WinExe` with single-instance `Mutex`
- [ ] Implement `NotifyIcon` tray menu (Open Settings, Pause/Resume, Reload Cursors, Exit)
- [ ] Build dark-mode calibration window with per-monitor diagonal inputs and live pixel sliders
- [ ] **Milestone Check:** Verify live slider preview and tray behavior

## Phase 4: Auto-Startup Integration & Single-File Packaging
- [ ] Add `HKCU\...\Run` Registry startup toggle (`--minimized`)
- [ ] Add Task Scheduler elevated startup option (for scaling over Admin windows)
- [ ] Publish single-file executable (`dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true`)
# CursorEqualizer TODO

Current phase: Phase 3 verified by the user on 2026-10-23 and committed locally at their request. Clean build and all 29 existing checks pass. User confirmed all four requested checks: live sliders, moving the window between displays, close/reopen from tray, and tray reload/pause/resume/exit restoration. Phase 4 is next; not started. Phase 2 is committed as 33865de.

## Next phase
- Phase 4: standard Registry Run startup with --minimized, elevated Task Scheduler startup, and single-file win-x64 packaging.
- Phase 3 UI checks were confirmed; Phase 4 implementation has not started.
- Continue regression checks for restart persistence, smaller-window scrolling, and physical hotplug as relevant.

## Remaining regression checks
- Saved Calibrated references: D1=32 px at 120 DPI / D2=23 px at 192 DPI. D2 targets 31 px at 150%, 23 px at 200%.
- User confirmed comparable appearance at both scales. Logs were not supplied to distinguish Windows-event delivery from fallback DPI detection.
- Check unplug/reconnect, primary-display changes, or resolution changes; confirm monitor settings persist and no polling-tick cursor replacement occurs. These physical regression cases remain untested.
- D1 diagonal=24.5 inches is user-supplied. D2=16 inches is provisional; confirm exact laptop diagonal later.

## Phase 1: Win32 console prototype
- [x] Create .NET 8 project and centralized native declarations.
- [x] Initialize Per Monitor V2 awareness as the first startup call.
- [x] Enumerate monitor handles, resolutions, effective DPI, and primary monitor.
- [x] Read and validate 13 registry cursor roles and expanded paths.
- [x] Poll at 15 ms; apply fresh cursor handles only on monitor transitions.
- [x] Restore on normal exit, Ctrl+C, ProcessExit, SessionEnding, ApplicationExit, and exceptions.
- [x] Build with zero warnings/errors and run native API smoke checks.
- [ ] User verifies sizes, animation, cursor roles, and restoration on physical monitors.
- [ ] Compare app off/on with the secondary monitor set to 200% before launching the app.
- [x] Commit Phase 1 diagnostic checkpoints locally (239a8ab, d663c83); never push. Exact physical equality remained unresolved; user authorized proceeding to Phase 2.

## Phase 2 (authorized 2026-10-23)
- [x] SettingsManager: validated JSON at %AppData%\CursorEqualizer\settings.json, atomic save, malformed-file preservation.
- [x] Auto formula with physical diagonals and clamping; manual 16-128 px overrides.
- [x] Monitor-interface identity persistence, retaining disconnected monitor entries.
- [x] DisplaySettingsChanged enqueues debounced monitor/DPI refresh; unknown handles retry and R triggers the same path.
- [x] Thin Program.cs; ConsoleApp.cs owns console configuration and lifecycle.
- [x] Clean build, 20 focused checks, native smoke/restoration, cross-process reload, and R refresh checks.
- [x] Add Calibrated mode preserving a saved pixel/DPI reference while fixed Manual mode remains unchanged.
- [x] Once-per-second DPI-change detection queues refresh as fallback to Windows display events.
- [x] Expand to 29 focused checks; verify calibrated reference persistence and saved native smoke/restoration.
- [x] User confirms Calibrated mode looks approximately the same at 150% and 200%.
- [x] Cross-process persistence and live scaling change behavior verified (event versus fallback source not identified).
- [ ] Regression: verify physical hotplug/resolution/primary-monitor changes and distinguish Windows events from fallback detection.
- [x] Commit Phase 2 milestone locally after user's visual confirmation; never push.

## Phase 3 (authorized 2026-10-23)
- [x] Convert to WinExe; default launch uses TrayAppContext and WinForms timers.
- [x] MainForm owns NotifyIcon/menu, open/hide behavior, pause/resume, registry reload, and exit.
- [x] Dark calibration UI with per-monitor resolution/scale, diagonal input, mode selector, and live 16-128 px sliders.
- [x] Preserve Calibrated DPI anchors; slider edits capture current DPI, with 250 ms autosave and status feedback.
- [x] Refresh monitor cards on display changes; synchronize controls affected by automatic-mode settings.
- [x] Restore scheme on pause; resume marks new replacements dirty so final exit restores again.
- [x] Build: zero warnings/errors; GUI smoke exits 0 and restores; all 29 Phase 2 checks pass.
- [x] Inspect layout at 125%; UI Automation Pause/Resume and keyboard slider save/restoration checks passed.
- [x] User verifies live sliders and moving the window between displays.
- [x] User verifies close-to-tray/reopen and tray reload/pause/resume/exit restoration.
- [x] Commit verified Phase 3 locally at user's request; never push.

## Phase 4 (next; not started)
- [ ] Registry Run startup with --minimized.
- [ ] Elevated Task Scheduler startup option.
- [ ] Publish and verify single-file win-x64 executable.

## Open checks
- Requested Auto formula computes about 13.06 px (clamped to 16) for 24.5-inch D1 at 125% and provisional 16-inch D2 at 200%. This contradicts prior visual evidence that 20 px was already too small. Auto math is implemented as specified; the current test instead uses empirical Calibrated references. Exact physical equality is still unmeasured.
- Manual overrides are fixed per monitor, not per scaling percentage; they do not promise equal appearance at both 150% and 200%.
- Phase 1 trials found inverse-DPI compensation too small and fixed sizing inconsistent across DPI settings. Phase 2 Calibrated mode now has user confirmation of approximate visual consistency at 150% and 200%; this does not validate the separate physical-diagonal Auto formula.
- Physical size compensation is Phase 2. Equal pixel requests do not guarantee equal physical sizes on screens with different pixel density.
- Check animated .ani behavior and actual requested pixel sizing on the installed theme.
- Force kill cannot run cleanup; use the documented --restore command for recovery.

## Phase 2 verification (2026-10-23)
- `dotnet build`: zero warnings/errors.
- `dotnet run --project tests/Phase2Checks/Phase2Checks.csproj`: 29 checks passed, covering formula/clamps, fixed Manual behavior, Calibrated 23 -> 31 -> 23 behavior, settings roundtrip, case-insensitive identity, and invalid-file preservation.
- AppData settings reloaded across native application runs; Auto/Manual/Calibrated configuration and cursor restoration passed.
- R refresh path re-read monitor state and reapplied once, then normal exit restored the registry scheme.
- User's final Calibrated visual result: both scales look about the same. Physical hotplug/resolution regression scenarios remain pending.

## Phase 1 verification (2026-10-22)
- `dotnet build`: zero warnings and errors.
- `dotnet run --no-build -- --smoke-test`: exit 0; all 13 Glitch .cur/.ani files loaded; registry scheme restored after two seconds.
- Interactive `dotnet run --no-build`: transitions observed between DISPLAY1 (2560x1440, 120 DPI, 32 px) and DISPLAY2 (2560x1440, 144 DPI, 27 px). Ctrl+C logged successful restoration (terminal host reported exit 1 for the interrupted command).
- Session-ending and crash hooks are implemented; logoff and forced exception scenarios have not been exercised.
- Equal-32px diagnostic revision: after the user stopped the original instance, standard `dotnet build` passed with zero warnings/errors. `dotnet run --no-build -- --smoke-test` exited 0, reported 32 px targets on both monitors, applied all 13 roles on DISPLAY1, and restored the registry scheme. Awaiting the user's cross-monitor visual comparison.
- Comparison/logging revision: `dotnet build` passed with zero warnings/errors. `dotnet run --no-build -- --smoke-test --inverse-dpi` exited 0; primary=120 DPI/32 px, secondary=192 DPI/20 px. All 13 roles on DISPLAY1 reported loaded=32x32 and SetSystemCursor success; restoration succeeded. The actual 20 px secondary load and its visual result still need verification.
- User supplied complete inverse-DPI logs: all 13 Display 2 roles loaded at 20x20, all 13 Display 1 roles at 32x32, replacement succeeded, and registry scheme restored. Display 2 looked smaller than Display 1.
- Temporary secondary-size diagnostic: `dotnet build` passed with zero warnings/errors. `dotnet run --no-build -- --smoke-test --test-secondary-px 26` exited 0, reported targets 32 px / 26 px at 125% / 200%, loaded all primary roles at 32x32, and restored successfully. Secondary 26 px visual comparison is pending.
- User's 26 px visual result: Display 2 still larger than Display 1, but smaller than Display 2 with the app off. Next trial: 23 px, keeping primary=32 px and scaling unchanged at 125%/200%.
- User's 23 px visual result: displays look about the same, but user is uncertain of exact equality. Treat as a provisional match at this configuration, not proof of automatic equalization across DPI settings. Suggested next check: compare visible arrow height with a physical ruler on the same desktop background; do not compare transparent bitmap canvas dimensions as physical size.
- End-of-session clarification: Display 2 is back at 150%. The precise scale during the user's 26 px and 23 px visual comparisons was not explicitly confirmed. Repeat those comparisons at a fixed 200% before drawing a sizing conclusion.

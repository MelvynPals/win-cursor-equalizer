# CursorEqualizer TODO

Current phase: Phase 1 sizing investigation. User reports the equal-32px diagnostic looks about the same at 125%/150%, but the original difference is too subtle to establish success. User requested a checkpoint commit before a stronger 200% comparison. Phase 2 remains on hold.

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
- [ ] Commit verified Phase 1 locally; never push.

## Phase 2 (await Phase 1 confirmation)
- [ ] SettingsManager: JSON at %AppData%\CursorEqualizer\settings.json.
- [ ] Auto formula with physical diagonals; manual 16-128 px overrides.
- [ ] DisplaySettingsChanged refreshes monitors.
- [ ] User verifies persistence and display changes.

## Phase 3 (await Phase 2 confirmation)
- [ ] WinExe, tray menu, pause/resume, reload, and exit.
- [ ] Dark calibration UI with diagonal inputs and live sliders.
- [ ] User verifies UI.

## Phase 4 (await Phase 3 confirmation)
- [ ] Registry Run startup with --minimized.
- [ ] Elevated Task Scheduler startup option.
- [ ] Publish and verify single-file win-x64 executable.

## Open checks
- Initial inverse-DPI compensation made the second monitor cursor smaller than the primary and its original appearance. Equal 32 px requests look approximately matched at 125%/150%; the user reports a mismatch at 200%. A controlled comparison with 200% configured before startup is pending.
- Physical size compensation is Phase 2. Equal pixel requests do not guarantee equal physical sizes on screens with different pixel density.
- Check animated .ani behavior and actual requested pixel sizing on the installed theme.
- Force kill cannot run cleanup; use the documented --restore command for recovery.

## Phase 1 verification (2026-10-22)
- `dotnet build`: zero warnings and errors.
- `dotnet run --no-build -- --smoke-test`: exit 0; all 13 Glitch .cur/.ani files loaded; registry scheme restored after two seconds.
- Interactive `dotnet run --no-build`: transitions observed between DISPLAY1 (2560x1440, 120 DPI, 32 px) and DISPLAY2 (2560x1440, 144 DPI, 27 px). Ctrl+C logged successful restoration (terminal host reported exit 1 for the interrupted command).
- Session-ending and crash hooks are implemented; logoff and forced exception scenarios have not been exercised.
- Equal-32px diagnostic revision: after the user stopped the original instance, standard `dotnet build` passed with zero warnings/errors. `dotnet run --no-build -- --smoke-test` exited 0, reported 32 px targets on both monitors, applied all 13 roles on DISPLAY1, and restored the registry scheme. Awaiting the user's cross-monitor visual comparison.

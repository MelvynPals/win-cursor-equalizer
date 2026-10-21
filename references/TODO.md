# CursorEqualizer TODO

Current phase: Phase 1 sizing investigation, stopped for the day at the user's request. Display 2 is back at 150%. Fixed 32 px was too large at 200%; inverse-DPI 20 px was too small. Later visual trials found 26 px too large and 23 px approximately matched, but the timing of the return to 150% is unconfirmed. Do not treat 23 px as a verified 200% calibration. Phase 2 remains on hold.

## Resume next session
- Keep the primary at 125%. Set Display 2 to 200% before starting the app and keep that scale fixed for the full comparison.
- Exit/restart the Phase 1 app after every Windows scale change; its DPI snapshot is taken at startup.
- Confirm Display 2 reports 192 DPI / 200%, then repeat `dotnet run -- --test-secondary-px 23` and compare app off/on on the desktop.
- Log the scale with each size result. Verify 150% in a separate test afterwards; don't assume the same target works at both scales.
- The user requested a checkpoint commit of current diagnostics before returning tomorrow; this does not mark Phase 1 fully verified.

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
- Initial inverse-DPI compensation made the second monitor cursor smaller than the primary and its original appearance at 150%. The 200% test found 32 px too large and 20 px too small. Later 26 px / 23 px visual results need a repeat at a confirmed, fixed scale because the user has returned to 150%. User's Display 2 logs confirm all 13 roles loaded at 20x20 and SetSystemCursor succeeded. Exact physical equality is unmeasured; no general sizing formula is verified yet.
- Physical size compensation is Phase 2. Equal pixel requests do not guarantee equal physical sizes on screens with different pixel density.
- Check animated .ani behavior and actual requested pixel sizing on the installed theme.
- Force kill cannot run cleanup; use the documented --restore command for recovery.

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

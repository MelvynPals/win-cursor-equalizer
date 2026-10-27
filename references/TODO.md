# CursorEqualizer TODO

Current work: the owner confirms the 1.0.1 installation worked and retained the current calibration (2026-10-28). The live feedback link and version bump are built; specific browser-link/version-label checks remain pending. Earlier installer integration and recipient/payment checks remain below; no release tag is recorded.

## Feedback link update 1.0.1 (authorized 2026-10-28)
- [x] Connect the sidebar's Click Here link to https://cursor-equalizer.emjaypaleguin.com/feedback and bump project version to 1.0.1.
- [x] Build/publish app and setup; inspect version metadata. Release build has zero warnings/errors. Portable and self-contained publishes succeed with NU1900 vulnerability-audit connectivity warnings; Inno Setup compiles artifacts/installer/CursorEqualizer-1.0.1-Setup.exe successfully. All three app outputs report ProductVersion 1.0.1 / FileVersion 1.0.1.0. `git diff --check` passes.
  - `dotnet run -c Release --no-build -- --smoke-test` reaches the existing-instance guard; the current app is left running. Runtime/browser-link and owner verification remain pending. The web inspection tool could not load the destination; owner reports the form is live. No feedback was submitted.
- [x] Owner confirms the 1.0.1 installation worked and current calibration was retained (2026-10-28).
- [ ] Owner separately verifies the browser link and Version 1.0.1 label; these details were not explicitly reported with installation confirmation. Save the installed update locally without claiming these checks or creating a release tag; never push.

## Installable Windows release (authorized 2026-10-25)
- [x] Add separate self-contained InstallerWinX64 publish profile and tools/Build-Installer.ps1; keep dotnet run and the existing portable publish profile.
- [x] Add Inno Setup per-user installation with stable identity/path, Start menu shortcut, uninstall registration, versioned setup executable and downgrade protection.
- [x] Require normal exit through the existing Local\\CursorEqualizer mutex before install/upgrade/uninstall; never force-close the engine.
- [x] Add startup-only lifecycle helpers before the engine/mutex. Fresh install uses the selected standard startup mode; upgrades preserve off/standard registration and migrate elevated startup to standard. Uninstall removes startup registration; calibration JSON is untouched, including malformed files.
- [x] Build zero warnings/errors; 52 focused checks pass, including migration choices and startup-only settings preservation.
- [x] Owner confirms the installer installs the app, the installed app opens and works, it appears in the Windows Startup apps list, and uninstall through Windows Settings succeeds (2026-10-25).
- [ ] Compile the self-contained setup; verify fresh install, search shortcut, installed-app metadata, same-version reinstall/new-version upgrade, downgrade rejection and uninstall cleanup.
  - Setup 1.0.0 compiled (49,429,424 bytes; self-contained payload 162,008,567 bytes). First compiler resource-update attempt failed; retry succeeded without changing antivirus settings. An isolated 1.0.1 upgrade-test payload/setup was also built under .local; it has not been installed or released.
  - Install and uninstall correctly refuse the running engine (exit 1). Fresh install creates the exact installed path, Start shortcut and one HKCU uninstall entry (version 1.0.0, publisher MelvynPals). Installed GUI smoke exits 0 and logs restoration. Calibration hash stays CBE1ADC8C5A5F11FC14BDD46568FC6BD48C621F8C1D66B76852096F5231024B5.
  - Initial automated migration UAC was not approved: setup returned 10 with an explicit startup error and preserved the legacy elevated task XML. The owner subsequently installed, used and uninstalled successfully and confirmed Startup apps visibility. The migration sequence and post-uninstall Registry/task/shortcut/settings state were not separately reported or audited.
- [ ] Verify Task Manager / Windows Startup apps toggles control standard startup and survive an upgrade; elevated Task Scheduler startup is separate.
- [x] Owner explicitly requests committing the current implementation for now, including versioning and the about label, with unfinished verification documented; checkpoint locally without a release tag or push.
- [ ] Separately verify Start search, Task Manager enable/disable and restart behavior, upgrade/downgrade handling, and post-uninstall startup/shortcut cleanup plus settings preservation. The basic install/use/Settings-uninstall flow is owner-confirmed above.

## Repository README (authorized 2026-10-25)
- [x] Refresh the main image with the owner's 2026-10-28 screenshot; add current 1.0.1 installer guidance, product website and account-free feedback link, and update the tools/Windows requirements descriptions. Preserve qualified 1.0.0 performance evidence.
  - Inspect the supplied image; verify the repository copy has an identical SHA-256, all README local links resolve, and `git diff --check` passes. Documentation/assets only; no app rebuild or runtime change.
- [x] Adapt the supplied external README template to CursorEqualizer's native .NET 8/WinForms stack, commands, architecture, calibration, startup, publishing and recovery.
- [x] Include the existing app screenshot and qualified measured performance; credit only the personal maintainer and remove company-template ownership.
- [x] Validate local links/image, command references, and absence of unrelated template content; commit the README milestone locally without pushing.

## Performance evidence (authorized 2026-10-25)
- [x] Add reusable external tools/Measure-Performance.ps1 sampler and retain raw CSV/JSON with executable hash, version, source state and machine details.
- [x] Record two 60-sample runs; correct the first run's unverified Settings-open label to initial-observation. Verify Settings is hidden before/after the second background tray run.
- [x] Independently recompute CPU from raw background sample deltas; document CPU, working set, private committed memory, handles and threads in references/PERFORMANCE.md.
- [x] Record qualified claim wording and limits: approximately 0.02% total CPU and 83 MiB resident RAM in this 61-second background tray sample on this PC.
- [x] Record the owner's requested rapid monitor-crossing workload for 10.155 seconds: 0.8366% average total CPU, 1.0582% highest one-second CPU sample, 84.30 MiB average working set. Preserve ten raw samples and JSON; crossing count and latency are not instrumented.
- [ ] Optional follow-up before broader performance claims: controlled repeated monitor crossings, longer memory-stability sampling, startup/latency/energy measurements and other PCs.

## Application versioning (authorized 2026-10-25)
- [x] Count existing history: 14 commits = initial commit + 12 updates + one merge; no existing tags.
- [x] Set explicit 1.0.0 baseline and document patch/minor/major numbering independently of settings schema version.
- [x] Add CHANGELOG.md with historical commit references; leave release notes under Unreleased until verification.
- [x] Show the compiled application version in Support & about using Application.ProductVersion and existing small muted text styling.
- [ ] Owner confirms the version label on both displays; the installed copy now includes the label and is open for review.
  - Label change builds and publishes to .local/versioning-1.0.0 with zero errors and the existing NU1900 audit warning. Runtime smoke attempt again reaches the existing-instance guard; visual verification remains pending.
  - Follow-up: republished the canonical bin/Release/net8.0-windows/win-x64/publish executable. GUI smoke with settings access passed and logged restoration. Removed a stalled sandbox debug process after its restoration log; the canonical published app is now running with Support & about open. UI Automation confirms Version 1.0.0 is onscreen; .local/version-about.png was visually inspected at 125%. Second-display and owner confirmation remain pending.
- [x] Build: zero warnings/errors. Publish to .local/versioning-1.0.0 succeeds; NU1900 audit connectivity warning persists after network-enabled retry. Debug and published exe report ProductVersion=1.0.0 / FileVersion=1.0.0.0.
- [ ] Run the versioned build and obtain owner confirmation on physical monitors, following AGENTS.md.
  - `dotnet run --no-build -- --smoke-test` reached the single-instance guard: an existing app is running. No cursor runtime/restoration pass is claimed for this change.
- [ ] Finalize dated 1.0.0 notes, commit locally and create annotated v1.0.0 tag after confirmation; never push.

## Support sidebar and branding (authorized 2026-10-24)
- [x] Add "Don't have a GitHub account? Click Here" below the GitHub account note; leave the browser destination unset as requested (2026-10-28).
  - `dotnet build` passes with zero warnings/errors; `git diff --check` passes. After dismissing stale duplicate-instance dialogs, `dotnet run --no-build` launches successfully. Owner confirms the added sentence looks good and requests the local commit (2026-10-28).
- [x] Obtain owner visual confirmation of the added feedback sentence and authorization to commit locally.
- [x] Supply the alternative feedback browser destination: https://cursor-equalizer.emjaypaleguin.com/feedback (2026-10-28).
- [x] Style Hide to tray as a primary white/black button at user request; clean build/publish and native screenshot verified.
- [x] Replace the sidebar's bright native scrollbar with a rounded dark rail/thumb aligned to the close-button centerline; retain native content wheel/focus scrolling and add drag, page click and keyboard input. Verify Home/End and real thumb dragging; exercise wheel input. Inspect 125% and 200% captures, including corrected DPI-transition clipping.
- [x] Reuse the dark scrollbar for the main calibration area; clip native chrome using a DPI-fitted viewport, size monitor rows to the visible viewport and keep header/footer fixed. Build/publish zero warnings/errors; GUI smoke restores cursors. Actual drag, wheel and Home/End pass; inspect references/main-scrollbar.png and main-scrollbar-200.png.
- [x] Confirm creator credit MelvynPals; choose Ko-fi for donations and GitHub issues for feedback.
- [x] Render the supplied references/icon.svg and generate multi-resolution Assets/AppIcon.ico plus embedded AppLogo.png; add a repeatable export script.
- [x] Embed the logo in the header, tray and window, and compile the icon into the executable.
- [x] Add a closable Support & about drawer, docked on wide windows and overlaid on narrow ones; add Escape close and profile credit.
- [x] Wire Buy me a coffee to the supplied Ko-fi handle: https://ko-fi.com/emjay0303.
- [x] Owner confirms using this repository for feedback and will make it public; wire the issue composer without changing repository visibility.
- [ ] Before public distribution, verify the repository is public with Issues enabled. Public API currently returns 404. Owner should check the PayPal destination in a normal browser.
- [x] Inspect native normal/narrow/wide/200% screenshots; verify close/reopen, Escape focus restoration, four-action Tab cycle, Pause/Resume and vertical-only scrolling.
- [x] Fix stale horizontal scroll styles after DPI changes and wrap the sidebar identity caption to available width.
- [x] Build/publish zero warnings/errors; all 46 existing checks pass. Final exe-only GUI smoke exits 0 with cursor restoration; actual executable icon extracts correctly and ICO contains nine resolutions.
- [x] Fix close glyph centering with symmetric painted strokes, removing font-bearing/baseline bias; native 125%/200% captures place glyph within 0.1px of button visual center.
- [x] Optically center logo artwork with translate(64 -16); keep original in icon-original.svg and export final references/icon.svg plus 1024px references/icon.png. Regenerate embedded PNG/ICO and published exe.
- [x] Centering changes build/publish without warnings/errors; close/reopen/Escape and final exe-only smoke/restoration pass.
- [x] Owner selects PayPal plus QR Ph after ruling out Stripe-based Buy Me a Coffee.
- [x] Prepare a QR Ph viewer with aspect-preserving sharp rendering, Close/Escape behavior and a successful build; no receiving data is fabricated.
- [x] Receive https://paypal.me/emjaypals and the GoTyme receiving QR; replace Ko-fi with Support with PayPal and Show QR Ph. Embed the exact original PNG (matching SHA-256).
- [x] Build/publish zero warnings/errors; GUI smoke exits 0 and restores cursors. Native PayPal/QR action presence and actual mouse QR opening verified; Close/reopen/Escape pass. Inspect references/support-qr-ph.png with the original recipient image visible and no clipping.
- [ ] User verifies a bank/wallet scan resolves the correct recipient and checks PayPal in their browser; additionally inspect QR dialog fitting on the 200% display. No payment is submitted automatically.
- [x] User explicitly authorizes committing the support sidebar, links, branding and optical-centering fixes locally; never push. External destination checks remain listed above.

## Design experiment
- [x] Create and switch to `new-design` from verified Phase 3.
- [x] Preserve the verified Phase 3 window screenshot at `references/design-before.png` for comparison.
- [x] Install Anthropic `frontend-design` into `.agents/skills`.
- [x] Install Emil Kowalski's main `emil-design-eng` skill into `.agents/skills`.
- [x] Install Taste's `design-taste-frontend` skill and bundled resources into `.agents/skills`.
- [x] Apply all three skills to the native WinForms UI: black surfaces, aligned display rows, prominent pixel readouts, painted sliders, segmented mode controls, and immediate feedback.
- [x] Rebuild display rows when window DPI changes; keep row widths fitted when scrollbars/layout change.
- [x] Capture `references/design-after.png` at 125% and `references/design-after-200.png` at 200% for comparison with the baseline.
- [x] Build with zero warnings/errors; all 29 engine checks pass; GUI smoke exits 0 and restores cursors.
- [x] Native UI Automation: pause/resume; keyboard slider increment/autosave and decrement with exact original settings restored. Inspect screenshots after moving between both displays.
- [x] Fix reported slider drag/paint defects: replace mixed native/custom trackbar with one fully painted input control; clear each frame and capture drags outside bounds.
- [x] Fix maximized stretching with a centered 960 logical px content cap; stack monitor details above controls below 660 logical px row width; repaint rounded surfaces on resize.
- [x] Exercise actual pointer dragging at 125% and 200%, 16/128 boundary clamps, 72 px midpoint, autosave, keyboard and exact original JSON restoration. Capture maximized, narrow, and post-drag evidence in references.
- [ ] User rechecks corrected pointer dragging/live preview, mode and diagonal editing, tray behavior, and responsive layout on both physical displays.
- [x] User authorizes committing the responsive redesign and slider fixes locally; never push.

## Next phase
- Phase 4 implementation completed with user confirmation: standard/elevated startup and portable single-file publishing. The installable Windows release is the current additional milestone; its remaining integration/owner checks are tracked above.
- Phase 3 UI checks were confirmed; standard and elevated startup both have user-confirmed restart verification.
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

## Phase 4 (standard startup authorized 2026-10-24)
- [x] SettingsManager owns per-user Registry Run registration; quote the executable path and append --minimized.
- [x] Add Start with Windows to the shared tray/More actions menu, refreshing the check state from the registry.
- [x] Validate executable path, reject host/DLL/relative/unsafe paths, and enforce the documented 260-character Run command limit.
- [x] Build with zero warnings/errors; all 37 calibration, persistence and startup-command checks pass.
- [x] Native UI enable/disable/re-enable verifies the exact Run value and removal; leave enabled for user verification.
- [x] Exit normally with registry cursor restoration; launch the registered executable with --minimized from TEMP, verify process alive, Settings hidden, and cursor replacements active.
- [x] User restarted and confirmed the app survived (automatic startup works).
- [ ] Separately verify post-restart tray Open Settings/Exit restoration and calibration details; these were not explicitly reported with the restart confirmation.
- [x] Commit the verified standard-startup milestone locally; never push.
- [x] Add Off / Standard / Elevated (administrator) options in the shared startup menu.
- [x] SettingsManager partial startup implementation: same-user SID task, interactive highest privileges, --minimized, explicit working directory, battery support, unlimited runtime and no scheduler hard termination.
- [x] Separate runas configuration helper before the app mutex/engine; keep the UI polling while waiting. Reject alternate-account elevation, handle UAC cancellation, and attempt rollback on configuration failure.
- [x] Build zero warnings/errors; 46 focused tests pass; GUI smoke exits 0 and restores cursors.
- [x] Real UAC-backed UI test: Standard -> Elevated -> Standard -> Elevated. Verify enabled task/current-user identity/highest interactive privileges and remove the duplicate Run entry; restoring Standard deletes the task and restores Run.
- [x] User restarted with elevated startup enabled and confirmed the app is active and working.
- [x] During publishing verification, query the actual scheduled published process token: Elevated=1.
- [ ] Separately verify post-restart tray Exit restoration on physical displays; this detail was not explicitly reported.
- [ ] Exercise UAC cancellation/failure rollback explicitly; these paths are implemented but were not forced in this run.
- [x] Commit the elevated-startup milestone after user verification; never push.
- [x] Add Properties/PublishProfiles/WinX64.pubxml for framework-dependent single-file win-x64 release publishing with embedded symbols and no trimming.
- [x] Publish successfully with zero warnings/errors; output contains only CursorEqualizer.exe (260,228 bytes). All 46 focused checks pass.
- [x] Copy only the exe into a fresh isolated test folder; local .NET 8 Windows Desktop runtime is installed.
- [x] Isolated exe-only copy from TEMP: --list, console/GUI smoke exit 0, cursor restoration logs, Pause/Resume, real pointer 72px midpoint and keyboard 73px, exact calibration restoration, and normal menu exit.
- [x] Published duplicate instance rejected with exit 1 without opening another UI.
- [x] Update elevated startup through the published app to the canonical published exe, verify --minimized/no Run duplicate, launch the real scheduled task and confirm its process path and elevated token.
- [x] Verify scheduled published startup is hidden/minimized and preserves exact original calibration JSON; leave it running.
- [x] User confirms the published app works after the requested monitor check.
- [ ] A later restart can additionally regress the migrated published executable path; direct task execution already passed.
- [x] Commit the verified publishing milestone locally after user confirmation; never push.

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

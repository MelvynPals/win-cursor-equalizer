# Changelog

## Unreleased

- Introduce explicit application version 1.0.0 and a patch/minor/major versioning workflow.
- Show the compiled application version in Support & about, below the app identity and above the creator credit.
- Add a separate self-contained Windows installer with Start menu search shortcut, installed-app/uninstall registration, stable upgrade identity, version checks and startup lifecycle helpers. Keep direct-executable development. The owner confirms install, launch/use, Startup apps visibility and Settings uninstall; upgrade/downgrade, startup toggle/restart and detailed cleanup checks remain pending.

1.0.0 is the baseline for the existing app. Basic installed runtime behavior is owner-confirmed; remaining release verification and a local release tag are pending. Earlier commits were development updates, not numbered releases.

## Development history before versioning

14 commits: one initial commit, 12 updates, and one merge commit. Updates, oldest first:

| Commit | Update |
| --- | --- |
| 239a8ab | Phase 1 cursor engine diagnostics |
| d663c83 | Cursor sizing comparison diagnostics |
| 33865de | Per-monitor calibration persistence and DPI adaptation |
| 583896e | Tray controls and dark calibration UI |
| 88ba364 | Design skills and baseline comparison |
| e6b63a1 | Responsive calibration redesign and reliable sliders |
| 4f55f22 | Standard Windows sign-in startup |
| 3e2dd7a | Elevated startup through Task Scheduler |
| d1422d5 | Windows x64 single-file publishing |
| f611ab7 | Support sidebar and custom branding |
| 4d9a660 | PayPal, QR Ph and sidebar scrolling |
| 57adce0 | Main calibration scrollbar styling |

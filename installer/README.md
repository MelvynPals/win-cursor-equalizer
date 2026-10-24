# Windows release installer

The publishable artifact is `artifacts/installer/CursorEqualizer-<version>-Setup.exe`. It installs the self-contained Windows x64 application for the current user. Development still uses `dotnet build` / `dotnet run` or the Debug apphost directly, without installation.

## Build

Install the .NET 8 SDK and Inno Setup 6.7+ or 7, then run from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-Installer.ps1
```

If the compiler is elsewhere, pass `-IsccPath 'C:\path\to\ISCC.exe'`. The helper also discovers the project-local compiler under `.local/installer-tools/InnoSetup`, the standard Inno Setup 6/7 locations, or PATH. It reads the numeric project Version, publishes with InstallerWinX64, checks the executable's product version, and supplies the same version to the installer. Do not change the stable AppId between releases.

The InstallerWinX64 profile bundles the Windows Desktop Runtime and native libraries. Users do not need a separate .NET installation. Native libraries can be extracted by the .NET single-file host at launch. Trimming is disabled. `artifacts/` is generated and ignored by Git. The original WinX64 profile remains available for framework-dependent portable/development publishing.

## Windows integration

| Item | Behavior |
| --- | --- |
| Install directory | `%LocalAppData%\Programs\CursorEqualizer` |
| Scope | Current Windows user; standard installation does not request elevation |
| Search | Current-user Start menu shortcut named CursorEqualizer, targeting the installed executable |
| Installed apps | CursorEqualizer name, version, MelvynPals publisher, icon and uninstaller in Windows Settings / Programs and Features |
| Upgrade identity | `MelvynPals.CursorEqualizer`, with a stable install path and one uninstall entry |
| Upgrade behavior | Replaces executable in place; rejects an older version; keeps calibration JSON |
| Startup | First-install checkbox selects standard startup via the existing HKCU Run integration |
| Removal | Removes owned app files, shortcut, uninstall registration and startup registration; keeps calibration JSON |

Setup and Uninstall use the existing `Local\CursorEqualizer` mutex to require that the app is exited normally before file changes. Close the app using 'Exit & Restore Default Cursors' from its tray menu. Closing Settings only hides it. Setup never force-kills the cursor engine; silent installation cancels if it is running.

## Startup and existing portable copies

Choose standard startup to manage CursorEqualizer in Task Manager > Startup apps or Settings > Apps > Startup. Windows owns its enabled/disabled approval state. The installer does not modify undocumented StartupApproved Registry values or reset a Windows-disabled entry during an upgrade. If startup was turned off from inside the app (Run entry removed), an upgrade keeps it off. To create the entry again, choose Standard in the app.

The existing Elevated option remains an advanced app feature backed by Task Scheduler. That mode does not use the Task Manager startup toggle. Selecting it later opts out of the standard Windows-managed startup path.

Startup setup uses `--installer-startup standard|disabled|preserve` before the app mutex and cursor engine. These helpers do not read or write calibration JSON. They use SettingsManager's existing same-user startup/UAC/rollback implementation; they never start another equalizer engine. Upgrades use preserve, keeping off/standard registration and converting a legacy elevated registration to standard. First install applies the selected checkbox, including migration from a portable startup path to the installed executable.

Removing an existing elevated task requires administrator approval, even though the install is per-user. UAC cancellation leaves the prior startup configuration intact. The installer reports incomplete startup configuration and returns exit code 10 rather than claiming full success; the app files remain installed. Resolve startup through the app before relying on it. Uninstall aborts before deleting app files if startup cleanup fails or UAC is cancelled. Reinstall a damaged/missing executable before retrying startup cleanup.

## Release verification

1. Build the app and run tests/Phase2Checks (including startup migration and malformed-settings preservation).
2. Build the installer and confirm payload/product/setup versions match.
3. Verify setup refuses to run while the engine mutex exists.
4. Install; inspect the Start shortcut target, Installed apps entry and startup command. Verify no duplicate elevated task remains after migration.
5. Run the installed app on both physical monitors. Verify Pause/Resume, calibration persistence and normal exit/restoration.
6. Disable/re-enable its standard entry in Task Manager. Upgrade while disabled and verify Windows' choice is preserved. Verify no duplicate Installed apps entry.
7. Test a newer-version upgrade, older-version rejection and uninstall. Confirm startup/shortcut removal and exact calibration preservation. Reinstall the intended final version for review.

Code signing is not configured in this repository. Setup and the application are unsigned artifacts; Windows may show an unknown-publisher/SmartScreen prompt. Signing and public distribution are separate release steps, and no certificate or signing identity is invented.

Sources: [Inno Setup AppId](https://jrsoftware.org/ishelp/topic_setup_appid.htm), [AppMutex](https://jrsoftware.org/ishelp/topic_setup_appmutex.htm), [non-administrative installation](https://jrsoftware.org/ishelp/topic_admininstallmode.htm), [Microsoft startup controls](https://support.microsoft.com/en-au/windows/experience/startup-boot/configure-startup-applications-in-windows), [.NET single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

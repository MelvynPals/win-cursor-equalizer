# Cursor schemes missing from Mouse Properties — investigation

Date: 6 November 2026 (Windows 11 Home 10.0.26200). Status: **registry entries restored; original trigger not proven.**

## Symptom

While the Windows app (`win-cursor-equalizer`, tray name CursorEqualizer) was running, the owner changed the cursor scheme in Mouse Properties. The app kept switching the pointer back to the previously active cursor when the mouse crossed to another monitor. Choosing **Reload Cursors from Registry** from the tray fixed that. Afterwards the previously used custom schemes were no longer in the Mouse Properties Scheme dropdown.

## What the app does (verified in source)

- `CursorManager.ReadCursorSources()` reads the active `.cur`/`.ani` paths from `HKCU\Control Panel\Cursors` **once at startup and on Reload**. Registry access is read-only (`OpenSubKey` without write access).
- On each monitor change, `Poll()` calls `LoadImageW` and `SetSystemCursor` with those **cached** paths. A scheme picked in Mouse Properties while the app runs is therefore overridden in memory by the old cached scheme. This explains the "changes back to the previous cursor on another screen" bug.
- `ReloadCursors()` calls `ReadCursorSources()` and then `SystemParametersInfoW(SPI_SETCURSORS, 0, NULL, 0)`. Flags are `0`, so there is no `SPIF_UPDATEINIFILE`, and it does not write the registry. It reloads the system cursors from the registry and refreshes the app's cached list.
- A search of every `.cs` file found only these registry writes: the startup `Run` key (`SettingsManager.cs`, `SettingsManager.Startup.cs`). Nothing touches `Control Panel\Cursors` or `Control Panel\Cursors\Schemes`.

**Conclusion: the app cannot delete or modify the scheme list.** Reload only re-reads and re-applies the registry.

## What was found on the machine

- The Scheme dropdown is built **only** from `HKCU\Control Panel\Cursors\Schemes` (HKLM `Schemes` is empty). Mouse Properties does not scan `C:\Windows\Cursors`.
- `Schemes` held a single value, `HuTao Cursor`, pointing to `%LOCALAPPDATA%\Cursors\HuTao\`. That folder was created at 21:33:59 on 6 Nov, so the pack was installed that day.
- Nine other packs were intact on disk in `C:\Windows\Cursors\<pack>\` (folders dated 2–3 Sep 2026) but were unregistered: Abracadabra, Anathema Pink, Apocalypse, coal, Crystal Clear v4.1 - Original, DIM v4 - InfraRed, Glitch, Night Diamond - Ruby Red v3.0, Pulse Glass. No `.inf` files remained in those folders.
- Active values under `HKCU\Control Panel\Cursors` were all empty (Windows default), with `Scheme Source = 0`.
- Nothing in the app logged or recorded the removal. Registry auditing is off, so the time and process that removed the entries cannot be recovered.

## Owner-confirmed history

The owner states that all nine packs had been installed and registered (visible in the Scheme dropdown), and that they had also saved **custom schemes of their own**. All of these were removed from the registry; only `HuTao Cursor` was left. So this was a loss of previously working entries, not entries that were never registered.

## Most likely explanation (unproven)

The packs in `C:\Windows\Cursors` were registered by `.inf` installs (that is where those installers copy files), and the `Schemes` values were later removed from the user hive. The app is ruled out by code. The leading suspect is the **HuTao install itself**: it is the one scheme that survived, it was written at 21:33 on 6 Nov, and the loss was noticed shortly afterwards. If that installer or script clears or recreates the `Schemes` key (for example an `.inf` `DelReg`, or a script that rewrites the key), it would remove everything else and keep only its own entry. This is a hypothesis; the installer is no longer on disk. Other possibilities that fit the evidence: another cursor-changer, the Delete button in Mouse Properties, or a Windows/theme reset.

## Not recovered

The owner's **custom schemes cannot be restored**. Their definitions existed only as registry values, nothing exported them, and their source files could not be located: the only other cursor files on the machine are HuTao and Windows' own accessibility pointer-size set in `%LOCALAPPDATA%\Microsoft\Windows\Cursors` (`*_eoa.cur`). If the custom schemes were mixes of files from the nine packs, they can be recreated in Mouse Properties and saved again with **Save As…**.

## Fix applied

Registered all nine packs as user schemes by writing one string value per pack under `HKCU\Control Panel\Cursors\Schemes`, using the 17-role order Arrow, Help, AppStarting, Wait, Crosshair, IBeam, NWPen, No, SizeNS, SizeWE, SizeNWSE, SizeNESW, SizeAll, UpArrow, Hand, Pin, Person. Roles a pack does not ship are left blank. Guesses: Glitch Alternate Select = `arrow2.cur`; Pulse Glass Diagonal-Left = NWSE and Diagonal-Right = NESW (may be swapped); DIM v4 uses its main animated set. A backup of the previous `Schemes` key was exported before the change. After registration, the owner applied Glitch in Mouse Properties and it saved correctly (`Scheme Source = 1`, `(default) = Glitch`), confirming the entries work.

## Original recommendations (investigation-time status)

1. **Re-read the registry on change.** Watch `HKCU\Control Panel\Cursors` (for example `RegNotifyChangeKeyValue`, or the `WM_SETTINGCHANGE` / `SPI_SETCURSORS` notification) and refresh `sources` automatically. That removes the need to press Reload and fixes the root cause of the original bug.
2. **Pause or restore while Mouse Properties is open** (or when a registry change is detected), so a manual scheme change is not overwritten by the cached one.
3. **Back up `Schemes`.** Custom schemes exist only in the registry, so export `HKCU\Control Panel\Cursors\Schemes` (for example to `%AppData%\CursorEqualizer\schemes-backup.reg`) before installing any new cursor pack. A wiped list can then be restored with one double-click.
4. If the removal repeats, enable registry auditing (or run Process Monitor filtered on `Control Panel\Cursors\Schemes`) to identify the writer.

Relevant source: `CursorManager.cs` and `MainForm.cs` in this repo. No app code was changed for this investigation.

## Implementation follow-up (6 November 2026)

At the owner's request, automatic active-path refresh is now implemented in CursorManager using read-only registry snapshots every 250 ms and before a monitor replacement. Changed paths block cached replacements until stable for 250 ms, then the full registry scheme is restored and the current monitor is resized using the new sources. This also handles changes while paused and switches to schemes without usable custom files. It does not detect whether Mouse Properties is open or implement saved-scheme backups.

Build and 61 focused checks pass; console and GUI smoke tests exit successfully with restoration on the current Glitch scheme. The owner subsequently confirmed custom/default switching across monitors, pause/change/resume and Exit restoration. Windows defaults without custom files do not resize, as expected; this limitation is now stated in Settings, Support & about and README. The investigation above describes the earlier implementation and historical findings; the deletion trigger remains unproven.

#ifndef AppVersion
  #error AppVersion must come from tools/Build-Installer.ps1
#endif
#ifndef PayloadDir
  #error PayloadDir must point to the self-contained published app
#endif

[Setup]
AppId=MelvynPals.CursorEqualizer
AppName=CursorEqualizer
AppVersion={#AppVersion}
AppPublisher=MelvynPals
AppPublisherURL=https://github.com/MelvynPals
AppSupportURL=https://github.com/MelvynPals/win-cursor-equalizer/issues
DefaultDirName={localappdata}\Programs\CursorEqualizer
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputBaseFilename=CursorEqualizer-{#AppVersion}-Setup
SetupIconFile=..\Assets\AppIcon.ico
UninstallDisplayIcon={app}\CursorEqualizer.exe
UninstallDisplayName=CursorEqualizer
AppMutex=Local\CursorEqualizer
CloseApplications=no
RestartApplications=no
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UsePreviousAppDir=yes

[Tasks]
Name: startup; Description: "Start CursorEqualizer when I sign in (managed in Windows Startup apps)"; Check: IsFreshInstall

[Files]
Source: "{#PayloadDir}\CursorEqualizer.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\CursorEqualizer"; Filename: "{app}\CursorEqualizer.exe"; WorkingDir: "{app}"; AppUserModelID: "MelvynPals.CursorEqualizer"

[Run]
Filename: "{app}\CursorEqualizer.exe"; Description: "Open CursorEqualizer"; Flags: postinstall nowait skipifsilent

[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\MelvynPals.CursorEqualizer_is1';
var
  ExistingInstall: Boolean;
  StartupConfigurationFailed: Boolean;

function InitializeSetup: Boolean;
var
  InstalledVersion: String;
  PreviousVersion, IncomingVersion: Int64;
begin
  ExistingInstall := RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', InstalledVersion);
  Result := True;
  if ExistingInstall and StrToVersion(InstalledVersion, PreviousVersion) and
     StrToVersion('{#AppVersion}', IncomingVersion) and
     (ComparePackedVersion(PreviousVersion, IncomingVersion) > 0) then
  begin
    SuppressibleMsgBox('A newer version of CursorEqualizer is installed. Uninstall it before installing an older version. Your calibration settings will be kept.', mbError, MB_OK, IDOK);
    Result := False;
  end;
end;

function IsFreshInstall: Boolean;
begin
  Result := not ExistingInstall;
end;

function ConfigureStartup(const Action: String): Boolean;
var
  ExitCode: Integer;
begin
  Result := Exec(ExpandConstant('{app}\CursorEqualizer.exe'), '--installer-startup ' + Action,
    ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ExitCode);
  if Result then Result := ExitCode = 0;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Action: String;
begin
  if CurStep = ssPostInstall then
  begin
    if ExistingInstall then Action := 'preserve'
    else if WizardIsTaskSelected('startup') then Action := 'standard'
    else Action := 'disabled';
    StartupConfigurationFailed := not ConfigureStartup(Action);
    if StartupConfigurationFailed then
      SuppressibleMsgBox('CursorEqualizer was installed, but Windows startup could not be configured. A previous elevated startup task may require administrator approval. Open the app and choose a startup mode before relying on automatic startup.', mbError, MB_OK, IDOK);
  end;
end;

function GetCustomSetupExitCode: Integer;
begin
  if StartupConfigurationFailed then Result := 10 else Result := 0;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    if not ConfigureStartup('disabled') then
      RaiseException('Windows startup cleanup failed or administrator approval was cancelled. The application has not been removed. Retry after disabling its startup mode in CursorEqualizer.');
end;

// Calibration JSON is deliberately not owned or deleted by Setup/Uninstall.
// AppMutex requires normal tray exit before replacing/removing any app files.

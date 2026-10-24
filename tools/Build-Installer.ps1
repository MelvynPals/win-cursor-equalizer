param([string]$IsccPath)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $IsccPath) {
    $candidates = @(
        (Join-Path $projectRoot '.local\installer-tools\InnoSetup\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe')
    )
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $IsccPath) { $IsccPath = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source }
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) {
    throw 'Install Inno Setup 6.7+ or 7 and pass -IsccPath with the full path to ISCC.exe.'
}
[xml]$project = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'CursorEqualizer.csproj')
$appVersion = [string]$project.Project.PropertyGroup.Version
if ($appVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Installer releases require a numeric major.minor.patch project Version.' }
$payload = Join-Path $projectRoot 'artifacts\installer\app'
$output = Join-Path $projectRoot 'artifacts\installer'
& dotnet publish (Join-Path $projectRoot 'CursorEqualizer.csproj') -c Release -p:PublishProfile=InstallerWinX64
if ($LASTEXITCODE -ne 0) { throw 'Installer payload publish failed.' }
$binary = Join-Path $payload 'CursorEqualizer.exe'
if ((Get-Item -LiteralPath $binary).VersionInfo.ProductVersion -ne $appVersion) { throw 'Payload version does not match the project.' }
& $IsccPath "/DAppVersion=$appVersion" "/DPayloadDir=$payload" "/O$output" (Join-Path $projectRoot 'installer\CursorEqualizer.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Get-Item -LiteralPath (Join-Path $output "CursorEqualizer-$appVersion-Setup.exe") | Select-Object FullName,Length

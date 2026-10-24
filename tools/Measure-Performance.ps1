param(
    [Parameter(Mandatory = $true)][int]$TargetProcessId,
    [Parameter(Mandatory = $true)][string]$Scenario,
    [int]$DurationSeconds = 60,
    [string]$OutputDirectory = 'references/performance'
)

$ErrorActionPreference = 'Stop'
if ($DurationSeconds -lt 1) { throw 'Duration must be positive.' }
if ($Scenario -notmatch '^[a-z0-9-]+$') { throw 'Use lowercase letters, numbers and hyphens for the scenario.' }
$target = Get-Process -Id $TargetProcessId
$originalStart = $target.StartTime.ToUniversalTime()
$executable = $target.Path
if (-not $executable) { throw 'Cannot read the executable path.' }
$cpuInfo = @(Get-CimInstance Win32_Processor)
$logicalProcessors = [int](($cpuInfo | Measure-Object NumberOfLogicalProcessors -Sum).Sum)
$computer = Get-CimInstance Win32_ComputerSystem
$os = Get-CimInstance Win32_OperatingSystem
$version = (Get-Item -LiteralPath $executable).VersionInfo.ProductVersion
$binaryHash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash
$sourceCommit = git rev-parse HEAD
$sourceStatus = @(git status --porcelain 2>$null)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$started = [DateTime]::UtcNow
$prefix = Join-Path $OutputDirectory ($started.ToString('yyyyMMddTHHmmssZ') + '-' + $Scenario)
$samples = [System.Collections.Generic.List[object]]::new()
$clock = [Diagnostics.Stopwatch]::StartNew()
$target.Refresh()
$previousCpu = $target.TotalProcessorTime.TotalSeconds
$initialCpu = $previousCpu
$previousTime = $clock.Elapsed.TotalSeconds
$initialTime = $previousTime
for ($index = 0; $index -lt $DurationSeconds; $index++) {
    Start-Sleep -Milliseconds 1000
    $target.Refresh()
    if ($target.HasExited -or $target.StartTime.ToUniversalTime() -ne $originalStart) { throw 'Target process changed or exited; discard this run.' }
    $now = $clock.Elapsed.TotalSeconds
    $cpu = $target.TotalProcessorTime.TotalSeconds
    $corePercent = 100 * ($cpu - $previousCpu) / ($now - $previousTime)
    $samples.Add([pscustomobject]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        ElapsedSeconds = $now
        IntervalSeconds = $now - $previousTime
        CpuSecondsDelta = $cpu - $previousCpu
        CpuPercentMachine = $corePercent / $logicalProcessors
        CpuPercentOneCore = $corePercent
        WorkingSetBytes = $target.WorkingSet64
        PrivateBytes = $target.PrivateMemorySize64
        Handles = $target.HandleCount
        Threads = $target.Threads.Count
    })
    $previousCpu = $cpu
    $previousTime = $now
}
$samples | Export-Csv -LiteralPath ($prefix + '.csv') -NoTypeInformation -Encoding UTF8
$elapsed = $previousTime - $initialTime
$summary = [ordered]@{
    Scenario = $Scenario
    StartedUtc = $started.ToString('o')
    DurationSeconds = $elapsed
    SampleCount = $samples.Count
    ProcessId = $TargetProcessId
    ProcessStartedUtc = $originalStart.ToString('o')
    ProductVersion = $version
    ExecutableSha256 = $binaryHash
    SourceCommit = $sourceCommit
    SourceHasUncommittedChanges = ($sourceStatus.Count -gt 0)
    CpuModel = @($cpuInfo | ForEach-Object { $_.Name.Trim() })
    LogicalProcessors = $logicalProcessors
    InstalledRamGiB = $computer.TotalPhysicalMemory / 1GB
    OperatingSystem = $os.Caption
    OperatingSystemVersion = $os.Version
    OperatingSystemBuild = $os.BuildNumber
    CpuSeconds = $previousCpu - $initialCpu
    AverageCpuPercentMachine = 100 * ($previousCpu - $initialCpu) / $elapsed / $logicalProcessors
    PeakSampleCpuPercentMachine = ($samples | Measure-Object CpuPercentMachine -Maximum).Maximum
    AverageCpuPercentOneCore = 100 * ($previousCpu - $initialCpu) / $elapsed
    AverageWorkingSetMiB = ($samples | Measure-Object WorkingSetBytes -Average).Average / 1MB
    MinimumWorkingSetMiB = ($samples | Measure-Object WorkingSetBytes -Minimum).Minimum / 1MB
    MaximumWorkingSetMiB = ($samples | Measure-Object WorkingSetBytes -Maximum).Maximum / 1MB
    AveragePrivateMiB = ($samples | Measure-Object PrivateBytes -Average).Average / 1MB
    MinimumPrivateMiB = ($samples | Measure-Object PrivateBytes -Minimum).Minimum / 1MB
    MaximumPrivateMiB = ($samples | Measure-Object PrivateBytes -Maximum).Maximum / 1MB
    MinimumHandles = ($samples | Measure-Object Handles -Minimum).Minimum
    MaximumHandles = ($samples | Measure-Object Handles -Maximum).Maximum
    MinimumThreads = ($samples | Measure-Object Threads -Minimum).Minimum
    MaximumThreads = ($samples | Measure-Object Threads -Maximum).Maximum
    Notes = 'External 1-second process samples; CPU normalized across all logical processors. Peaks are interval averages, not instantaneous maxima. Working set includes shared resident pages; private bytes are committed private memory, not resident RAM. No forced GC or working-set trimming. Other apps remain running. Scenario is operator-described, not detected by this script.'
}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath ($prefix + '.json') -Encoding UTF8
[pscustomobject]$summary

# CursorEqualizer performance record

Measured on 2026-10-25 at 04:14-04:16 Asia/Manila (2026-10-24 20:14-20:16 UTC). These are observations from one PC and one warmed-up process, not minimum requirements or guarantees.

## Build and test machine

- Application: 1.0.0, framework-dependent Release win-x64 single-file executable, 456,324 bytes.
- Binary: bin/Release/net8.0-windows/win-x64/publish/CursorEqualizer.exe.
- SHA-256: `871543C8FB55D00F759402F2045969B509B412D645121285B5E2A83A8FB2B0D8`.
- Source base: `57adce0cd0a4779d8e0e29d31fdc77a3401378a9`, with uncommitted versioning and Support & about version-label changes. The binary hash identifies the tested artifact; this is not a clean release-tag benchmark.
- CPU: Intel Core i5-14400F, 16 logical processors.
- OS: Windows 11 Home, version 10.0.26200, build 26200.
- OS-reported installed physical RAM: 31.76 GiB.
- Two connected 2560 x 1440 displays; the last inspected UI showed 125% and 150% scaling. Display state was not instrumented during sampling.
- Process started at 04:10:55 local time; measurement excluded cold startup. Other desktop apps and the external PowerShell sampler remained running.

## Results

| Metric | Initial observation | Background tray sample |
| --- | ---: | ---: |
| Actual elapsed time | 60.83 s | 61.06 s |
| Samples | 60 | 60 |
| Average CPU, whole machine | 0.0096% | 0.0160% |
| Highest sampled CPU interval, whole machine | 0.1930% | 0.2889% |
| Average CPU, one-core equivalent | 0.1541% | 0.2559% |
| Total process CPU time used | 0.09375 s | 0.15625 s |
| Average resident working set | 82.55 MiB | 82.63 MiB |
| Working-set range | 82.55-82.56 MiB | 82.53-82.74 MiB |
| Average private committed memory | 22.22 MiB | 22.21 MiB |
| Private-memory range | 22.22-22.25 MiB | 22.18-22.25 MiB |
| Handles, sampled range | 400-402 | 396-396 |
| Threads, sampled range | 9-10 | 8-10 |

The initial run was originally named settings-open-idle. A mid-run check found no visible Settings window, so its label was corrected to initial-observation; it provides no evidence for UI-open performance. For the second run, UI Automation found no visible app window before and after sampling. The equalizer was left running, with no scripted pointer movement, calibration changes, pause, restart, forced garbage collection or memory trimming. User pointer activity and background system load were not controlled or logged. The scenario name tray-idle describes the intended workload, not a verified absence of monitor crossings.

## Measurement method and raw evidence

[Measure-Performance.ps1](../tools/Measure-Performance.ps1) reads cumulative process CPU time, working set, private bytes, handles and thread count approximately once per second. It records the actual interval using a monotonic Stopwatch. Average CPU is total process CPU seconds divided by actual elapsed seconds and by 16 logical processors, multiplied by 100. The one-core equivalent omits division by 16. This describes process CPU time, not processor-frequency-adjusted utility.

Working set is resident memory including shared pages. Private bytes are committed private memory and may include nonresident pages. Do not present 22 MiB as total RAM usage. MiB means 1,048,576 bytes. Sampled CPU peaks are approximately one-second averages, so faster spikes may be missed; small CPU-time changes are also limited by the process counter's resolution. Memory averages are arithmetic means of the samples.

- Initial observation: [raw CSV](performance/20261024T201406Z-initial-observation.csv), [metadata and summary JSON](performance/20261024T201406Z-initial-observation.json).
- Background tray sample: [raw CSV](performance/20261024T201524Z-tray-idle.csv), [metadata and summary JSON](performance/20261024T201524Z-tray-idle.json).

Validation: the background CSV contains 60 samples; summing its CPU-time and wall-time deltas independently reproduces the JSON CPU average. Both runs observed the same process and executable hash.

To repeat, identify the current published process PID, establish the intended UI/workload state, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Measure-Performance.ps1 -TargetProcessId 34624 -Scenario tray-idle -DurationSeconds 60
```

The PID is an example from this run. Hardware inspection may require execution outside the restricted coding sandbox. The script itself does not hide or open the app, move the pointer, or detect the scenario.

## Wording supported by this measurement

Suggested claim: 'In a 61-second background tray sample on a Windows 11 PC with an Intel Core i5-14400F, CursorEqualizer 1.0.0 averaged about 0.02% total CPU and 83 MiB of resident RAM.'

Keep the hardware and duration available alongside the claim. These samples do not establish zero CPU use, usage on all PCs, long-term memory stability, battery impact, GPU/disk/network usage, cold-start cost, cursor-switch latency, or performance during repeated monitor transitions. A longer repeated-monitor-crossing workload and multiple hardware configurations are needed before broader claims.

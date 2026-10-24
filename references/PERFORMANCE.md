# CursorEqualizer performance record

Background measurements: 2026-10-25 at 04:14-04:16 Asia/Manila (2026-10-24 20:14-20:16 UTC). User-driven crossing sample: 04:21 Asia/Manila (20:21 UTC). These are observations from one PC and one warmed-up process, not minimum requirements or guarantees.

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

## User-driven rapid monitor crossings

The owner requested a roughly 10-second stress sample and was asked to move the mouse rapidly between physical monitors from the start cue until the stop cue. Sampling ran from 04:21:45.778 to approximately 04:21:55.933 Asia/Manila on 2026-10-25 (20:21 UTC on 2026-10-24). Setup occurred after the start cue, so the requested motion interval was longer than the actual capture. Mouse movements and crossing count were not instrumented; this is a user-driven workload, not a controlled crossing-rate benchmark.

The same running published process (PID 34624) and executable SHA-256 were measured. Source HEAD at capture was `670205cd8dd14b744fba69120c4e989bc9a67e4d`, with uncommitted versioning changes; the binary was not rebuilt between samples. Hardware, CPU normalization and one-second sampling match the background baseline.

| Metric | Rapid monitor crossings |
| --- | ---: |
| Actual elapsed time | 10.155 s |
| Samples | 10 |
| Average CPU, whole machine | 0.8366% |
| Highest sampled CPU interval, whole machine | 1.0582% |
| Average CPU, one-core equivalent | 13.3863% |
| Total process CPU time used | 1.359375 s |
| Average resident working set | 84.30 MiB |
| Working-set range | 84.17-84.56 MiB |
| Average private committed memory | 23.51 MiB |
| Private-memory range | 23.33-23.91 MiB |
| Handles, sampled range | 396-399 |
| Threads, sampled range | 11-12 |

The process remained alive throughout the capture. These readings do not establish cursor-switch latency, smoothness, exact crossing throughput, or long-term memory stability. Working set averaged 1.67 MiB above the earlier background sample, but the samples occurred at different times; this is not an isolated allocation measurement or evidence of a leak.

Evidence: [raw CSV](performance/20261024T202145Z-manual-rapid-monitor-crossings.csv), [metadata and summary JSON](performance/20261024T202145Z-manual-rapid-monitor-crossings.json). The CSV contains ten samples; its CPU-time and wall-time deltas independently reproduce the reported average.

Suggested claim: 'In a 10-second user-driven rapid monitor-crossing sample on our Intel Core i5-14400F Windows 11 test PC, CursorEqualizer 1.0.0 averaged 0.84% total CPU and 84 MiB resident RAM; the highest one-second CPU reading was 1.06%.'

## Wording supported by the background measurement

Suggested claim: 'In a 61-second background tray sample on a Windows 11 PC with an Intel Core i5-14400F, CursorEqualizer 1.0.0 averaged about 0.02% total CPU and 83 MiB of resident RAM.'

Keep the hardware and duration available alongside the claim. The background samples do not establish zero CPU use, usage on all PCs, long-term memory stability, battery impact, GPU/disk/network usage, cold-start cost, cursor-switch latency, or performance during repeated monitor transitions. The separate user-driven sample above provides a short observation of the requested crossing workload. Longer controlled runs and multiple hardware configurations are needed before broader claims.

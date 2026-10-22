using CursorEqualizer;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
var primary = new MonitorSettings { DiagonalInches = 24.5 };
var secondary = new MonitorSettings { DiagonalInches = 16 };
Check(CursorManager.CalculateTargetPixels(32, 120, 192, primary, secondary) == 16, "Requested formula/lower clamp");
Check(CursorManager.CalculateTargetPixels(32, 120, 120, primary, primary) == 32, "Primary reference size");
secondary.DiagonalInches = 24.5;
Check(CursorManager.CalculateTargetPixels(32, 120, 192, primary, secondary) == 20, "DPI ratio at 200%");
Check(CursorManager.CalculateTargetPixels(32, 120, 144, primary, secondary) == 27, "DPI ratio after change to 150%");
secondary.DiagonalInches = 200;
Check(CursorManager.CalculateTargetPixels(32, 192, 96, primary, secondary) == 128, "Upper clamp");
secondary.DiagonalInches = null;
Check(CursorManager.CalculateTargetPixels(32, 120, 192, primary, secondary) == 32, "Missing diagonal fallback");
secondary.Mode = SizingMode.Manual;
secondary.ManualPixels = 23;
Check(CursorManager.CalculateTargetPixels(32, 120, 192, primary, secondary) == 23, "Manual mode bypasses formula");
Check(CursorManager.CalculateTargetPixels(32, 120, 144, primary, secondary) == 23, "Manual pixels unchanged after DPI change");
secondary.Mode = SizingMode.Calibrated;
secondary.CalibrationPixels = 23;
secondary.CalibrationDpi = 192;
Check(CursorManager.CalculateTargetPixels(32, 120, 192, primary, secondary) == 23, "Calibrated reference at 200%");
Check(CursorManager.CalculateTargetPixels(32, 120, 144, primary, secondary) == 31, "Compensated 200% to 150%");
Check(CursorManager.CalculateTargetPixels(32, 120, 192, primary, secondary) == 23, "Return to reference without cumulative rounding");
Check(CursorManager.CalculateTargetPixels(32, 120, 960, primary, secondary) == 16, "Calibrated lower clamp");
Check(CursorManager.CalculateTargetPixels(32, 120, 24, primary, secondary) == 128, "Calibrated upper clamp");

string directory = Path.Combine(Directory.GetCurrentDirectory(), ".local", "phase2-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string file = Path.Combine(directory, "settings.json");
var store = new SettingsManager(file);
store.Settings.BasePixels = 40;
var monitor = store.ForMonitor("MONITOR-ID");
monitor.Mode = SizingMode.Manual;
monitor.ManualPixels = 23;
monitor.DiagonalInches = 16;
store.Save();
var reloaded = new SettingsManager(file);
Check(reloaded.Settings.BasePixels == 40 && reloaded.Settings.Monitors.Count == 1, "Root settings roundtrip");
Check(reloaded.ForMonitor("monitor-id").Mode == SizingMode.Manual &&
    reloaded.ForMonitor("monitor-id").ManualPixels == 23 &&
    reloaded.ForMonitor("monitor-id").DiagonalInches == 16 && reloaded.Settings.Monitors.Count == 1,
    "Case-insensitive monitor identity and calibration roundtrip");
Check(!Directory.EnumerateFiles(directory, "*.tmp").Any(), "No temporary files after atomic save");
monitor.Mode = SizingMode.Calibrated;
monitor.CalibrationPixels = 23;
monitor.CalibrationDpi = 192;
store.Save();
var savedCalibration = new SettingsManager(file).ForMonitor("monitor-id");
Check(savedCalibration.Mode == SizingMode.Calibrated && savedCalibration.CalibrationPixels == 23 &&
    savedCalibration.CalibrationDpi == 192, "Calibrated reference roundtrip");
string original = File.ReadAllText(file);
foreach (string invalid in new[]
{
    "{ invalid json", "null", "{\"Version\":2}", "{\"BasePixels\":0}",
    "{\"Monitors\":null}", "{\"Monitors\":{\"x\":null}}",
    "{\"Monitors\":{\"x\":{\"Mode\":\"Unknown\"}}}",
    "{\"Monitors\":{\"x\":{\"ManualPixels\":129}}}",
    "{\"Monitors\":{\"x\":{\"DiagonalInches\":0}}}",
    "{\"Monitors\":{\"x\":{\"Mode\":\"Calibrated\"}}}",
    "{\"Monitors\":{\"x\":{\"CalibrationDpi\":0}}}",
    "{\"Monitors\":{\"x\":{\"CalibrationPixels\":129}}}"
})
{
    File.WriteAllText(file, invalid);
    bool rejected = false;
    try { _ = new SettingsManager(file); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected && File.ReadAllText(file) == invalid, "Invalid settings rejected without overwriting: " + invalid);
}
File.WriteAllText(file, original);
Console.WriteLine($"PASS: {checks} sizing, persistence, identity, validation, and preservation checks.");

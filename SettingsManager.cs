using System.Text.Json;
using System.Text.Json.Serialization;

namespace CursorEqualizer;

internal enum SizingMode { Auto, Manual, Calibrated }

internal sealed class MonitorSettings
{
    public SizingMode Mode { get; set; } = SizingMode.Auto;
    public double? DiagonalInches { get; set; }
    public int ManualPixels { get; set; } = 32;
    public int CalibrationPixels { get; set; } = 32;
    public uint? CalibrationDpi { get; set; }
}

internal sealed class AppSettings
{
    public int Version { get; set; } = 1;
    public int BasePixels { get; set; } = 32;
    public Dictionary<string, MonitorSettings> Monitors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter<SizingMode>(allowIntegerValues: false) }
    };

    internal string FilePath { get; }
    internal AppSettings Settings { get; private set; } = new();

    internal SettingsManager(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), "CursorEqualizer", "settings.json");
        if (!File.Exists(FilePath)) return;
        try
        {
            Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions)
                ?? throw new InvalidDataException("Settings document is null.");
            Validate();
            Settings.Monitors = new(Settings.Monitors, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException)
        {
            // Never silently overwrite a malformed calibration file.
            throw new InvalidDataException($"Invalid settings in {FilePath}. Correct or rename the file before restarting. {ex.Message}", ex);
        }
    }

    internal MonitorSettings ForMonitor(string id)
    {
        if (!Settings.Monitors.TryGetValue(id, out var monitor))
            Settings.Monitors.Add(id, monitor = new());
        return monitor;
    }

    internal void Save()
    {
        Validate();
        string directory = Path.GetDirectoryName(Path.GetFullPath(FilePath))!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $"settings.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(Settings, JsonOptions) + Environment.NewLine);
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private void Validate()
    {
        if (Settings.Version != 1 || Settings.BasePixels is < 16 or > 128 || Settings.Monitors is null)
            throw new InvalidDataException("Expected version 1, base pixels 16-128, and a monitors object.");
        foreach (var (id, monitor) in Settings.Monitors)
        {
            if (string.IsNullOrWhiteSpace(id) || monitor is null ||
                !Enum.IsDefined(monitor.Mode) || monitor.ManualPixels is < 16 or > 128 ||
                monitor.CalibrationPixels is < 16 or > 128 || monitor.CalibrationDpi == 0 ||
                (monitor.Mode == SizingMode.Calibrated && monitor.CalibrationDpi is null) ||
                (monitor.DiagonalInches is double diagonal && (!double.IsFinite(diagonal) || diagonal <= 0 || diagonal > 200)))
                throw new InvalidDataException($"Invalid calibration for monitor '{id}'. Diagonals must be >0 to 200 inches; pixels 16-128; Calibrated mode requires a positive reference DPI.");
        }
    }
}

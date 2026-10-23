using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

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

internal sealed partial class SettingsManager
{
    internal const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string StartupValueName = "CursorEqualizer";
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

    // The Run value is the source of truth, independent of monitor calibration JSON.
    internal string? GetStartupCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath);
        return key?.GetValue(StartupValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    }

    internal void SetStartWithWindows(bool enabled)
    {
        if (!enabled)
        {
            using var existing = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true);
            existing?.DeleteValue(StartupValueName, throwOnMissingValue: false);
            return;
        }
        string executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot locate the application executable.");
        string command = BuildStartupCommand(executable);
        if (!File.Exists(executable)) throw new FileNotFoundException("The application executable is missing.", executable);
        using var key = Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true);
        key.SetValue(StartupValueName, command, RegistryValueKind.String);
    }

    internal static string BuildStartupCommand(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || executable.IndexOfAny(['"', '\r', '\n', '\0']) >= 0 ||
            !Path.IsPathFullyQualified(executable) ||
            !Path.GetExtension(executable).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(executable).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Start CursorEqualizer.exe directly or use dotnet run before enabling Windows startup.", nameof(executable));
        string command = $"\"{Path.GetFullPath(executable)}\" --minimized";
        // Windows documents a 260-character limit for Run command lines.
        if (command.Length > 260)
            throw new ArgumentException("Move CursorEqualizer to a shorter path before enabling Windows startup.", nameof(executable));
        return command;
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

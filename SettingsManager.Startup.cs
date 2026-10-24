using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.Win32;

namespace CursorEqualizer;

internal enum StartupMode { Disabled, Standard, Elevated }

internal sealed partial class SettingsManager
{
    private const int TaskCreateOrUpdate = 6;
    private const int TaskLogonInteractiveToken = 3;
    internal static string CurrentUserSid
    {
        get { using var identity = WindowsIdentity.GetCurrent(); return identity.User!.Value; }
    }
    internal static string StartupTaskName(string sid) => "CursorEqualizer-" + new SecurityIdentifier(sid).Value;

    // Installer helpers never start the cursor engine or read/write calibration JSON.
    internal static int ConfigureInstallerStartup(string[] args)
    {
        if (args.Length != 2 || args[0] != "--installer-startup" ||
            args[1] is not ("standard" or "disabled" or "preserve"))
            throw new ArgumentException("Expected --installer-startup standard, disabled or preserve.");
        var settings = new SettingsManager(loadSettings: false);
        var requested = GetInstallerStartupMode(args[1], settings.GetStartupMode());
        return settings.SetStartupModeAsync(requested).GetAwaiter().GetResult() ? 0 : 2;
    }

    internal static StartupMode GetInstallerStartupMode(string action, StartupMode existing) => action switch
    {
        "standard" => StartupMode.Standard,
        "disabled" => StartupMode.Disabled,
        // Installed releases use Windows-managed startup, including migration from a portable elevated task.
        "preserve" when Enum.IsDefined(existing) => existing == StartupMode.Disabled
            ? StartupMode.Disabled : StartupMode.Standard,
        _ => throw new ArgumentException("Unknown installer startup action or mode.")
    };

    internal StartupMode GetStartupMode()
    {
        string? xml = GetElevatedStartupTaskXml();
        if (xml is not null)
        {
            var task = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
            if ((bool?)task.Root?.Element(ns + "Settings")?.Element(ns + "Enabled") != false)
                return StartupMode.Elevated;
        }
        return GetStartupCommand() is null ? StartupMode.Disabled : StartupMode.Standard;
    }

    internal Task<bool> SetStartupModeAsync(StartupMode mode) => Task.Run(() =>
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        bool requiresAdmin = mode == StartupMode.Elevated || GetElevatedStartupTaskXml() is not null;
        using var identity = WindowsIdentity.GetCurrent();
        if (!requiresAdmin || new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
        {
            ApplyStartupMode(mode);
            return true;
        }
        string executable = Environment.ProcessPath!;
        _ = BuildStartupCommand(executable);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        start.ArgumentList.Add("--configure-startup");
        start.ArgumentList.Add(mode.ToString());
        start.ArgumentList.Add(CurrentUserSid);
        try
        {
            using var helper = Process.Start(start) ?? throw new InvalidOperationException("Could not start the startup configuration helper.");
            helper.WaitForExit();
            if (helper.ExitCode != 0) throw new InvalidOperationException("Windows startup could not be changed. See the configuration error shown by the helper.");
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) { return false; }
    });

    // Runs before the app mutex and cursor engine; this helper only configures startup.
    internal static void ConfigureStartup(string[] args)
    {
        if (args.Length != 3 || args[0] != "--configure-startup" ||
            !Enum.TryParse<StartupMode>(args[1], out var mode) || !Enum.IsDefined(mode) ||
            args[2] != CurrentUserSid)
            throw new ArgumentException("Startup configuration must use the same Windows account as the running app.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new UnauthorizedAccessException("Administrator approval is required to change elevated startup.");
        new SettingsManager(loadSettings: false).ApplyStartupMode(mode);
    }

    private void ApplyStartupMode(StartupMode mode)
    {
        string? previousRun = GetStartupCommand();
        string? previousTask = GetElevatedStartupTaskXml();
        bool runChanged = false, taskChanged = false;
        try
        {
            if (mode == StartupMode.Elevated)
            {
                string executable = Environment.ProcessPath!;
                _ = BuildStartupCommand(executable);
                if (!File.Exists(executable)) throw new FileNotFoundException("The application executable is missing.", executable);
                RegisterElevatedTask(BuildElevatedStartupXml(executable, CurrentUserSid));
                taskChanged = true;
                SetStartWithWindows(false);
                runChanged = true;
            }
            else
            {
                SetStartWithWindows(mode == StartupMode.Standard);
                runChanged = true;
                if (previousTask is not null) { DeleteElevatedTask(); taskChanged = true; }
            }
        }
        catch (Exception changeError)
        {
            try
            {
                if (runChanged)
                {
                    if (previousRun is null) SetStartWithWindows(false);
                    else
                    {
                        using var key = Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true);
                        key.SetValue(StartupValueName, previousRun, RegistryValueKind.String);
                    }
                }
                if (taskChanged)
                {
                    if (previousTask is null) DeleteElevatedTask();
                    else RegisterElevatedTask(previousTask);
                }
            }
            catch (Exception rollbackError)
            {
                throw new AggregateException("Startup configuration and restoration failed. Check the CursorEqualizer Run entry and scheduled task.", changeError, rollbackError);
            }
            throw;
        }
    }

    internal static string BuildElevatedStartupXml(string executable, string sid)
    {
        _ = BuildStartupCommand(executable);
        sid = new SecurityIdentifier(sid).Value;
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        XElement Element(string name, object value) => new(ns + name, value);
        return new XDocument(new XElement(ns + "Task", new XAttribute("version", "1.2"),
            Element("RegistrationInfo", Element("Description", "CursorEqualizer: elevated tray startup for the signed-in user.")),
            Element("Triggers", Element("LogonTrigger", new object[] { Element("Enabled", true), Element("UserId", sid) })),
            Element("Principals", new XElement(ns + "Principal", new XAttribute("id", "User"),
                Element("UserId", sid), Element("LogonType", "InteractiveToken"), Element("RunLevel", "HighestAvailable"))),
            Element("Settings", new object[]
            {
                Element("MultipleInstancesPolicy", "IgnoreNew"), Element("DisallowStartIfOnBatteries", false),
                Element("StopIfGoingOnBatteries", false), Element("AllowHardTerminate", false),
                Element("StartWhenAvailable", true), Element("AllowStartOnDemand", true),
                Element("Enabled", true), Element("ExecutionTimeLimit", "PT0S")
            }),
            new XElement(ns + "Actions", new XAttribute("Context", "User"),
                Element("Exec", new object[] { Element("Command", Path.GetFullPath(executable)),
                    Element("Arguments", "--minimized"), Element("WorkingDirectory", Path.GetDirectoryName(Path.GetFullPath(executable))!) }))
        )).ToString();
    }

    internal string? GetElevatedStartupTaskXml() => WithTaskFolder<string?>(folder =>
    {
        using var objects = new ComObjects();
        try
        {
            dynamic task = objects.Own(folder.GetTask(StartupTaskName(CurrentUserSid)));
            return (string)task.Xml;
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80070002)) { return null; }
    });

    private static void RegisterElevatedTask(string xml) => WithTaskFolder(folder =>
    {
        using var objects = new ComObjects();
        objects.Own(folder.RegisterTask(StartupTaskName(CurrentUserSid), xml, TaskCreateOrUpdate,
            CurrentUserSid, null, TaskLogonInteractiveToken, null));
        return true;
    });

    private static void DeleteElevatedTask() => WithTaskFolder(folder =>
    {
        folder.DeleteTask(StartupTaskName(CurrentUserSid), 0);
        return true;
    });

    private static T WithTaskFolder<T>(Func<dynamic, T> action)
    {
        using var objects = new ComObjects();
        dynamic service = objects.Own(Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", throwOnError: true)!)!);
        service.Connect();
        dynamic folder = objects.Own(service.GetFolder(@"\"));
        return action(folder);
    }

    private sealed class ComObjects : IDisposable
    {
        private readonly List<object> values = new();
        internal dynamic Own(object value) { values.Add(value); return value; }
        public void Dispose()
        {
            for (int i = values.Count - 1; i >= 0; i--) Marshal.FinalReleaseComObject(values[i]);
        }
    }
}

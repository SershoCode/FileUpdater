#pragma warning disable CA1416 // Validate platform compatibility

using System.Diagnostics;
using System.Management;

namespace SershoCode.FileUpdater.Analytics.HardwareMonitoring;

public static class OsVersionManager
{
    public static string GetUserFriendlyName()
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsUserFriendlyName();

        if (OperatingSystem.IsLinux())
            return GetLinuxUserFriendlyName();

        return "Unknown OS";
    }

    private static string GetLinuxUserFriendlyName()
    {
        const string osInfoPath = "/etc/os-release";
        const string cmd = $"cat {osInfoPath}";

        var process = new Process();

        process.StartInfo.FileName = "/bin/sh";
        process.StartInfo.Arguments = $"-c \"{cmd}\"";
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        process.WaitForExit();

        while (process.StandardOutput.ReadLine() is { } fileLine)
        {
            if (!fileLine.StartsWith("PRETTY_NAME"))
                continue;

            return fileLine.Split('\"')[1];
        }

        return "Unknown Linux";
    }

    private static string GetWindowsUserFriendlyName()
    {
        var result = string.Empty;

        var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");

        foreach (var os in searcher.Get())
        {
            result = os["Caption"].ToString();

            break;
        }

        return result ?? "Unknown Windows";
    }
}
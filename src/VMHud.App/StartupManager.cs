using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace VMHud.App;

internal static class StartupManager
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "VMHud";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var val = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(val);
        }
        catch { return false; }
    }

    public static bool Enable()
    {
        try
        {
            var exePath = GetExecutablePath();
            if (string.IsNullOrWhiteSpace(exePath)) return false;
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true) ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            key.SetValue(ValueName, Quote(exePath));
            return true;
        }
        catch { return false; }
    }

    public static bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.DeleteValue(ValueName, false);
            return true;
        }
        catch { return false; }
    }

    public static bool IsDevHost()
    {
        try
        {
            var path = Environment.ProcessPath ?? string.Empty;
            return path.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static string GetExecutablePath()
    {
        try
        {
            // In published app, this will be the app exe.
            // In development (dotnet run), this may be dotnet.exe.
            return Environment.ProcessPath
                   ?? Process.GetCurrentProcess().MainModule?.FileName
                   ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private static string Quote(string path) => path.Contains(' ') ? $"\"{path}\"" : path;
}

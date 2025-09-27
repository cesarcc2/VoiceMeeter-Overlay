using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VMHud.Backend;

internal static class VoicemeeterDllLocator
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    public static bool EnsureSearchPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "VB", "Voicemeeter"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VB", "Voicemeeter"),
        };

        foreach (var dir in candidates)
        {
            try
            {
                var dll = Path.Combine(dir, "VoicemeeterRemote.dll");
                if (File.Exists(dll))
                {
                    SetDllDirectory(dir);
                    var h = LoadLibrary("VoicemeeterRemote.dll");
                    var h64 = LoadLibrary("VoicemeeterRemote64.dll");
                    var ok = (h != IntPtr.Zero) || (h64 != IntPtr.Zero);
                    if (ok)
                    {
                        VMHud.Core.Diagnostics.Log.Info($"Voicemeeter DLLs located in: {dir}");
                        return true;
                    }
                }
            }
            catch { }
        }
        VMHud.Core.Diagnostics.Log.Warn("Voicemeeter DLLs not found in default locations.");
        return false;
    }
}

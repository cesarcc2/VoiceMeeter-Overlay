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
        var is64 = Environment.Is64BitProcess;
        var pf  = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var preferred = is64
            ? new[] { Path.Combine(pf, "VB", "Voicemeeter"), Path.Combine(pfx, "VB", "Voicemeeter") }
            : new[] { Path.Combine(pfx, "VB", "Voicemeeter"), Path.Combine(pf, "VB", "Voicemeeter") };

        var requiredDll = is64 ? "VoicemeeterRemote64.dll" : "VoicemeeterRemote.dll";

        foreach (var dir in preferred)
        {
            try
            {
                var reqPath = Path.Combine(dir, requiredDll);
                if (File.Exists(reqPath))
                {
                    SetDllDirectory(dir);
                    var hReq = LoadLibrary(requiredDll);
                    var ok = hReq != IntPtr.Zero;
                    if (ok)
                    {
                        VMHud.Core.Diagnostics.Log.Info($"Voicemeeter DLLs located in: {dir}");
                        return true;
                    }
                }
            }
            catch { }
        }

        // Fallback: accept directory if either DLL loads (original behavior)
        var fallbackCandidates = new[]
        {
            Path.Combine(pfx, "VB", "Voicemeeter"),
            Path.Combine(pf,  "VB", "Voicemeeter"),
        };
        foreach (var dir in fallbackCandidates)
        {
            try
            {
                var dll = Path.Combine(dir, "VoicemeeterRemote.dll");
                if (File.Exists(dll))
                {
                    SetDllDirectory(dir);
                    var h = LoadLibrary("VoicemeeterRemote.dll");
                    var h64 = LoadLibrary("VoicemeeterRemote64.dll");
                    if (h != IntPtr.Zero || h64 != IntPtr.Zero)
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

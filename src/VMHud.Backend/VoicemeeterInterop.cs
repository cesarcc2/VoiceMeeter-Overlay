using System;
using System.Runtime.InteropServices;

namespace VMHud.Backend;

internal static class VoicemeeterInterop
{
    private static bool Is64 => Environment.Is64BitProcess;

    // Wrapper methods choose the appropriate native DLL at runtime
    public static int VBVMR_Login() => Is64 ? Native64.VBVMR_Login() : Native32.VBVMR_Login();
    public static int VBVMR_Logout() => Is64 ? Native64.VBVMR_Logout() : Native32.VBVMR_Logout();
    public static int VBVMR_RunVoicemeeter(int v) => Is64 ? Native64.VBVMR_RunVoicemeeter(v) : Native32.VBVMR_RunVoicemeeter(v);
    public static int VBVMR_GetVoicemeeterType(out int pType) => Is64 ? Native64.VBVMR_GetVoicemeeterType(out pType) : Native32.VBVMR_GetVoicemeeterType(out pType);
    public static int VBVMR_IsParametersDirty() => Is64 ? Native64.VBVMR_IsParametersDirty() : Native32.VBVMR_IsParametersDirty();
    public static int VBVMR_GetParameterFloat(string name, out float val) => Is64 ? Native64.VBVMR_GetParameterFloat(name, out val) : Native32.VBVMR_GetParameterFloat(name, out val);
    public static int VBVMR_GetParameterStringA(string name, IntPtr buf, int size) => Is64 ? Native64.VBVMR_GetParameterStringA(name, buf, size) : Native32.VBVMR_GetParameterStringA(name, buf, size);
    public static int VBVMR_SetParameterFloat(string name, float val) => Is64 ? Native64.VBVMR_SetParameterFloat(name, val) : Native32.VBVMR_SetParameterFloat(name, val);

    public static bool TryGetStringA(string param, out string value)
    {
        const int size = 512;
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            var rc = VBVMR_GetParameterStringA(param, ptr, size);
            if (rc == 0)
            {
                value = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
                return true;
            }
        }
        catch { }
        finally { Marshal.FreeHGlobal(ptr); }
        value = string.Empty;
        return false;
    }

    private static class Native32
    {
        private const string Dll = "VoicemeeterRemote.dll";
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_Login();
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_Logout();
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_RunVoicemeeter(int v);
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_GetVoicemeeterType(out int pType);
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_IsParametersDirty();
        [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_GetParameterFloat(string szParamName, out float val);
        [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_GetParameterStringA(string szParamName, IntPtr szString, int lSize);
        [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_SetParameterFloat(string szParamName, float val);
    }

    private static class Native64
    {
        private const string Dll = "VoicemeeterRemote64.dll";
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_Login();
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_Logout();
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_RunVoicemeeter(int v);
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_GetVoicemeeterType(out int pType);
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_IsParametersDirty();
        [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_GetParameterFloat(string szParamName, out float val);
        [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_GetParameterStringA(string szParamName, IntPtr szString, int lSize);
        [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)] public static extern int VBVMR_SetParameterFloat(string szParamName, float val);
    }
}

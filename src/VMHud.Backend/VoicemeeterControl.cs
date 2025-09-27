using System;

namespace VMHud.Backend;

public static class VoicemeeterControl
{
    public static void Open()
    {
        // 1 asks Voicemeeter to run; see Remote API docs
        VoicemeeterInterop.VBVMR_RunVoicemeeter(1);
    }
}


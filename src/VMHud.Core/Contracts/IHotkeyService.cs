namespace VMHud.Core.Contracts;

public readonly record struct Hotkey(string Gesture);

public interface IHotkeyService
{
    void Register(Hotkey hotkey, Action onPressed, Action? onReleased = null);
    void Unregister(Hotkey hotkey);
}


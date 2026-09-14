namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Ported from personal-002. Non-visual pressed-signal accounting, kept separate so it
/// can be tested without Avalonia. Ref-counts signals so two keys mapped to the same signal (e.g.
/// two physical Shift keys) don't release it until both are up.</summary>
internal sealed class EmulatorKeyboardState(EmulatorKeyboardLayout layout, Action<string, bool> setKeyState)
{
    private readonly Dictionary<string, EmulatorKeyDefinition> _keys = layout.Keys.ToDictionary(key => key.Id, StringComparer.Ordinal);
    private readonly Dictionary<string, int> _signalReferences = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pressed = new(StringComparer.Ordinal);
    private readonly List<string> _pressOrder = [];

    public IReadOnlySet<string> PressedKeyIds => _pressed;

    public void Press(string id)
    {
        EmulatorKeyDefinition key = _keys[id];
        if (key.Behavior == EmulatorKeyBehavior.Toggle && _pressed.Contains(id)) { Release(id); return; }
        if (!_pressed.Add(id)) return;
        _pressOrder.Add(id);
        foreach (string signal in key.SignalIds)
        {
            _signalReferences.TryGetValue(signal, out int count);
            _signalReferences[signal] = count + 1;
            if (count == 0) setKeyState(signal, true);
        }
    }

    public void Release(string id)
    {
        EmulatorKeyDefinition key = _keys[id];
        if (!_pressed.Remove(id)) return;
        _pressOrder.Remove(id);
        for (int index = key.SignalIds.Count - 1; index >= 0; index--)
        {
            string signal = key.SignalIds[index];
            int count = _signalReferences[signal] - 1;
            if (count == 0) { _signalReferences.Remove(signal); setKeyState(signal, false); }
            else _signalReferences[signal] = count;
        }
    }

    public void ReleaseMomentary(string id)
    {
        if (_keys[id].Behavior == EmulatorKeyBehavior.Momentary) Release(id);
    }

    public void ReleaseAll()
    {
        foreach (string id in _pressOrder.ToArray().Reverse()) Release(id);
    }
}

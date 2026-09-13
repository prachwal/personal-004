namespace PetEmulator.Chips;

/// <summary>
/// Models the active-high IEI/IEO priority chain for Z80 PIO devices.
/// Sources are ordered from highest to lowest priority.
/// </summary>
public sealed class Z80PioInterruptChain(params IZ80PioInterruptSource[] sources)
{
    private readonly IZ80PioInterruptSource[] _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    private readonly Stack<int> _inService = new();

    public Z80PioInterruptChainState CaptureState() =>
        new(_inService.ToArray());

    public void RestoreState(Z80PioInterruptChainState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _inService.Clear();
        foreach (var index in state.InServiceOrder.Reverse())
        {
            if (index < 0 || index >= _sources.Length)
                throw new ArgumentOutOfRangeException(nameof(state));
            _inService.Push(index);
        }
        RefreshEnableInputs();
    }

    public bool InterruptRequested
    {
        get
        {
            RefreshEnableInputs();
            return _sources.Any(source => source.InterruptRequested);
        }
    }

    public bool TryAcknowledgeInterrupt(out byte vector)
    {
        vector = 0;
        RefreshEnableInputs();
        for (var index = 0; index < _sources.Length; index++)
        {
            var source = _sources[index];
            if (!source.InterruptInputEnabled || !source.InterruptRequested)
                continue;

            if (!source.TryAcknowledgeInterrupt(out vector))
                continue;

            _inService.Push(index);
            RefreshEnableInputs();
            return true;
        }

        return false;
    }

    public void NotifyReti()
    {
        if (_inService.Count == 0)
            return;

        _sources[_inService.Pop()].NotifyReti();
        RefreshEnableInputs();
    }

    private void RefreshEnableInputs()
    {
        var highestInService = _inService.Count == 0 ? int.MaxValue : _inService.Peek();
        var iei = true;
        for (var index = 0; index < _sources.Length; index++)
        {
            var enabled = iei && index <= highestInService;
            _sources[index].InterruptInputEnabled = enabled;

            if (!enabled || _sources[index].InterruptRequested)
                iei = false;
        }
    }
}

public sealed record Z80PioInterruptChainState(IReadOnlyList<int> InServiceOrder);

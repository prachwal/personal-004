using PetEmulator.Core;

namespace Cpu6502;

/// <summary>Default cycle counter used when no external <see cref="IClock"/> is injected.</summary>
internal sealed class Clock : IClock
{
    private ulong _cycleCount;

    public ulong CycleCount => _cycleCount;

    public void Reset() => _cycleCount = 0;

    public void Advance(ulong cycles) => _cycleCount += cycles;
}

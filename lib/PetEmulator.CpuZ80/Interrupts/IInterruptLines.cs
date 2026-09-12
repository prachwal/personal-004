using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Interrupts;

public interface IInterruptLines : PetEmulator.Core.IInterruptLines, IWaitLine
{
    /// <summary>
    /// The Z80's WAIT line - real hardware freezes the CPU mid-bus-cycle
    /// while this is asserted (no fetch, no execute, no interrupt
    /// service), released only by whatever external device asserted it.
    /// See <see cref="PetEmulator.Chips.FD1793"/>'s own doc
    /// comment for the one real source this codebase models (port 0xF4
    /// bit 6).
    /// </summary>
    void Clear();
}

namespace PetEmulator.Core;

/// <summary>One real access a machine's memory bus served - a CPU (or any other bus master)
/// reading or writing one byte at one address, with the value actually transferred. Doesn't
/// distinguish an opcode fetch from an operand/data access (a plain <see cref="IMemoryBus"/> has
/// no way to tell them apart, unlike a cycle-accurate CPU core's own bus-cycle stream).
///
/// Lives in Core (not machine-specific) because it's the payload of an optional
/// <c>Observer</c> hook any <see cref="IMemoryBus"/> implementation can expose - originally built
/// for <c>PetEmulator.Pet.PetMemoryBus</c>, moved here once a second machine (VIC-20) needed the
/// identical shape instead of a copy-pasted duplicate record.</summary>
public readonly record struct BusAccess(bool IsWrite, ushort Address, byte Value);

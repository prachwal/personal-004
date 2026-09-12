using System.Diagnostics.CodeAnalysis;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Memory;

namespace PetEmulator.CpuZ80.Tests;

public sealed class MemoryTests
{
    [Fact]
    public void RamMemoryStoresIndependentBytesAcrossAddressSpace()
    {
        var memory = new RamMemory();

        WriteThroughMemory(memory);

        Assert.Equal((byte)0x12, memory.Read(0x0000));
        Assert.Equal((byte)0x34, memory.Read(0xFFFF));
    }

    [Fact]
    public void RamMemoryRejectsAddressSpaceOtherThan64Kb()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RamMemory(1024));
    }

    [SuppressMessage("Performance", "CA1859", Justification = "This helper verifies the IMemoryBus contract.")]
    private static void WriteThroughMemory(IMemoryBus memory)
    {
        memory.Write(0x0000, 0x12);
        memory.Write(0xFFFF, 0x34);
    }
}

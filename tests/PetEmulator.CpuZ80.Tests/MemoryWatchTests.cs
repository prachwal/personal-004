using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Memory;

namespace PetEmulator.CpuZ80.Tests;

public sealed class MemoryWatchTests
{
    [Fact]
    public void HookFiresOnlyForItsWatchedAddress()
    {
        var bus = new SystemBus(new RamMemory());
        var hits = new List<(MemoryAccessKind Kind, ushort Address, byte Value)>();
        bus.Watch.Add(0x4000, new MemoryHook((_, _, _) => true, (k, a, v) => hits.Add((k, a, v))));

        bus.WriteMemory(0x4000, 0x42);
        bus.WriteMemory(0x4001, 0x99); // unwatched - must not fire
        bus.ReadMemory(0x4000);

        Assert.Equal(
        [
            (MemoryAccessKind.Write, (ushort)0x4000, (byte)0x42),
            (MemoryAccessKind.Read, (ushort)0x4000, (byte)0x42),
        ], hits);
    }

    [Fact]
    public void MultipleHooksOnTheSameAddressAllRun()
    {
        var bus = new SystemBus(new RamMemory());
        var order = new List<string>();
        bus.Watch.Add(0x4000, new MemoryHook((_, _, _) => true, (_, _, _) => order.Add("first")));
        bus.Watch.Add(0x4000, new MemoryHook((_, _, _) => true, (_, _, _) => order.Add("second")));

        bus.WriteMemory(0x4000, 0x01);

        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public void ConditionCanFilterByAccessKindOrValue()
    {
        var bus = new SystemBus(new RamMemory());
        var writesOfFive = 0;
        bus.Watch.Add(0x4000, new MemoryHook(
            (kind, _, value) => kind == MemoryAccessKind.Write && value == 5,
            (_, _, _) => writesOfFive++));

        bus.WriteMemory(0x4000, 3);
        bus.WriteMemory(0x4000, 5);
        bus.ReadMemory(0x4000); // a read of the same now-5 byte must not count

        Assert.Equal(1, writesOfFive);
    }

    [Fact]
    public void RewritingTheSameValueDoesNotFireAWriteHook()
    {
        var bus = new SystemBus(new RamMemory());
        var writeCount = 0;
        bus.Watch.Add(0x4000, new MemoryHook(
            (kind, _, _) => kind == MemoryAccessKind.Write,
            (_, _, _) => writeCount++));

        bus.WriteMemory(0x4000, 7);
        bus.WriteMemory(0x4000, 7); // same value again - must not count
        bus.WriteMemory(0x4000, 8); // an actual change - must count

        Assert.Equal(2, writeCount);
    }

    [Fact]
    public void UnwatchedAddressesNeverTouchAHook()
    {
        var bus = new SystemBus(new RamMemory());
        var fired = false;
        bus.Watch.Add(0x4000, new MemoryHook((_, _, _) => true, (_, _, _) => fired = true));

        for (ushort a = 0; a < 0x4000; a++)
            bus.ReadMemory(a);
        bus.ReadMemory(0x4001);

        Assert.False(fired);
    }
}

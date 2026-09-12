using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;
using PetEmulator.CpuZ80.Memory;

namespace PetEmulator.CpuZ80.Tests;

public sealed class CpuHookTests
{
    [Fact]
    public void HookFiresOnlyWhenItsConditionMatches()
    {
        // NOP, NOP, LD A,5, HALT - PC visits 0,1,2,4 across four Step() calls.
        var (cpu, _) = CreateCpu(0x00, 0x00, 0x3E, 0x05, 0x76);
        var firedAt = new List<ushort>();
        cpu.Hooks.Add(new CpuHook(c => c.Registers.PC == 2, c => firedAt.Add(c.Registers.PC)));

        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal([(ushort)2], firedAt);
    }

    [Fact]
    public void HookSeesStateBeforeTheStepItFiresOn()
    {
        var (cpu, _) = CreateCpu(0x3E, 0x05, 0x76); // LD A,5, HALT
        byte aAtHalt = 0xFF;
        cpu.Hooks.Add(new CpuHook(c => c.Registers.PC == 2, c => aAtHalt = c.Registers.A));

        cpu.Step(); // LD A,5
        cpu.Step(); // HALT - hook fires here, before HALT executes but after A was already loaded

        Assert.Equal((byte)5, aAtHalt);
    }

    [Fact]
    public void MultipleHooksRunInAdditionOrder()
    {
        var (cpu, _) = CreateCpu(0x00, 0x76);
        var order = new List<string>();
        cpu.Hooks.Add(new CpuHook(_ => true, _ => order.Add("first")));
        cpu.Hooks.Add(new CpuHook(_ => true, _ => order.Add("second")));

        cpu.Step();

        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public void NoPredicateOverloadFiresOnEveryStep()
    {
        var (cpu, _) = CreateCpu(0x00, 0x00, 0x76); // NOP, NOP, HALT
        var fireCount = 0;
        cpu.Hooks.Add(new CpuHook(_ => fireCount++));

        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal(3, fireCount);
    }

    [Fact]
    public void EmptyHooksListDoesNotAffectExecution()
    {
        var (cpu, memory) = CreateCpu(0x3E, 0x05, 0x76);

        cpu.Step();
        cpu.Step();

        Assert.True(cpu.Halted);
        Assert.Equal((byte)5, cpu.Registers.A);
        Assert.Empty(cpu.Hooks);
        _ = memory;
    }

    [Fact]
    public void HasAnyTracksAddAndRemoveNotJustCount()
    {
        var (cpu, _) = CreateCpu(0x00, 0x76);
        Assert.False(cpu.Hooks.HasAny);

        var hook = new CpuHook(_ => true, _ => { });
        cpu.Hooks.Add(hook);
        Assert.True(cpu.Hooks.HasAny);

        cpu.Hooks.Remove(hook);
        Assert.False(cpu.Hooks.HasAny); // last one removed - back to false, not just Count == 0
    }

    private static (Z80Cpu Cpu, RamMemory Memory) CreateCpu(params byte[] program)
    {
        var memory = new RamMemory();
        for (var index = 0; index < program.Length; index++)
            memory.Write((ushort)index, program[index]);

        var cpu = new Z80Cpu(new SystemBus(memory), new InterruptLines());
        return (cpu, memory);
    }
}

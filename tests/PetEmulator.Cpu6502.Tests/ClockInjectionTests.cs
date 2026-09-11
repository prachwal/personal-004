using PetEmulator.Cpu6502;
using PetEmulator.Cpu6502.Variants;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public class ClockInjectionTests
{
    private sealed class FakeClock : IClock
    {
        public ulong CycleCount { get; private set; }
        public int ResetCallCount { get; private set; }
        public void Reset() { CycleCount = 0; ResetCallCount++; }
        public void Advance(ulong cycles) => CycleCount += cycles;
    }

    [Test]
    public void Default_constructor_still_works_without_an_injected_clock()
    {
        var cpu = new Cpu6502Classic(new FlatMemory());
        cpu.Reset();
        Assert.AreEqual(0UL, cpu.CycleCount);
    }

    [Test]
    public void Injected_clock_is_exposed_via_the_Clock_property()
    {
        var clock = new FakeClock();
        var cpu = new Cpu6502Classic(new FlatMemory(), clock: clock);
        Assert.AreSame(clock, cpu.Clock);
    }

    [Test]
    public void Injected_clock_advances_as_instructions_execute()
    {
        var clock = new FakeClock();
        var memory = new FlatMemory();
        var cpu = new Cpu6502Classic(memory, clock: clock);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
        memory.Write(0x0000, 0xEA); // NOP, 2 cycles
        cpu.PC = 0x0000;
        cpu.StepInstruction();
        Assert.AreEqual(2UL, clock.CycleCount);
        Assert.AreEqual(clock.CycleCount, cpu.CycleCount, "CPU's own CycleCount must delegate to the injected clock");
    }

    [Test]
    public void Reset_resets_the_injected_clock()
    {
        var clock = new FakeClock();
        var memory = new FlatMemory();
        var cpu = new Cpu6502Classic(memory, clock: clock);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
        memory.Write(0x0000, 0xEA);
        cpu.PC = 0x0000;
        cpu.StepInstruction();
        cpu.Reset();
        Assert.AreEqual(0UL, clock.CycleCount);
        Assert.GreaterOrEqual(clock.ResetCallCount, 1);
    }

    [Test]
    public void Every_variant_accepts_an_injected_clock()
    {
        var mem = new FlatMemory();
        Assert.DoesNotThrow(() => new Cpu6502Classic(mem, clock: new FakeClock()));
        Assert.DoesNotThrow(() => new Cpu6502Nes(mem, clock: new FakeClock()));
        Assert.DoesNotThrow(() => new Cpu6502Commodore6510(mem, clock: new FakeClock()));
        Assert.DoesNotThrow(() => new Cpu6502Atari6507(mem, clock: new FakeClock()));
        Assert.DoesNotThrow(() => new Cpu6502Cmos65C02(mem, clock: new FakeClock()));
        Assert.DoesNotThrow(() => new Cpu6502WdcR65C02S(mem, clock: new FakeClock()));
    }
}

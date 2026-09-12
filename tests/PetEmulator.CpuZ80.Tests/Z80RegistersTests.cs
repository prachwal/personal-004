using PetEmulator.CpuZ80.Cpu;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80RegistersTests
{
    [Fact]
    public void StateResetClearsArchitecturalAndLifecycleState()
    {
        var state = new Z80State
        {
            AF = 0x1234,
            BC = 0x5678,
            IX = 0x9ABC,
            Iff1 = true,
            Iff2 = true,
            InterruptMode = 2,
            InterruptDelay = 2,
            PreviousNmi = true,
            Halted = true
        };

        state.Reset();

        Assert.Equal((ushort)0, state.AF);
        Assert.Equal((ushort)0, state.BC);
        Assert.Equal((ushort)0, state.IX);
        Assert.False(state.Iff1);
        Assert.False(state.Iff2);
        Assert.Equal((byte)0, state.InterruptMode);
        Assert.Equal(0, state.InterruptDelay);
        Assert.False(state.PreviousNmi);
        Assert.False(state.Halted);
    }

    [Fact]
    public void StateSnapshotRestoresArchitecturalAndLifecycleState()
    {
        var state = new Z80State
        {
            AF = 0x1234,
            BC = 0x5678,
            PC = 0x9ABC,
            SP = 0xDEF0,
            Iff1 = true,
            Iff2 = false,
            InterruptMode = 2,
            InterruptDelay = 1,
            PreviousNmi = true,
            Halted = true
        };

        var snapshot = state.CaptureSnapshot();
        state.Reset();
        state.RestoreSnapshot(snapshot);

        Assert.Equal((ushort)0x1234, state.AF);
        Assert.Equal((ushort)0x5678, state.BC);
        Assert.Equal((ushort)0x9ABC, state.PC);
        Assert.Equal((ushort)0xDEF0, state.SP);
        Assert.True(state.Iff1);
        Assert.False(state.Iff2);
        Assert.Equal((byte)2, state.InterruptMode);
        Assert.Equal(1, state.InterruptDelay);
        Assert.True(state.PreviousNmi);
        Assert.True(state.Halted);
    }

    [Fact]
    public void ResetClearsIndexAndAlternateRegisters()
    {
        var registers = new Z80Registers
        {
            IX = 0x1234,
            IY = 0x5678,
            AlternateAF = 0xAAAA,
            AlternateBC = 0xBBBB,
            AlternateDE = 0xCCCC,
            AlternateHL = 0xDDDD
        };

        registers.Reset();

        Assert.Equal((ushort)0, registers.IX);
        Assert.Equal((ushort)0, registers.IY);
        Assert.Equal((ushort)0, registers.AlternateAF);
        Assert.Equal((ushort)0, registers.AlternateBC);
        Assert.Equal((ushort)0, registers.AlternateDE);
        Assert.Equal((ushort)0, registers.AlternateHL);
    }

    [Fact]
    public void ExchangeInstructionsSwapMainAndAlternateState()
    {
        var bus = new TestBus();
        var cpu = new Z80Cpu(bus, new TestInterruptLines());
        cpu.Registers.AF = 0x1234;
        cpu.Registers.AlternateAF = 0x5678;
        cpu.Registers.BC = 0x1111;
        cpu.Registers.DE = 0x2222;
        cpu.Registers.HL = 0x3333;
        cpu.Registers.AlternateBC = 0x4444;
        cpu.Registers.AlternateDE = 0x5555;
        cpu.Registers.AlternateHL = 0x6666;

        bus.Memory[0] = 0x08;
        bus.Memory[1] = 0xD9;
        bus.Memory[2] = 0xEB;

        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal((ushort)0x5678, cpu.Registers.AF);
        Assert.Equal((ushort)0x4444, cpu.Registers.BC);
        Assert.Equal((ushort)0x6666, cpu.Registers.DE);
        Assert.Equal((ushort)0x5555, cpu.Registers.HL);
    }

    private sealed class TestBus : PetEmulator.CpuZ80.Bus.IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }

    private sealed class TestInterruptLines : PetEmulator.CpuZ80.Interrupts.IInterruptLines
    {
        public bool IntAsserted => false;
        public bool NmiAsserted => false;
        public bool WaitAsserted => false;
        public void Clear() { }
    }
}

using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80InterruptAndFlagsTests
{
    [Fact]
    public void EdInterruptModeOpcodesSelectAllThreeModes()
    {
        var (cpu, _) = CreateCpu(0xED, 0x46, 0xED, 0x56, 0xED, 0x5E);

        cpu.Step();
        Assert.Equal((byte)0, cpu.InterruptMode);
        cpu.Step();
        Assert.Equal((byte)1, cpu.InterruptMode);
        cpu.Step();
        Assert.Equal((byte)2, cpu.InterruptMode);
    }

    [Fact]
    public void SixteenBitAdcSetsSignOverflowAndClearsZeroCorrectly()
    {
        var (cpu, _) = CreateCpu(0x21, 0xFF, 0x7F, 0x01, 0x01, 0x00, 0xED, 0x4A);

        cpu.Step();
        cpu.Step();
        Assert.Equal(15, cpu.Step());

        Assert.Equal((ushort)0x8000, cpu.Registers.HL);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.ParityOverflow));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
    }

    [Fact]
    public void EiAllowsInterruptOnlyAfterFollowingInstruction()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus();
        bus.Memory[0] = 0xED;
        bus.Memory[1] = 0x56;
        bus.Memory[2] = 0xFB;
        bus.Memory[3] = 0x00;
        var cpu = new Z80Cpu(bus, lines);

        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.Equal((ushort)4, cpu.Registers.PC);
        Assert.Equal(13, cpu.Step());
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
    }

    [Fact]
    public void EiDelaysInterruptAcrossPrefixedInstruction()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { Memory = { [0] = 0xFB, [1] = 0xED, [2] = 0x47, [3] = 0x00 } };
        var cpu = new Z80Cpu(bus, lines);

        cpu.Step();
        Assert.Equal((ushort)1, cpu.Registers.PC);
        cpu.Step();
        Assert.Equal((ushort)3, cpu.Registers.PC);
        Assert.Equal(13, cpu.Step());
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
    }

    [Fact]
    public void DiCancelsPendingEiInterruptWindow()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { Memory = { [0] = 0xFB, [1] = 0xF3, [2] = 0x00 } };
        var cpu = new Z80Cpu(bus, lines);

        cpu.Step();
        cpu.Step();
        Assert.False(cpu.Iff1);
        Assert.Equal((ushort)2, cpu.Registers.PC);
        cpu.Step();

        Assert.Equal((ushort)3, cpu.Registers.PC);
        Assert.False(cpu.Iff1);
    }

    [Fact]
    public void NmiRequiresANewRisingEdgeAfterTheLineIsHeldHigh()
    {
        var lines = new TestInterruptLines { NmiAsserted = true };
        var bus = new TestBus();
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;

        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x0066, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);

        Assert.Equal(4, cpu.Step());
        Assert.Equal((ushort)0x0067, cpu.Registers.PC);

        lines.NmiAsserted = false;
        Assert.Equal(4, cpu.Step());
        Assert.Equal((ushort)0x0068, cpu.Registers.PC);

        lines.NmiAsserted = true;
        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x0066, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFC, cpu.Registers.SP);
    }

    [Fact]
    public void LoadAccumulatorFromInterruptRegisterUsesIff2ForParityFlag()
    {
        var (cpu, _) = CreateCpu(0xFB, 0xED, 0x57);
        cpu.Registers.I = 0x80;
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();
        Assert.Equal(9, cpu.Step());

        Assert.Equal((byte)0x80, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.ParityOverflow));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    [Fact]
    public void LoadAccumulatorFromRefreshRegisterUsesRefreshedValueAndIff2()
    {
        var (cpu, _) = CreateCpu(0xFB, 0xED, 0x5F);
        cpu.Registers.R = 0xFD;
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();
        Assert.Equal((byte)0xFE, cpu.Registers.R);
        Assert.Equal(9, cpu.Step());
        Assert.Equal((byte)0x80, cpu.Registers.R);

        Assert.Equal((byte)0x80, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.ParityOverflow));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    [Fact]
    public void LoadAccumulatorFromInterruptRegisterClearsParityWhenIff2IsClear()
    {
        var (cpu, _) = CreateCpu(0xED, 0x57);
        cpu.Registers.I = 0x80;
        cpu.Registers.F = Z80Flags.Carry;

        Assert.Equal(9, cpu.Step());

        Assert.Equal((byte)0x80, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.ParityOverflow));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    [Fact]
    public void LoadAccumulatorFromRefreshRegisterClearsParityWhenIff2IsClear()
    {
        var (cpu, _) = CreateCpu(0xED, 0x5F);
        cpu.Registers.R = 0x80;
        cpu.Registers.F = Z80Flags.Carry;

        Assert.Equal(9, cpu.Step());

        Assert.Equal((byte)0x82, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.ParityOverflow));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    [Fact]
    public void LoadAccumulatorFromInterruptRegisterCopiesUndocumentedBits()
    {
        var (cpu, _) = CreateCpu(0xFB, 0xED, 0x57);
        cpu.Registers.I = 0x28;
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();
        Assert.Equal(9, cpu.Step());

        Assert.Equal((byte)0x28, cpu.Registers.A);
        Assert.Equal((byte)(Z80Flags.ParityOverflow | Z80Flags.X | Z80Flags.Y | Z80Flags.Carry), cpu.Registers.F);
    }

    [Fact]
    public void LoadAccumulatorFromRefreshRegisterCopiesUndocumentedBits()
    {
        var (cpu, _) = CreateCpu(0xED, 0x5F);
        cpu.Registers.R = 0xA6;
        cpu.Registers.F = Z80Flags.Carry;

        Assert.Equal(9, cpu.Step());

        Assert.Equal((byte)0xA8, cpu.Registers.A);
        Assert.Equal((byte)(Z80Flags.Sign | Z80Flags.X | Z80Flags.Y | Z80Flags.Carry), cpu.Registers.F);
    }

    private static (Z80Cpu Cpu, TestBus Bus) CreateCpu(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        return (new Z80Cpu(bus, new TestInterruptLines()), bus);
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }

    private sealed class TestInterruptLines : IInterruptLines
    {
        public bool IntAsserted { get; set; }
        public bool NmiAsserted { get; set; }
        public bool WaitAsserted { get; set; }
        public void Clear() { }
    }
}

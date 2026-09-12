using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80BaseOpcodeTests
{
    [Fact]
    public void SixteenBitPairInstructionsUpdateTheSelectedPair()
    {
        var (cpu, _) = CreateCpu(0x01, 0x34, 0x12, 0x21, 0x00, 0x10, 0x03, 0x09, 0x0B);

        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal((ushort)0x1234, cpu.Registers.BC);
        Assert.Equal((ushort)0x2235, cpu.Registers.HL);
        Assert.Equal((ushort)0x0009, cpu.Registers.PC);
    }

    [Fact]
    public void DjnzDecrementsBAndBranchesOnlyWhenNonZero()
    {
        var (cpu, _) = CreateCpu(0x06, 0x02, 0x10, 0xFE);

        cpu.Step();
        Assert.Equal(13, cpu.Step());
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);

        cpu.Step();
        Assert.Equal((byte)0, cpu.Registers.B);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
    }

    [Fact]
    public void AccumulatorRotatesAndPreservesNonArithmeticFlags()
    {
        var (cpu, _) = CreateCpu(0x3E, 0x81, 0x07, 0x17, 0x0F);

        cpu.Step();
        cpu.Step();
        Assert.Equal((byte)0x03, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));

        cpu.Step();
        Assert.Equal((byte)0x07, cpu.Registers.A);
        cpu.Step();
        Assert.Equal((byte)0x83, cpu.Registers.A);
    }

    [Fact]
    public void DaaAdjustsPackedDecimalAddition()
    {
        var (cpu, _) = CreateCpu(0x3E, 0x09, 0xC6, 0x01, 0x27);

        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal((byte)0x10, cpu.Registers.A);
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    [Fact]
    public void DaaAdjustsPackedDecimalSubtractionAndPreservesSubtract()
    {
        var (cpu, _) = CreateCpu(0x3E, 0x10, 0xD6, 0x01, 0x27);

        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal((byte)0x09, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.AddSubtract));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    private static (Z80Cpu Cpu, TestBus Bus) CreateCpu(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        return (new Z80Cpu(bus, new InterruptLines()), bus);
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }
}

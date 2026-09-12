using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80EdOpcodeTests
{
    [Fact]
    public void EdLoadsInterruptAndRefreshRegisters()
    {
        var (cpu, _) = CreateCpu(0x3E, 0x12, 0xED, 0x47, 0xED, 0x4F);

        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal((byte)0x12, cpu.Registers.I);
        Assert.Equal((byte)0x12, cpu.Registers.R);
        Assert.Equal((byte)0x12, cpu.Registers.A);
    }

    [Fact]
    public void EdSixteenBitArithmeticUsesCarryAndSetsHl()
    {
        var (cpu, _) = CreateCpu(0x21, 0x00, 0x10, 0x01, 0x01, 0x00, 0xED, 0x4A, 0x37, 0xED, 0x42);

        cpu.Step();
        cpu.Step();
        Assert.Equal(15, cpu.Step());
        Assert.Equal((ushort)0x1001, cpu.Registers.HL);

        cpu.Step();
        Assert.Equal(15, cpu.Step());
        Assert.Equal((ushort)0x0FFF, cpu.Registers.HL);
    }

    [Fact]
    public void LddrCopiesBackwardsUntilBcReachesZero()
    {
        var (cpu, bus) = CreateCpu(0xED, 0xB8);
        cpu.Registers.HL = 0x2001;
        cpu.Registers.DE = 0x3001;
        cpu.Registers.BC = 2;
        bus.Memory[0x2001] = 0x12;
        bus.Memory[0x2000] = 0x34;

        Assert.Equal(21, cpu.Step());
        Assert.Equal(16, cpu.Step());

        Assert.Equal((byte)0x12, bus.Memory[0x3001]);
        Assert.Equal((byte)0x34, bus.Memory[0x3000]);
        Assert.Equal((ushort)0, cpu.Registers.BC);
        Assert.Equal((ushort)0x1FFF, cpu.Registers.HL);
    }

    [Fact]
    public void EdInputUsesPortCAndUpdatesFlags()
    {
        var (cpu, bus) = CreateCpu(0x01, 0x00, 0xA0, 0xED, 0x78);
        bus.PortValue = 0x80;

        cpu.Step();
        Assert.Equal(12, cpu.Step());

        Assert.Equal((byte)0x80, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
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
        public byte PortValue { get; set; }
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => PortValue;
        public void WritePort(byte port, byte value) { }
    }
}

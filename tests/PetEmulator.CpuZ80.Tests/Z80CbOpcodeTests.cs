using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80CbOpcodeTests
{
    [Fact]
    public void RotateAndShiftFamiliesUpdateValueAndFlags()
    {
        var (cpu, _) = CreateCpu(0x06, 0x81, 0xCB, 0x00, 0xCB, 0x28, 0xCB, 0x38);

        cpu.Step();
        cpu.Step();
        Assert.Equal((byte)0x03, cpu.Registers.B);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
        cpu.Step();
        Assert.Equal((byte)0x01, cpu.Registers.B);
        cpu.Step();
        Assert.Equal((byte)0x00, cpu.Registers.B);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
    }

    [Fact]
    public void BitResAndSetOperateOnRegister()
    {
        var (cpu, _) = CreateCpu(0x06, 0x01, 0xCB, 0x40, 0xCB, 0x80, 0xCB, 0xC0);

        cpu.Step();
        cpu.Step();
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        cpu.Step();
        Assert.Equal((byte)0x00, cpu.Registers.B);
        cpu.Step();
        Assert.Equal((byte)0x01, cpu.Registers.B);
    }

    [Fact]
    public void CbMemoryVariantsUseHlAndCorrectTiming()
    {
        var (cpu, bus) = CreateCpu(0x21, 0x00, 0x40, 0x36, 0x80, 0xCB, 0x06, 0xCB, 0x7E);

        cpu.Step();
        cpu.Step();
        Assert.Equal(15, cpu.Step());
        Assert.Equal((byte)0x01, bus.Memory[0x4000]);
        Assert.Equal(12, cpu.Step());
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
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

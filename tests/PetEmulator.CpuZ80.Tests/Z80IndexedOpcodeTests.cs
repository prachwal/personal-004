using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80IndexedOpcodeTests
{
    [Fact]
    public void DdAndFdLoadIndexedMemoryWithSignedDisplacement()
    {
        var (cpu, bus) = CreateCpu(
            0xDD, 0x21, 0x00, 0x40,
            0xDD, 0x36, 0xFE, 0x12,
            0xFD, 0x21, 0x00, 0x50,
            0xFD, 0x36, 0x01, 0x34);

        cpu.Step();
        Assert.Equal(19, cpu.Step());
        cpu.Step();
        Assert.Equal(19, cpu.Step());

        Assert.Equal((byte)0x12, bus.Memory[0x3FFE]);
        Assert.Equal((byte)0x34, bus.Memory[0x5001]);
    }

    [Fact]
    public void IndexedRegisterLoadAndAddUseSelectedIndex()
    {
        var (cpu, bus) = CreateCpu(
            0xDD, 0x21, 0x00, 0x40,
            0xDD, 0x36, 0x02, 0x2A,
            0xDD, 0x7E, 0x02,
            0xDD, 0x09);
        cpu.Registers.BC = 1;

        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.Equal((byte)0x2A, cpu.Registers.A);
        cpu.Step();

        Assert.Equal((ushort)0x4001, cpu.Registers.IX);
        Assert.Equal((byte)0x2A, bus.Memory[0x4002]);
    }

    [Fact]
    public void LoadHOrLFromIndexedMemoryUsesRealRegistersNotIndexHalves()
    {
        // Real Z80 quirk: register codes 4/5 mean IXH/IXL only in a pure
        // register-to-register DD op. The moment (IX+d) is one operand,
        // the other operand's 4/5 reverts to plain H/L - found via a real
        // TRS-80 Level II ROM boot trace: this exact confusion corrupted
        // IX mid-driver-dispatch and sent the CPU into an infinite reset
        // loop (RET popped a stack slot the corrupted IX had never
        // actually written the real return address into).
        var (cpu, bus) = CreateCpu(
            0xDD, 0x21, 0x00, 0x40, // LD IX,4000H
            0xDD, 0x66, 0x01,       // LD H,(IX+1)
            0xDD, 0x6E, 0x02);      // LD L,(IX+2)
        bus.Memory[0x4001] = 0xAA;
        bus.Memory[0x4002] = 0xBB;

        cpu.Step(); // LD IX,4000H
        cpu.Step(); // LD H,(IX+1)
        Assert.Equal((byte)0xAA, cpu.Registers.H);
        Assert.Equal((ushort)0x4000, cpu.Registers.IX); // must be untouched
        cpu.Step(); // LD L,(IX+2)
        Assert.Equal((byte)0xBB, cpu.Registers.L);
        Assert.Equal((ushort)0x4000, cpu.Registers.IX); // must be untouched
    }

    [Fact]
    public void StoreHOrLToIndexedMemoryWritesRealRegistersNotIndexHalves()
    {
        var (cpu, bus) = CreateCpu(
            0xDD, 0x21, 0x00, 0x40, // LD IX,4000H
            0x26, 0xCC,             // LD H,0xCC
            0x2E, 0xDD,             // LD L,0xDD
            0xDD, 0x74, 0x01,       // LD (IX+1),H
            0xDD, 0x75, 0x02);      // LD (IX+2),L

        cpu.Step(); // LD IX
        cpu.Step(); // LD H,CC
        cpu.Step(); // LD L,DD
        cpu.Step(); // LD (IX+1),H
        Assert.Equal((byte)0xCC, bus.Memory[0x4001]);
        cpu.Step(); // LD (IX+2),L
        Assert.Equal((byte)0xDD, bus.Memory[0x4002]);
        Assert.Equal((ushort)0x4000, cpu.Registers.IX); // must be untouched throughout
    }

    [Fact]
    public void IndexedCbOperatesOnMemoryAfterDisplacement()
    {
        var (cpu, bus) = CreateCpu(0xFD, 0x21, 0x00, 0x40, 0xFD, 0xCB, 0xFF, 0xC6);

        cpu.Step();
        Assert.Equal(23, cpu.Step());

        Assert.Equal((byte)1, bus.Memory[0x3FFF]);
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

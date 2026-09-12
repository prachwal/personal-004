using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80CbSemanticMatrixTests
{
    [Fact]
    public void EveryCbOpcodeMatchesItsRegisterOperandSemantics()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        for (var input = 0; input <= byte.MaxValue; input++)
        {
            var bus = new TestBus { Memory = { [0] = 0xCB, [1] = (byte)opcode } };
            var cpu = new Z80Cpu(bus, new InterruptLines());
            cpu.Registers.B = (byte)input;
            cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = cpu.Registers.A = (byte)input;
            cpu.Registers.H = cpu.Registers.L = (byte)input;
            if ((opcode & 7) == 6)
            {
                cpu.Registers.HL = 0x4000;
                bus.Memory[0x4000] = (byte)input;
            }
            cpu.Registers.F = Z80Flags.Carry;
            cpu.Step();

            var group = opcode >> 6;
            var bit = (opcode >> 3) & 7;
            var operation = (opcode >> 3) & 7;
            var expected = group switch
            {
                0 => Rotate(operation, (byte)input, true),
                1 => (byte)input,
                2 => (byte)(input & ~(1 << bit)),
                _ => (byte)(input | (1 << bit))
            };
            var actual = (opcode & 7) == 6 ? bus.Memory[0x4000] : ReadRegister(cpu, opcode & 7);
            Assert.Equal(expected, actual);

            var expectedFlags = group switch
            {
                0 => RotateFlags(operation, (byte)input, true),
                1 => BitFlags((byte)input, bit, (opcode & 7) == 6 ? (byte)0 : (byte)input),
                _ => Z80Flags.Carry
            };
            Assert.Equal(expectedFlags, cpu.Registers.F);
        }
    }

    private static byte Rotate(int operation, byte value, bool carry)
    {
        var oldCarry = carry ? 1 : 0;
        return operation switch
        {
            0 => (byte)((value << 1) | (value >> 7)),
            1 => (byte)((value >> 1) | (value << 7)),
            2 => (byte)((value << 1) | oldCarry),
            3 => (byte)((value >> 1) | (oldCarry << 7)),
            4 => (byte)(value << 1),
            5 => (byte)((value >> 1) | (value & 0x80)),
            6 => (byte)((value << 1) | 1),
            _ => (byte)(value >> 1)
        };
    }

    private static byte RotateFlags(int operation, byte input, bool carry)
    {
        var result = Rotate(operation, input, carry);
        var outgoingCarry = operation is 0 or 2 or 4 or 6 ? (input & 0x80) != 0 : (input & 1) != 0;
        return (byte)(Z80Flags.SignZero(result) | Z80Flags.Parity(result) |
            (result & (Z80Flags.X | Z80Flags.Y)) | (outgoingCarry ? Z80Flags.Carry : 0));
    }

    private static byte BitFlags(byte value, int bit, byte undocumented)
    {
        var set = (value & (1 << bit)) != 0;
        return (byte)(Z80Flags.Carry | Z80Flags.HalfCarry |
            (bit == 7 && set ? Z80Flags.Sign : 0) |
            (set ? 0 : Z80Flags.Zero | Z80Flags.ParityOverflow) |
            (undocumented & (Z80Flags.X | Z80Flags.Y)));
    }

    [Fact]
    public void BitMemoryUsesAddressHighByteForUndocumentedFlags()
    {
        var bus = new TestBus { Memory = { [0] = 0xCB, [1] = 0x46, [0xA800] = 0x00 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.HL = 0xA800;
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();

        Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.HalfCarry | Z80Flags.X | Z80Flags.Y), cpu.Registers.F);
    }

    [Theory]
    [InlineData(0x80, 0xFF, 0xFE)]
    [InlineData(0x88, 0xFF, 0xFD)]
    [InlineData(0x90, 0xFF, 0xFB)]
    [InlineData(0x98, 0xFF, 0xF7)]
    [InlineData(0xC0, 0x00, 0x01)]
    [InlineData(0xC8, 0x00, 0x02)]
    [InlineData(0xD0, 0x00, 0x04)]
    [InlineData(0xD8, 0x00, 0x08)]
    public void ResAndSetPreserveAllFlags(byte opcode, byte input, byte expected)
    {
        var bus = new TestBus { Memory = { [0] = 0xCB, [1] = opcode } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.B = input;
        cpu.Registers.F = 0xFF;

        cpu.Step();

        Assert.Equal(expected, cpu.Registers.B);
        Assert.Equal((byte)0xFF, cpu.Registers.F);
    }

    private static byte ReadRegister(Z80Cpu cpu, int register) => register switch
    {
        0 => cpu.Registers.B,
        1 => cpu.Registers.C,
        2 => cpu.Registers.D,
        3 => cpu.Registers.E,
        4 => cpu.Registers.H,
        5 => cpu.Registers.L,
        _ => cpu.Registers.A
    };

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }
}

using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80BaseValidationMatrixTests
{
    [Fact]
    public void EveryBaseOpcodeHasIndependentPcAndTStateExpectations()
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var opcode = (byte)value;
            var (cpu, bus) = CreateCpu(opcode, 0x00, 0x00);
            Initialize(cpu);
            bus.Memory[0x4000] = 0x78;
            bus.Memory[0x4001] = 0x56;
            var expected = Expected(opcode);

            var cycles = cpu.Step();

            Assert.True(cycles == expected.TStates, $"opcode={opcode:X2} expected T={expected.TStates} actual={cycles}");
            Assert.True(cpu.Registers.PC == expected.PC, $"opcode={opcode:X2} expected PC={expected.PC:X4} actual={cpu.Registers.PC:X4}");
        }
    }

    [Fact]
    public void ConditionalBaseOpcodesCoverTakenAndUntakenBranches()
    {
        foreach (var (opcode, takenFlags, notTakenFlags, takenTStates, skippedTStates) in new (byte, byte, byte, int, int)[]
        {
            (0x20, 0, Z80Flags.Zero, 12, 7), (0x28, Z80Flags.Zero, 0, 12, 7),
            (0x30, 0, Z80Flags.Carry, 12, 7), (0x38, Z80Flags.Carry, 0, 12, 7),
            (0xC0, 0, Z80Flags.Zero, 11, 5), (0xC8, Z80Flags.Zero, 0, 11, 5),
            (0xD0, 0, Z80Flags.Carry, 11, 5), (0xD8, Z80Flags.Carry, 0, 11, 5),
            (0xE0, 0, Z80Flags.ParityOverflow, 11, 5), (0xE8, Z80Flags.ParityOverflow, 0, 11, 5),
            (0xF0, 0, Z80Flags.Sign, 11, 5), (0xF8, Z80Flags.Sign, 0, 11, 5),
            (0xC2, 0, Z80Flags.Zero, 10, 10), (0xCA, Z80Flags.Zero, 0, 10, 10),
            (0xD2, 0, Z80Flags.Carry, 10, 10), (0xDA, Z80Flags.Carry, 0, 10, 10),
            (0xE2, 0, Z80Flags.ParityOverflow, 10, 10), (0xEA, Z80Flags.ParityOverflow, 0, 10, 10),
            (0xF2, 0, Z80Flags.Sign, 10, 10), (0xFA, Z80Flags.Sign, 0, 10, 10),
            (0xC4, 0, Z80Flags.Zero, 17, 10), (0xCC, Z80Flags.Zero, 0, 17, 10),
            (0xD4, 0, Z80Flags.Carry, 17, 10), (0xDC, Z80Flags.Carry, 0, 17, 10),
            (0xE4, 0, Z80Flags.ParityOverflow, 17, 10), (0xEC, Z80Flags.ParityOverflow, 0, 17, 10),
            (0xF4, 0, Z80Flags.Sign, 17, 10), (0xFC, Z80Flags.Sign, 0, 17, 10)
        })
        {
            AssertConditional(opcode, takenFlags, true, takenTStates);
            AssertConditional(opcode, notTakenFlags, false, skippedTStates);
        }
    }

    [Fact]
    public void BaseOpcodeFamiliesHaveRepresentativeIndependentEffects()
    {
        var (transfer, transferBus) = CreateCpu(0x36, 0x5A, 0x7E);
        transfer.Registers.HL = 0x4000;
        Assert.Equal(10, transfer.Step());
        Assert.Equal((byte)0x5A, transferBus.Memory[0x4000]);
        Assert.Equal(7, transfer.Step());
        Assert.Equal((byte)0x5A, transfer.Registers.A);

        var (arithmetic, _) = CreateCpu(0x80, 0x90, 0xA0, 0xB0);
        arithmetic.Registers.A = 0x7F;
        arithmetic.Registers.B = 0x01;
        Assert.Equal(4, arithmetic.Step());
        Assert.Equal((byte)0x80, arithmetic.Registers.A);
        Assert.Equal((byte)(Z80Flags.Sign | Z80Flags.HalfCarry | Z80Flags.ParityOverflow), arithmetic.Registers.F);
        Assert.Equal(4, arithmetic.Step());
        Assert.Equal((byte)0x7F, arithmetic.Registers.A);
        Assert.Equal(4, arithmetic.Step());
        Assert.Equal((byte)0x01, arithmetic.Registers.A);
        Assert.Equal(4, arithmetic.Step());
        Assert.Equal((byte)0x01, arithmetic.Registers.A);

        var (stack, stackBus) = CreateCpu(0xC5, 0xC1);
        stack.Registers.BC = 0xA55A;
        stack.Registers.SP = 0x4000;
        Assert.Equal(11, stack.Step());
        Assert.Equal((byte)0x5A, stackBus.Memory[0x3FFE]);
        stack.Registers.BC = 0;
        Assert.Equal(10, stack.Step());
        Assert.Equal((ushort)0xA55A, stack.Registers.BC);
    }

    private static void AssertConditional(byte opcode, byte flags, bool taken, int expectedTStates)
    {
        var (cpu, bus) = CreateCpu(opcode, 0x34, 0x12);
        Initialize(cpu);
        cpu.Registers.F = flags;
        bus.Memory[0x4000] = 0x78;
        bus.Memory[0x4001] = 0x56;

        Assert.Equal(expectedTStates, cpu.Step());
        var isReturn = (opcode & 0xC7) == 0xC0;
        var isRelative = opcode is >= 0x20 and <= 0x38;
        var expectedPc = isReturn ? (taken ? (ushort)0x5678 : (ushort)1) : taken ? (isRelative ? (ushort)0x0036 : (ushort)0x1234) : (ushort)(isRelative ? 2 : 3);
        Assert.Equal(expectedPc, cpu.Registers.PC);
    }

    private static ExpectedState Expected(byte opcode) => opcode switch
    {
        0x10 => new(8, 2), 0x18 => new(12, 2), 0x20 or 0x30 => new(12, 2), 0x28 or 0x38 => new(7, 2),
        0x22 or 0x2A => new(16, 3), 0x32 or 0x3A => new(13, 3),
        >= 0x40 and <= 0x7F => new(opcode == 0x76 ? 4 : (opcode & 7) == 6 || ((opcode >> 3) & 7) == 6 ? 7 : 4, 1),
        >= 0x80 and <= 0xBF => new((opcode & 7) == 6 ? 7 : 4, 1),
        0xC0 or 0xD0 or 0xE0 or 0xF0 => new(11, 0x5678),
        0xC8 or 0xD8 or 0xE8 or 0xF8 => new(5, 1),
        0xC1 or 0xD1 or 0xE1 or 0xF1 => new(10, 1),
        0xC2 or 0xD2 or 0xE2 or 0xF2 => new(10, 0),
        0xCA or 0xDA or 0xEA or 0xFA => new(10, 3),
        0xC3 => new(10, 0), 0xC4 or 0xD4 or 0xE4 or 0xF4 => new(17, 0),
        0xCC or 0xDC or 0xEC or 0xFC => new(10, 3), 0xC5 or 0xD5 or 0xE5 or 0xF5 => new(11, 1),
        0xC6 or 0xCE or 0xD6 or 0xDE or 0xE6 or 0xEE or 0xF6 or 0xFE => new(7, 2),
        0xC7 => new(11, 0), 0xCF => new(11, 8), 0xD7 => new(11, 0x10), 0xDF => new(11, 0x18),
        0xE7 => new(11, 0x20), 0xEF => new(11, 0x28), 0xF7 => new(11, 0x30), 0xFF => new(11, 0x38),
        0xC9 => new(10, 0x5678), 0xCB or 0xED or 0xDD or 0xFD => new(8, 2),
        0xCD => new(17, 0), 0xD3 or 0xDB => new(11, 2), 0xD9 or 0xEB or 0xF3 or 0xFB => new(4, 1),
        0xE3 => new(19, 1), 0xE9 => new(4, 0x5000), 0xF9 => new(6, 1),
        _ => new(BaseTStates(opcode), BaseLength(opcode))
    };

    private static int BaseTStates(byte opcode) => (opcode & 0x0F) switch
    {
        0x04 or 0x05 when (opcode & 0x38) == 0x30 => 11,
        0x06 when (opcode & 0x38) == 0x30 => 10,
        0x01 => 10, 0x02 or 0x0A => 7, 0x03 or 0x0B => 6, 0x04 or 0x05 or 0x07 or 0x08 or 0x0C or 0x0D or 0x0F => 4,
        0x06 or 0x0E => 7, 0x09 => 11, _ => 4
    };

    private static ushort BaseLength(byte opcode) => opcode is 0x01 or 0x11 or 0x21 or 0x31
        ? (ushort)3
        : (opcode & 0xC7) == 0x06 ? (ushort)2 : (ushort)1;

    private static void Initialize(Z80Cpu cpu)
    {
        cpu.Registers.AF = 0x3300;
        cpu.Registers.BC = 0x0112;
        cpu.Registers.DE = 0x3456;
        cpu.Registers.HL = 0x5000;
        cpu.Registers.SP = 0x4000;
    }

    private static (Z80Cpu Cpu, TestBus Bus) CreateCpu(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        return (new Z80Cpu(bus, new InterruptLines()), bus);
    }

    private sealed record ExpectedState(int TStates, ushort PC);

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }
}

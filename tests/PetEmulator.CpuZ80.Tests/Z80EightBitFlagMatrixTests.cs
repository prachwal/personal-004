using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80EightBitFlagMatrixTests
{
    [Fact]
    public void AddAdcSubSbcAndCpMatchIndependentFlagEquations()
    {
        foreach (var opcode in new byte[] { 0x80, 0x88, 0x90, 0x98, 0xB8 })
        {
            for (var a = 0; a <= byte.MaxValue; a++)
            {
                for (var value = 0; value <= byte.MaxValue; value++)
                {
                    foreach (var carry in new[] { false, true })
                    {
                        var cpu = CreateCpu(opcode);
                        cpu.Registers.A = (byte)a;
                        cpu.Registers.B = (byte)value;
                        cpu.Registers.F = carry ? Z80Flags.Carry : (byte)0;
                        cpu.Step();

                        var expected = opcode switch
                        {
                            0x80 or 0x88 => AddFlags((byte)a, (byte)value, opcode == 0x88 && carry),
                            _ => SubtractFlags((byte)a, (byte)value, opcode == 0x98 && carry)
                        };
                        Assert.Equal(expected, cpu.Registers.F);
                        if (opcode is 0x80 or 0x88)
                            Assert.Equal((byte)(a + value + (opcode == 0x88 && carry ? 1 : 0)), cpu.Registers.A);
                        else if (opcode == 0xB8)
                            Assert.Equal((byte)a, cpu.Registers.A);
                        else
                            Assert.Equal((byte)(a - value - (opcode == 0x98 && carry ? 1 : 0)), cpu.Registers.A);
                    }
                }
            }
        }
    }

    [Fact]
    public void IncAndDecMatchIndependentFlagEquationsAndPreserveCarry()
    {
        foreach (var value in Enumerable.Range(0, 256).Select(static value => (byte)value))
        {
            foreach (var carry in new[] { false, true })
            {
                var inc = CreateCpu(0x04);
                inc.Registers.B = value;
                inc.Registers.F = carry ? Z80Flags.Carry : (byte)0;
                inc.Step();
                Assert.Equal(IncrementFlags(value, carry), inc.Registers.F);

                var dec = CreateCpu(0x05);
                dec.Registers.B = value;
                dec.Registers.F = carry ? Z80Flags.Carry : (byte)0;
                dec.Step();
                Assert.Equal(DecrementFlags(value, carry), dec.Registers.F);
            }
        }
    }

    [Fact]
    public void LogicalOperationsMatchIndependentFlagsForEveryInput()
    {
        foreach (var opcode in new byte[] { 0xA0, 0xA8, 0xB0 })
        for (var accumulator = 0; accumulator <= byte.MaxValue; accumulator++)
        for (var operand = 0; operand <= byte.MaxValue; operand++)
        {
            var cpu = CreateCpu(opcode);
            cpu.Registers.A = (byte)accumulator;
            cpu.Registers.B = (byte)operand;
            cpu.Step();

            var result = opcode switch
            {
                0xA0 => (byte)(accumulator & operand),
                0xA8 => (byte)(accumulator ^ operand),
                _ => (byte)(accumulator | operand)
            };
            var expectedFlags = LogicalFlags(result, opcode == 0xA0);
            Assert.Equal(result, cpu.Registers.A);
            Assert.Equal(expectedFlags, cpu.Registers.F);
        }
    }

    [Fact]
    public void DaaMatchesIndependentAdjustmentForAllRelevantInputFlags()
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            foreach (var subtract in new[] { false, true })
            foreach (var halfCarry in new[] { false, true })
            foreach (var carry in new[] { false, true })
            {
                var cpu = CreateCpu(0x27);
                cpu.Registers.A = (byte)value;
                cpu.Registers.F = (byte)((subtract ? Z80Flags.AddSubtract : 0) |
                    (halfCarry ? Z80Flags.HalfCarry : 0) | (carry ? Z80Flags.Carry : 0));

                cpu.Step();

                var expected = AdjustDecimal((byte)value, subtract, halfCarry, carry);
                Assert.Equal(expected.A, cpu.Registers.A);
                Assert.Equal(expected.F, cpu.Registers.F);
            }
        }
    }

    [Theory]
    [InlineData(0x80)]
    [InlineData(0x81)]
    [InlineData(0x82)]
    [InlineData(0x83)]
    [InlineData(0x84)]
    [InlineData(0x85)]
    [InlineData(0x86)]
    [InlineData(0x87)]
    public void AddOracleCoversEveryRegisterAndMemorySource(byte opcode)
    {
        var bus = new TestBus { Memory = { [0] = opcode, [0x4000] = 0x81 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0x7F;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x81;
        cpu.Registers.H = cpu.Registers.L = 0x81;
        cpu.Registers.HL = 0x4000;

        var value = opcode switch
        {
            0x84 => cpu.Registers.H,
            0x85 => cpu.Registers.L,
            0x87 => cpu.Registers.A,
            _ => (byte)0x81
        };

        cpu.Step();

        Assert.Equal((byte)(0x7F + value), cpu.Registers.A);
        Assert.Equal(AddFlags(0x7F, value, false), cpu.Registers.F);
    }

    [Theory]
    [InlineData(0x90)]
    [InlineData(0x91)]
    [InlineData(0x92)]
    [InlineData(0x93)]
    [InlineData(0x94)]
    [InlineData(0x95)]
    [InlineData(0x96)]
    [InlineData(0x97)]
    public void SubOracleCoversEveryRegisterAndMemorySource(byte opcode)
    {
        var bus = new TestBus { Memory = { [0] = opcode, [0x4000] = 0x01 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0x80;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x01;
        cpu.Registers.H = cpu.Registers.L = 0x01;
        cpu.Registers.HL = 0x4000;

        var value = opcode switch
        {
            0x94 => cpu.Registers.H,
            0x95 => cpu.Registers.L,
            0x97 => cpu.Registers.A,
            _ => (byte)0x01
        };
        cpu.Step();

        Assert.Equal((byte)(0x80 - value), cpu.Registers.A);
        Assert.Equal(SubtractFlags(0x80, value, false), cpu.Registers.F);
    }

    [Theory]
    [InlineData(0x88)]
    [InlineData(0x89)]
    [InlineData(0x8A)]
    [InlineData(0x8B)]
    [InlineData(0x8C)]
    [InlineData(0x8D)]
    [InlineData(0x8E)]
    [InlineData(0x8F)]
    public void AdcOracleCoversEveryRegisterAndMemorySource(byte opcode)
    {
        var bus = new TestBus { Memory = { [0] = opcode, [0x4000] = 0x01 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0x7F;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x01;
        cpu.Registers.H = cpu.Registers.L = 0x01;
        cpu.Registers.HL = 0x4000;
        var value = opcode switch
        {
            0x8C => cpu.Registers.H,
            0x8D => cpu.Registers.L,
            0x8F => cpu.Registers.A,
            _ => (byte)0x01
        };
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();

        Assert.Equal((byte)(0x7F + value + 1), cpu.Registers.A);
        Assert.Equal(AddFlags(0x7F, value, true), cpu.Registers.F);
    }

    [Theory]
    [InlineData(0x98)]
    [InlineData(0x99)]
    [InlineData(0x9A)]
    [InlineData(0x9B)]
    [InlineData(0x9C)]
    [InlineData(0x9D)]
    [InlineData(0x9E)]
    [InlineData(0x9F)]
    public void SbcOracleCoversEveryRegisterAndMemorySource(byte opcode)
    {
        var bus = new TestBus { Memory = { [0] = opcode, [0x4000] = 0x01 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0x00;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x01;
        cpu.Registers.H = cpu.Registers.L = 0x01;
        cpu.Registers.HL = 0x4000;
        var value = opcode switch
        {
            0x9C => cpu.Registers.H,
            0x9D => cpu.Registers.L,
            0x9F => cpu.Registers.A,
            _ => (byte)0x01
        };
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();

        Assert.Equal((byte)(0 - value - 1), cpu.Registers.A);
        Assert.Equal(SubtractFlags(0x00, value, true), cpu.Registers.F);
    }

    [Theory]
    [InlineData(0xB8)]
    [InlineData(0xB9)]
    [InlineData(0xBA)]
    [InlineData(0xBB)]
    [InlineData(0xBC)]
    [InlineData(0xBD)]
    [InlineData(0xBE)]
    [InlineData(0xBF)]
    public void CpOracleCoversEveryRegisterAndMemorySource(byte opcode)
    {
        var bus = new TestBus { Memory = { [0] = opcode, [0x4000] = 0x01 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0x80;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x01;
        cpu.Registers.H = cpu.Registers.L = 0x01;
        cpu.Registers.HL = 0x4000;
        var value = opcode switch
        {
            0xBC => cpu.Registers.H,
            0xBD => cpu.Registers.L,
            0xBF => cpu.Registers.A,
            _ => (byte)0x01
        };

        cpu.Step();

        Assert.Equal((byte)0x80, cpu.Registers.A);
        Assert.Equal(SubtractFlags(0x80, value, false), cpu.Registers.F);
    }

    [Fact]
    public void LogicalOracleCoversEveryOperationAndSource()
    {
        foreach (var operation in new byte[] { 0xA0, 0xA8, 0xB0 })
        for (var source = 0; source < 8; source++)
        {
            var opcode = (byte)(operation | source);
            var bus = new TestBus { Memory = { [0] = opcode, [0x4000] = 0x3C } };
            var cpu = new Z80Cpu(bus, new InterruptLines());
            cpu.Registers.A = 0xF0;
            cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x3C;
            cpu.Registers.H = cpu.Registers.L = 0x3C;
            cpu.Registers.HL = 0x4000;

            var value = source == 4 ? cpu.Registers.H : source == 5 ? cpu.Registers.L :
                source == 7 ? cpu.Registers.A : (byte)0x3C;
            var result = operation switch
            {
                0xA0 => (byte)(0xF0 & value),
                0xA8 => (byte)(0xF0 ^ value),
                _ => (byte)(0xF0 | value)
            };

            cpu.Step();

            Assert.Equal(result, cpu.Registers.A);
            Assert.Equal(LogicalFlags(result, operation == 0xA0), cpu.Registers.F);
        }
    }

    private static Z80Cpu CreateCpu(byte opcode)
    {
        var bus = new TestBus();
        bus.Memory[0] = opcode;
        return new Z80Cpu(bus, new InterruptLines());
    }

    private static byte AddFlags(byte left, byte right, bool carry)
    {
        var result = left + right + (carry ? 1 : 0);
        var value = (byte)result;
        var flags = (byte)(Z80Flags.SignZero(value) |
            (value & (Z80Flags.X | Z80Flags.Y)));
        if (((left ^ right ^ value) & 0x10) != 0) flags |= Z80Flags.HalfCarry;
        if (((~(left ^ right) & (left ^ value)) & 0x80) != 0) flags |= Z80Flags.ParityOverflow;
        if (result > byte.MaxValue) flags |= Z80Flags.Carry;
        return flags;
    }

    private static byte SubtractFlags(byte left, byte right, bool carry)
    {
        var result = left - right - (carry ? 1 : 0);
        var value = (byte)result;
        var flags = (byte)(Z80Flags.SignZero(value) | Z80Flags.AddSubtract |
            (value & (Z80Flags.X | Z80Flags.Y)));
        if (((left ^ right ^ value) & 0x10) != 0) flags |= Z80Flags.HalfCarry;
        if ((((left ^ right) & (left ^ value)) & 0x80) != 0) flags |= Z80Flags.ParityOverflow;
        if (result < 0) flags |= Z80Flags.Carry;
        return flags;
    }

    private static byte LogicalFlags(byte value, bool halfCarry) =>
        (byte)(Z80Flags.SignZero(value) | Z80Flags.Parity(value) |
            (value & (Z80Flags.X | Z80Flags.Y)) | (halfCarry ? Z80Flags.HalfCarry : 0));

    private static byte IncrementFlags(byte value, bool carry)
    {
        var result = (byte)(value + 1);
        var flags = (byte)(Z80Flags.SignZero(result) | (result & (Z80Flags.X | Z80Flags.Y)) |
            (carry ? Z80Flags.Carry : 0));
        if ((value & 0x0F) == 0x0F) flags |= Z80Flags.HalfCarry;
        if (value == 0x7F) flags |= Z80Flags.ParityOverflow;
        return flags;
    }

    private static byte DecrementFlags(byte value, bool carry)
    {
        var result = (byte)(value - 1);
        var flags = (byte)(Z80Flags.SignZero(result) | Z80Flags.AddSubtract |
            (result & (Z80Flags.X | Z80Flags.Y)) | (carry ? Z80Flags.Carry : 0));
        if ((value & 0x0F) == 0) flags |= Z80Flags.HalfCarry;
        if (value == 0x80) flags |= Z80Flags.ParityOverflow;
        return flags;
    }

    private static (byte A, byte F) AdjustDecimal(byte old, bool subtract, bool halfCarry, bool carry)
    {
        var correction = 0;
        var resultCarry = carry;
        if (!subtract)
        {
            if (halfCarry || (old & 0x0F) > 9) correction |= 0x06;
            if (carry || old > 0x99)
            {
                correction |= 0x60;
                resultCarry = true;
            }
        }
        else
        {
            if (halfCarry) correction |= 0x06;
            if (carry) correction |= 0x60;
        }

        var result = subtract ? (byte)(old - correction) : (byte)(old + correction);
        var flags = (byte)((subtract ? Z80Flags.AddSubtract : 0) | Z80Flags.SignZero(result) |
            Z80Flags.Parity(result) | (result & (Z80Flags.X | Z80Flags.Y)) |
            (resultCarry ? Z80Flags.Carry : 0));
        if (((old ^ result) & 0x10) != 0) flags |= Z80Flags.HalfCarry;
        return (result, flags);
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

using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80IndexedValidationMatrixTests
{
    [Fact]
    public void IndexedLoadAndAbsoluteFamiliesUseTheSelectedIndex()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        {
            var (load, _) = CreateCpu(prefix, 0x21, 0x34, 0x12);
            Assert.Equal(14, load.Step());
            Assert.Equal((ushort)0x1234, GetIndex(load, prefix));
            Assert.Equal((ushort)4, load.Registers.PC);

            var (store, storeBus) = CreateCpu(prefix, 0x22, 0x00, 0x40);
            SetIndex(store, prefix, 0xA55A);
            Assert.Equal(20, store.Step());
            Assert.Equal((byte)0x5A, storeBus.Memory[0x4000]);
            Assert.Equal((byte)0xA5, storeBus.Memory[0x4001]);

            var (fromMemory, memoryBus) = CreateCpu(prefix, 0x2A, 0x00, 0x40);
            memoryBus.Memory[0x4000] = 0x5A;
            memoryBus.Memory[0x4001] = 0xA5;
            Assert.Equal(20, fromMemory.Step());
            Assert.Equal((ushort)0xA55A, GetIndex(fromMemory, prefix));
        }
    }

    [Fact]
    public void IndexHalvesSupportImmediateTransferIncrementDecrementAndAlu()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        {
            var (cpu, _) = CreateCpu(prefix, 0x26, 0x7F, prefix, 0x2E, 0x81,
                prefix, 0x44, prefix, 0x24, prefix, 0x2D, prefix, 0x84, prefix, 0xAD);

            Assert.Equal(11, cpu.Step());
            Assert.Equal(11, cpu.Step());
            Assert.Equal(8, cpu.Step());
            Assert.Equal((byte)0x7F, cpu.Registers.B);
            Assert.Equal(8, cpu.Step());
            Assert.Equal(8, cpu.Step());
            Assert.Equal((ushort)0x8080, GetIndex(cpu, prefix));
            Assert.Equal(8, cpu.Step());
            Assert.Equal((byte)0x80, cpu.Registers.A);
            Assert.Equal(8, cpu.Step());
            Assert.Equal((byte)0x00, cpu.Registers.A);
            Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        }
    }

    [Fact]
    public void IndexedMemoryUsesNegativeZeroAndPositiveDisplacements()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        {
            var (cpu, bus) = CreateCpu(prefix, 0x36, 0xFF, 0x10,
                prefix, 0x36, 0x00, 0x20,
                prefix, 0x36, 0x01, 0x30,
                prefix, 0x34, 0xFF,
                prefix, 0x35, 0x00,
                prefix, 0x86, 0x01);
            SetIndex(cpu, prefix, 0x4000);

            Assert.Equal(19, cpu.Step());
            Assert.Equal(19, cpu.Step());
            Assert.Equal(19, cpu.Step());
            Assert.Equal(23, cpu.Step());
            Assert.Equal(23, cpu.Step());
            Assert.Equal(19, cpu.Step());
            Assert.Equal((byte)0x11, bus.Memory[0x3FFF]);
            Assert.Equal((byte)0x1F, bus.Memory[0x4000]);
            Assert.Equal((byte)0x30, bus.Memory[0x4001]);
            Assert.Equal((byte)0x30, cpu.Registers.A);
            Assert.Equal((ushort)21, cpu.Registers.PC);
        }
    }

    [Fact]
    public void IndexedAluUsesIndexHalvesAndMemoryForEveryOperation()
    {
        // Exhaustive over the full byte range for the indexed source (IXH/IXL/(IX+d))
        // and over both carry states, for every operation/source/prefix. The ALU
        // equation itself (add/adc/sub/sbc/and/xor/or/cp, incl. X/Y bits) is already
        // proven exhaustively for register/(HL) sources in
        // Z80EightBitFlagMatrixTests; this test's job is to prove the indexed
        // *source extraction* (high half, low half, displaced memory) feeds that
        // same equation correctly, so a handful of representative accumulators is
        // enough - full accumulator x value cross product here would just re-run
        // the already-proven ALU equation ~25x for no new signal.
        var accumulators = new byte[] { 0x00, 0x3C, 0x7F, 0x80, 0xFF };
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        foreach (var source in new[] { 4, 5, 6 })
        foreach (var operation in Enumerable.Range(0, 8))
        foreach (var accumulator in accumulators)
        foreach (var carry in new[] { false, true })
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var opcode = (byte)(0x80 | (operation << 3) | source);
            var program = source == 6 ? new byte[] { prefix, opcode, 0xFF } : new byte[] { prefix, opcode };
            var (cpu, bus) = CreateCpu(program);
            switch (source)
            {
                case 4: SetIndex(cpu, prefix, (ushort)(value << 8)); break;
                case 5: SetIndex(cpu, prefix, (ushort)(0x8100 | value)); break;
                default:
                    SetIndex(cpu, prefix, 0x810F);
                    bus.Memory[0x810E] = (byte)value;
                    break;
            }
            cpu.Registers.A = accumulator;
            cpu.Registers.F = carry ? Z80Flags.Carry : (byte)0;
            var expected = ExpectedAlu(accumulator, (byte)value, operation, carry);

            var label = $"prefix={prefix:X2} source={source} op={operation} a={accumulator:X2} v={value:X2} c={carry}";
            Assert.True(cpu.Step() == (source == 6 ? 19 : 8), label);
            Assert.True(expected.Accumulator == cpu.Registers.A, label);
            Assert.True(expected.Flags == cpu.Registers.F, label);
            Assert.True((source == 6 ? (ushort)3 : (ushort)2) == cpu.Registers.PC, label);
        }
    }

    [Fact]
    public void IndexedAddUsesEveryRightPairAndIndependentFlags()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        foreach (var (opcode, right) in new (byte Opcode, ushort Right)[] { (0x09, 0x0001), (0x19, 0x0FFF), (0x29, 0), (0x39, 0xFFFF) })
        {
            var (cpu, _) = CreateCpu(prefix, opcode);
            SetIndex(cpu, prefix, 0x0FFF);
            cpu.Registers.BC = right;
            cpu.Registers.DE = right;
            cpu.Registers.SP = right;
            var actualRight = opcode == 0x29 ? (ushort)0x0FFF : right;
            cpu.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow;

            Assert.Equal(15, cpu.Step());
            Assert.Equal((ushort)(0x0FFF + actualRight), GetIndex(cpu, prefix));
            Assert.Equal(AddIndexFlags(0x0FFF, actualRight, Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow), cpu.Registers.F);
            Assert.Equal((ushort)2, cpu.Registers.PC);
        }
    }

    [Fact]
    public void IndexedControlAndStackFamiliesPreserveTheSelectedIndex()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        {
            var (jump, _) = CreateCpu(prefix, 0xE9);
            SetIndex(jump, prefix, 0x4567);
            Assert.Equal(8, jump.Step());
            Assert.Equal((ushort)0x4567, jump.Registers.PC);

            var (push, pushBus) = CreateCpu(prefix, 0xE5);
            push.Registers.SP = 0x4000;
            SetIndex(push, prefix, 0xA55A);
            Assert.Equal(15, push.Step());
            Assert.Equal((ushort)0x3FFE, push.Registers.SP);
            Assert.Equal((byte)0x5A, pushBus.Memory[0x3FFE]);
            Assert.Equal((byte)0xA5, pushBus.Memory[0x3FFF]);

            var (pop, popBus) = CreateCpu(prefix, 0xE1);
            pop.Registers.SP = 0x4000;
            popBus.Memory[0x4000] = 0x5A;
            popBus.Memory[0x4001] = 0xA5;
            Assert.Equal(14, pop.Step());
            Assert.Equal((ushort)0xA55A, GetIndex(pop, prefix));

            var (exchange, exchangeBus) = CreateCpu(prefix, 0xE3);
            exchange.Registers.SP = 0x4000;
            SetIndex(exchange, prefix, 0xA55A);
            exchangeBus.Memory[0x4000] = 0x34;
            exchangeBus.Memory[0x4001] = 0x12;
            Assert.Equal(23, exchange.Step());
            Assert.Equal((ushort)0x1234, GetIndex(exchange, prefix));
            Assert.Equal((byte)0x5A, exchangeBus.Memory[0x4000]);

            var (loadSp, _) = CreateCpu(prefix, 0xF9);
            SetIndex(loadSp, prefix, 0xA55A);
            Assert.Equal(10, loadSp.Step());
            Assert.Equal((ushort)0xA55A, loadSp.Registers.SP);
        }
    }

    [Fact]
    public void IndexedCbRotateResAndSetWriteMemoryAndDestinationRegister()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        {
            var (rotate, rotateBus) = CreateCpu(prefix, 0xCB, 0xFF, 0x00);
            SetIndex(rotate, prefix, 0x4000);
            rotateBus.Memory[0x3FFF] = 0x81;
            Assert.Equal(23, rotate.Step());
            Assert.Equal((byte)0x03, rotateBus.Memory[0x3FFF]);
            Assert.Equal((byte)0x03, rotate.Registers.B);
            Assert.Equal((byte)(Z80Flags.ParityOverflow | Z80Flags.Carry), rotate.Registers.F);

            var (res, resBus) = CreateCpu(prefix, 0xCB, 0x00, 0x80);
            SetIndex(res, prefix, 0x4000);
            resBus.Memory[0x4000] = 0x01;
            Assert.Equal(23, res.Step());
            Assert.Equal((byte)0x00, resBus.Memory[0x4000]);
            Assert.Equal((byte)0x00, res.Registers.B);

            var (set, setBus) = CreateCpu(prefix, 0xCB, 0x01, 0xC1);
            SetIndex(set, prefix, 0x4000);
            setBus.Memory[0x4001] = 0x00;
            Assert.Equal(23, set.Step());
            Assert.Equal((byte)0x01, setBus.Memory[0x4001]);
            Assert.Equal((byte)0x01, set.Registers.C);
        }
    }

    [Theory]
    [InlineData(0xDD)]
    [InlineData(0xFD)]
    public void IndexedBitUsesEffectiveAddressHighByteForUndocumentedFlags(byte prefix)
    {
        var (cpu, bus) = CreateCpu(prefix, 0xCB, 0x00, 0x46);
        SetIndex(cpu, prefix, 0xA800);
        bus.Memory[0xA800] = 0x00;
        cpu.Registers.F = Z80Flags.Carry;

        cpu.Step();

        Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.HalfCarry | Z80Flags.X | Z80Flags.Y), cpu.Registers.F);
    }

    [Fact]
    public void IndexedBitMatchesIndependentFlagsForEveryAddressHighByteAndBit()
    {
        foreach (var prefix in new byte[] { 0xDD, 0xFD })
        for (var highByte = 0; highByte <= byte.MaxValue; highByte++)
        for (var bit = 0; bit < 8; bit++)
        foreach (var set in new[] { false, true })
        {
            var (cpu, bus) = CreateCpu(prefix, 0xCB, 0x00, (byte)(0x46 | (bit << 3)));
            SetIndex(cpu, prefix, (ushort)((highByte << 8) | 0x80));
            bus.Memory[GetIndex(cpu, prefix)] = (byte)(set ? 1 << bit : 0);
            cpu.Registers.F = Z80Flags.Carry;

            cpu.Step();

            var expected = (byte)(Z80Flags.Carry | Z80Flags.HalfCarry |
                (bit == 7 && set ? Z80Flags.Sign : 0) |
                (set ? 0 : Z80Flags.Zero | Z80Flags.ParityOverflow) |
                (highByte & (Z80Flags.X | Z80Flags.Y)));
            Assert.Equal(expected, cpu.Registers.F);
        }
    }

    [Theory]
    [InlineData(0xDD, 0x1000, 0x1800)]
    [InlineData(0xDD, 0x0000, 0x0800)]
    [InlineData(0xDD, 0x0000, 0x2000)]
    [InlineData(0xDD, 0xFFFF, 0x0001)]
    [InlineData(0xFD, 0x1000, 0x1800)]
    [InlineData(0xFD, 0x0000, 0x0800)]
    [InlineData(0xFD, 0x0000, 0x2000)]
    [InlineData(0xFD, 0xFFFF, 0x0001)]
    public void IndexedAddUsesResultHighByteForUndocumentedFlags(byte prefix, ushort left, ushort right)
    {
        var (cpu, _) = CreateCpu(prefix, 0x09);
        SetIndex(cpu, prefix, left);
        cpu.Registers.BC = right;
        cpu.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow;

        Assert.Equal(15, cpu.Step());
        Assert.Equal((ushort)(left + right), GetIndex(cpu, prefix));
        Assert.Equal(AddIndexFlags(left, right, (byte)(Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow)),
            cpu.Registers.F);
    }

    [Theory]
    [InlineData(0xDD, 0x24, 0x7F)]
    [InlineData(0xDD, 0x2C, 0x0F)]
    [InlineData(0xFD, 0x24, 0xFF)]
    [InlineData(0xFD, 0x2C, 0x80)]
    [InlineData(0xDD, 0x25, 0x80)]
    [InlineData(0xDD, 0x2D, 0x00)]
    [InlineData(0xFD, 0x25, 0x01)]
    [InlineData(0xFD, 0x2D, 0x7F)]
    public void IndexedHalfIncDecUsesIndependentUndocumentedFlags(byte prefix, byte opcode, byte value)
    {
        var (cpu, _) = CreateCpu(prefix, opcode);
        var highHalf = (opcode & 0x38) == 0x20;
        SetIndex(cpu, prefix, highHalf ? (ushort)(value << 8) : value);
        cpu.Registers.F = Z80Flags.Carry;

        Assert.Equal(8, cpu.Step());
        var result = opcode is 0x25 or 0x2D ? (byte)(value - 1) : (byte)(value + 1);
        var expectedFlags = opcode is 0x25 or 0x2D
            ? DecrementFlags(value, true)
            : IncrementFlags(value, true);
        Assert.Equal(highHalf ? (ushort)(result << 8) : result, GetIndex(cpu, prefix));
        Assert.Equal(expectedFlags, cpu.Registers.F);
    }

    [Theory]
    [InlineData(0xDD, 0x80, 0xFF, 0xFE)]
    [InlineData(0xDD, 0x88, 0xFF, 0xFD)]
    [InlineData(0xDD, 0xC0, 0x00, 0x01)]
    [InlineData(0xDD, 0xC8, 0x00, 0x02)]
    [InlineData(0xFD, 0x80, 0xFF, 0xFE)]
    [InlineData(0xFD, 0x88, 0xFF, 0xFD)]
    [InlineData(0xFD, 0xC0, 0x00, 0x01)]
    [InlineData(0xFD, 0xC8, 0x00, 0x02)]
    public void IndexedCbResAndSetPreserveAllFlags(byte prefix, byte opcode, byte input, byte expected)
    {
        var (cpu, bus) = CreateCpu(prefix, 0xCB, 0x00, opcode);
        SetIndex(cpu, prefix, 0x4000);
        bus.Memory[0x4000] = input;
        cpu.Registers.F = 0xFF;

        Assert.Equal(23, cpu.Step());
        Assert.Equal(expected, bus.Memory[0x4000]);
        Assert.Equal(expected, cpu.Registers.B);
        Assert.Equal((byte)0xFF, cpu.Registers.F);
    }

    [Theory]
    [InlineData(0xDD, 0x00)]
    [InlineData(0xFD, 0x00)]
    [InlineData(0xDD, 0x07)]
    [InlineData(0xFD, 0x07)]
    [InlineData(0xDD, 0x08)]
    [InlineData(0xFD, 0x08)]
    [InlineData(0xDD, 0x2F)]
    [InlineData(0xFD, 0x2F)]
    public void PrefixesBeforeUnindexedInstructionsPreserveResultAndTiming(byte prefix, byte opcode)
    {
        var (cpu, _) = CreateCpu(prefix, opcode);
        cpu.Registers.A = 0x81;
        cpu.Registers.AF = 0x8100;
        cpu.Registers.AlternateAF = 0x8100;

        Assert.Equal(8, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal(opcode switch
        {
            0x07 => (byte)0x03,
            0x2F => (byte)0x7E,
            _ => (byte)0x81
        }, cpu.Registers.A);
    }

    private static byte AddIndexFlags(ushort left, ushort right, byte preserved)
    {
        var result = left + right;
        var flags = (byte)(preserved & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow));
        flags |= (byte)((result >> 8) & (Z80Flags.X | Z80Flags.Y));
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        if (result > ushort.MaxValue) flags |= Z80Flags.Carry;
        return flags;
    }

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

    private static (byte Accumulator, byte Flags) ExpectedAlu(byte accumulator, byte value, int operation, bool carry)
    {
        var carryValue = carry ? 1 : 0;
        if (operation is 0 or 1)
        {
            var result = accumulator + value + (operation == 1 ? carryValue : 0);
            var output = (byte)result;
            var flags = (byte)(Z80Flags.SignZero(output) | (output & (Z80Flags.X | Z80Flags.Y)));
            if (((accumulator ^ value ^ output) & 0x10) != 0) flags |= Z80Flags.HalfCarry;
            if (((~(accumulator ^ value) & (accumulator ^ output)) & 0x80) != 0) flags |= Z80Flags.ParityOverflow;
            if (result > byte.MaxValue) flags |= Z80Flags.Carry;
            return (output, flags);
        }

        if (operation is 2 or 3 or 7)
        {
            var result = accumulator - value - (operation == 3 ? carryValue : 0);
            var output = (byte)result;
            var flags = (byte)(Z80Flags.SignZero(output) | Z80Flags.AddSubtract | (output & (Z80Flags.X | Z80Flags.Y)));
            if (((accumulator ^ value ^ output) & 0x10) != 0) flags |= Z80Flags.HalfCarry;
            if ((((accumulator ^ value) & (accumulator ^ output)) & 0x80) != 0) flags |= Z80Flags.ParityOverflow;
            if (result < 0) flags |= Z80Flags.Carry;
            return (operation == 7 ? accumulator : output, flags);
        }

        var logical = operation switch { 4 => (byte)(accumulator & value), 5 => (byte)(accumulator ^ value), _ => (byte)(accumulator | value) };
        var logicalFlags = (byte)(Z80Flags.SignZero(logical) | Z80Flags.Parity(logical) | (logical & (Z80Flags.X | Z80Flags.Y)));
        if (operation == 4) logicalFlags |= Z80Flags.HalfCarry;
        return (logical, logicalFlags);
    }

    private static ushort GetIndex(Z80Cpu cpu, byte prefix) => prefix == 0xDD ? cpu.Registers.IX : cpu.Registers.IY;
    private static void SetIndex(Z80Cpu cpu, byte prefix, ushort value)
    {
        if (prefix == 0xDD) cpu.Registers.IX = value;
        else cpu.Registers.IY = value;
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

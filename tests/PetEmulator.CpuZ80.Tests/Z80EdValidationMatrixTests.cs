using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80EdValidationMatrixTests
{
    [Fact]
    public void NegAliasesMatchIndependentSubtractionFlags()
    {
        foreach (var opcode in new byte[] { 0x44, 0x4C, 0x54, 0x5C, 0x64, 0x6C, 0x74, 0x7C })
        foreach (var value in new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFF })
        {
            var (cpu, _) = CreateCpu(opcode);
            cpu.Registers.A = value;

            Assert.Equal(8, cpu.Step());
            Assert.Equal((ushort)2, cpu.Registers.PC);
            Assert.Equal((byte)(0 - value), cpu.Registers.A);
            Assert.Equal(NegFlags(value), cpu.Registers.F);
        }
    }

    [Fact]
    public void ImAliasesSelectTheDocumentedModeAndTiming()
    {
        foreach (var (opcode, mode) in new (byte Opcode, byte Mode)[]
        {
            (0x46, 0), (0x4E, 0), (0x66, 0), (0x6E, 0),
            (0x56, 1), (0x76, 1), (0x5E, 2), (0x7E, 2)
        })
        {
            var (cpu, _) = CreateCpu(opcode);

            Assert.Equal(8, cpu.Step());
            Assert.Equal(mode, cpu.InterruptMode);
            Assert.Equal((ushort)2, cpu.Registers.PC);
        }
    }

    [Fact]
    public void RetnAliasesRestorePcAndIff1WithDocumentedTiming()
    {
        foreach (var opcode in new byte[] { 0x45, 0x4D, 0x55, 0x5D, 0x65, 0x6D, 0x75, 0x7D })
        {
            var (cpu, bus) = CreateCpu(opcode);
            bus.Memory[2] = 0xFB;
            cpu.Registers.PC = 2;
            cpu.Step();
            cpu.Registers.PC = 0;
            cpu.Registers.SP = 0x4000;
            bus.Memory[0x4000] = 0x5A;
            bus.Memory[0x4001] = 0xA5;

            Assert.Equal(14, cpu.Step());
            Assert.Equal((ushort)0xA55A, cpu.Registers.PC);
            Assert.Equal((ushort)0x4002, cpu.Registers.SP);
            Assert.True(cpu.Iff1);
        }
    }

    [Fact]
    public void AdcAndSbcHlCoverEveryPairAndBoundaryTiming()
    {
        foreach (var opcode in new byte[] { 0x4A, 0x5A, 0x6A, 0x7A, 0x42, 0x52, 0x62, 0x72 })
        {
            var (cpu, _) = CreateCpu(opcode);
            var right = opcode switch { 0x4A or 0x42 => (ushort)0x0001, 0x5A or 0x52 => (ushort)0x8000, 0x6A or 0x62 => (ushort)0x7FFF, _ => (ushort)0xFFFF };
            cpu.Registers.HL = opcode < 0x50 ? (ushort)0x7FFF : (ushort)0x8000;
            SetPair(cpu.Registers, (opcode >> 4) & 3, right);
            cpu.Registers.F = Z80Flags.Carry;
            var subtract = (opcode & 0x0F) == 2;
            var left = cpu.Registers.HL;
            var expected = subtract ? left - right - 1 : left + right + 1;

            Assert.Equal(15, cpu.Step());
            Assert.Equal((ushort)expected, cpu.Registers.HL);
            Assert.Equal((ushort)2, cpu.Registers.PC);
            Assert.Equal(AddWithCarryFlags(left, right, true, subtract), cpu.Registers.F);
        }
    }

    [Fact]
    public void InAndOutRegisterVariantsUseBcAndHaveExpectedFlags()
    {
        foreach (var opcode in new byte[] { 0x40, 0x48, 0x50, 0x58, 0x60, 0x68, 0x70, 0x78 })
        {
            var (cpu, bus) = CreateCpu(opcode);
            cpu.Registers.BC = 0xA012;
            cpu.Registers.F = Z80Flags.Carry;
            bus.PortValue = 0x85;

            Assert.Equal(12, cpu.Step());
            Assert.Equal((ushort)0xA012, bus.LastReadPort);
            Assert.Equal((ushort)2, cpu.Registers.PC);
            Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.Sign), cpu.Registers.F);
            if (opcode != 0x70) Assert.Equal((byte)0x85, GetRegister(cpu.Registers, (opcode >> 3) & 7));
        }

        foreach (var opcode in new byte[] { 0x41, 0x49, 0x51, 0x59, 0x61, 0x69, 0x71, 0x79 })
        {
            var (cpu, bus) = CreateCpu(opcode);
            cpu.Registers.BC = 0xA012;
            SetRegister(cpu.Registers, (opcode >> 3) & 7, 0x5A);
            var expectedPort = cpu.Registers.BC;

            Assert.Equal(12, cpu.Step());
            Assert.Equal(expectedPort, bus.LastWrittenPort);
            Assert.Equal(opcode == 0x71 ? (byte)0 : (byte)0x5A, bus.LastWrittenValue);
            Assert.Equal((ushort)2, cpu.Registers.PC);
        }
    }

    [Theory]
    [InlineData(0x40)]
    [InlineData(0x48)]
    [InlineData(0x50)]
    [InlineData(0x58)]
    [InlineData(0x60)]
    [InlineData(0x68)]
    [InlineData(0x70)]
    [InlineData(0x78)]
    public void InRegisterVariantsExposeUndocumentedResultBits(byte opcode)
    {
        var (cpu, bus) = CreateCpu(opcode);
        cpu.Registers.BC = 0xA012;
        cpu.Registers.F = Z80Flags.Carry;
        bus.PortValue = 0x28;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.ParityOverflow |
            Z80Flags.X | Z80Flags.Y), cpu.Registers.F);
    }

    [Fact]
    public void InRegisterVariantsMatchIndependentFlagsForEveryInput()
    {
        foreach (var opcode in new byte[] { 0x40, 0x48, 0x50, 0x58, 0x60, 0x68, 0x70, 0x78 })
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var (cpu, bus) = CreateCpu(opcode);
            cpu.Registers.BC = 0xA012;
            cpu.Registers.F = Z80Flags.Carry;
            bus.PortValue = (byte)value;

            Assert.Equal(12, cpu.Step());
            Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.SignZero((byte)value) |
                Z80Flags.Parity((byte)value) | (value & (Z80Flags.X | Z80Flags.Y))), cpu.Registers.F);
            if (opcode != 0x70)
                Assert.Equal((byte)value, GetRegister(cpu.Registers, (opcode >> 3) & 7));
        }
    }

    [Fact]
    public void EdRegisterTransfersAndDecimalRotationsHaveExpectedResults()
    {
        var (ldCpu, _) = CreateCpu(0x47);
        ldCpu.Registers.A = 0xA8;
        Assert.Equal(9, ldCpu.Step());
        Assert.Equal((byte)0xA8, ldCpu.Registers.I);
        Assert.Equal((ushort)2, ldCpu.Registers.PC);

        var (refreshCpu, _) = CreateCpu(0x4F);
        refreshCpu.Registers.A = 0x80;
        Assert.Equal(9, refreshCpu.Step());
        Assert.Equal((byte)0x80, refreshCpu.Registers.R);

        var (fromICpu, _) = CreateCpu(0x57);
        fromICpu.Registers.I = 0xA8;
        fromICpu.Registers.F = Z80Flags.Carry;
        Assert.Equal(9, fromICpu.Step());
        Assert.Equal((byte)0xA8, fromICpu.Registers.A);
        Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.Sign | Z80Flags.X | Z80Flags.Y), fromICpu.Registers.F);

        var (fromRCpu, _) = CreateCpu(0x5F);
        fromRCpu.Registers.R = 0xA8;
        Assert.Equal(9, fromRCpu.Step());
        Assert.Equal((byte)0xAA, fromRCpu.Registers.A);
        Assert.Equal((ushort)2, fromRCpu.Registers.PC);
        Assert.Equal((byte)(Z80Flags.Sign | Z80Flags.X | Z80Flags.Y), fromRCpu.Registers.F);

        var (rrdCpu, rrdBus) = CreateCpu(0x67);
        rrdCpu.Registers.HL = 0x4000;
        rrdCpu.Registers.A = 0x12;
        rrdBus.Memory[0x4000] = 0x34;
        Assert.Equal(18, rrdCpu.Step());
        Assert.Equal((byte)0x14, rrdCpu.Registers.A);
        Assert.Equal((byte)0x23, rrdBus.Memory[0x4000]);
        Assert.Equal(Z80Flags.ParityOverflow, rrdCpu.Registers.F);

        var (rldCpu, rldBus) = CreateCpu(0x6F);
        rldCpu.Registers.HL = 0x4000;
        rldCpu.Registers.A = 0x12;
        rldBus.Memory[0x4000] = 0x34;
        Assert.Equal(18, rldCpu.Step());
        Assert.Equal((byte)0x13, rldCpu.Registers.A);
        Assert.Equal((byte)0x42, rldBus.Memory[0x4000]);
        Assert.Equal((byte)0, rldCpu.Registers.F);
    }

    [Fact]
    public void PairLoadsStoresAndReturnsHaveDocumentedPcAndTiming()
    {
        foreach (var opcode in new byte[] { 0x43, 0x53, 0x63, 0x73 })
        {
            var (cpu, bus) = CreateCpu(opcode, 0x00, 0x40);
            SetPair(cpu.Registers, (opcode >> 4) & 3, 0xA55A);
            Assert.Equal(20, cpu.Step());
            Assert.Equal((byte)0x5A, bus.Memory[0x4000]);
            Assert.Equal((byte)0xA5, bus.Memory[0x4001]);
            Assert.Equal((ushort)4, cpu.Registers.PC);
        }

        foreach (var opcode in new byte[] { 0x4B, 0x5B, 0x6B, 0x7B })
        {
            var (cpu, bus) = CreateCpu(opcode, 0x00, 0x40);
            bus.Memory[0x4000] = 0x5A;
            bus.Memory[0x4001] = 0xA5;
            Assert.Equal(20, cpu.Step());
            Assert.Equal((ushort)0xA55A, GetPair(cpu.Registers, (opcode >> 4) & 3));
            Assert.Equal((ushort)4, cpu.Registers.PC);
        }

        foreach (var opcode in new byte[] { 0x45, 0x4D })
        {
            var (cpu, bus) = CreateCpu(opcode);
            cpu.Registers.SP = 0x4000;
            bus.Memory[0x4000] = 0x34;
            bus.Memory[0x4001] = 0x12;
            Assert.Equal(14, cpu.Step());
            Assert.Equal((ushort)0x1234, cpu.Registers.PC);
        }
    }

    [Fact]
    public void BlockFamiliesCoverDirectionRepeatPcAndTiming()
    {
        foreach (var opcode in new byte[] { 0xA0, 0xA8, 0xB0, 0xB8 })
        {
            var (cpu, bus) = CreateCpu(opcode);
            var increment = opcode is 0xA0 or 0xB0;
            var repeat = opcode is 0xB0 or 0xB8;
            cpu.Registers.HL = increment ? (ushort)0x4000 : (ushort)0x4001;
            cpu.Registers.DE = increment ? (ushort)0x4100 : (ushort)0x4101;
            cpu.Registers.BC = repeat ? (ushort)2 : (ushort)1;
            bus.Memory[cpu.Registers.HL] = 0x5A;
            Assert.Equal(repeat ? 21 : 16, cpu.Step());
            Assert.Equal((byte)0x5A, bus.Memory[increment ? 0x4100 : 0x4101]);
            Assert.Equal(repeat ? (ushort)0 : (ushort)2, cpu.Registers.PC);
            Assert.Equal(repeat ? (byte)(Z80Flags.X | Z80Flags.ParityOverflow) : Z80Flags.X, cpu.Registers.F);
        }

        foreach (var opcode in new byte[] { 0xA1, 0xA9, 0xB1, 0xB9 })
        {
            var (cpu, bus) = CreateCpu(opcode);
            var repeat = opcode is 0xB1 or 0xB9;
            cpu.Registers.A = 0x5A;
            cpu.Registers.HL = 0x4000;
            cpu.Registers.BC = repeat ? (ushort)2 : (ushort)1;
            bus.Memory[0x4000] = repeat ? (byte)0 : (byte)0x5A;
            Assert.Equal(repeat ? 21 : 16, cpu.Step());
            Assert.Equal(repeat ? (ushort)0 : (ushort)2, cpu.Registers.PC);
            Assert.Equal(repeat
                ? (byte)(Z80Flags.AddSubtract | Z80Flags.ParityOverflow | Z80Flags.X)
                : (byte)(Z80Flags.AddSubtract | Z80Flags.Zero), cpu.Registers.F);
        }
    }

    [Fact]
    public void BlockIoFamiliesCoverDirectionRepeatAndPorts()
    {
        foreach (var opcode in new byte[] { 0xA2, 0xAA, 0xB2, 0xBA, 0xA3, 0xAB, 0xB3, 0xBB })
        {
            var (cpu, bus) = CreateCpu(opcode);
            var increment = opcode is 0xA2 or 0xB2 or 0xA3 or 0xB3;
            var repeat = opcode is 0xB2 or 0xBA or 0xB3 or 0xBB;
            var input = (opcode & 1) == 0;
            cpu.Registers.BC = repeat ? (ushort)0x0201 : (ushort)0x0101;
            cpu.Registers.HL = increment ? (ushort)0x4000 : (ushort)0x4001;
            bus.PortValue = 0x5A;
            bus.Memory[cpu.Registers.HL] = 0x5A;

            Assert.Equal(repeat ? 21 : 16, cpu.Step());
            Assert.Equal(repeat ? (ushort)0 : (ushort)2, cpu.Registers.PC);
            Assert.Equal((byte)(repeat ? 1 : 0), cpu.Registers.B);
            if (input) Assert.Equal((byte)0x5A, bus.Memory[increment ? 0x4000 : 0x4001]);
            Assert.Equal((ushort)(repeat ? 0x0201 : 0x0101), input ? bus.LastReadPort : bus.LastWrittenPort);
        }
    }

    [Fact]
    public void UndefinedEdOpcodeIsAnEightTCycleNoOp()
    {
        var (cpu, _) = CreateCpu(0x00);
        cpu.Registers.AF = 0xA5C3;
        cpu.Registers.BC = 0x1234;
        cpu.Registers.HL = 0x5678;

        Assert.Equal(8, cpu.Step());
        Assert.Equal((ushort)2, cpu.Registers.PC);
        Assert.Equal((ushort)0xA5C3, cpu.Registers.AF);
        Assert.Equal((ushort)0x1234, cpu.Registers.BC);
        Assert.Equal((ushort)0x5678, cpu.Registers.HL);
    }

    [Fact]
    public void EveryUndefinedEdOpcodeIsAnEightTCycleNoOp()
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var opcode = (byte)value;
            if (IsDefinedEdOpcode(opcode))
                continue;

            var (cpu, _) = CreateCpu(opcode);
            cpu.Registers.AF = 0xA5C3;
            cpu.Registers.BC = 0x1234;
            cpu.Registers.DE = 0x2345;
            cpu.Registers.HL = 0x5678;
            cpu.Registers.IX = 0x6789;
            cpu.Registers.IY = 0x789A;

            Assert.Equal(8, cpu.Step());
            Assert.Equal((ushort)2, cpu.Registers.PC);
            Assert.Equal((ushort)0xA5C3, cpu.Registers.AF);
            Assert.Equal((ushort)0x1234, cpu.Registers.BC);
            Assert.Equal((ushort)0x2345, cpu.Registers.DE);
            Assert.Equal((ushort)0x5678, cpu.Registers.HL);
            Assert.Equal((ushort)0x6789, cpu.Registers.IX);
            Assert.Equal((ushort)0x789A, cpu.Registers.IY);
        }
    }

    private static bool IsDefinedEdOpcode(byte opcode) =>
        (opcode & 0xC7) is 0x40 or 0x41 or 0x44 or 0x46 ||
        (opcode & 0xCF) is 0x42 or 0x43 or 0x4A or 0x4B ||
        opcode is 0x45 or 0x4D or 0x47 or 0x4F or 0x55 or 0x57 or 0x5D or 0x5F or
            0x65 or 0x67 or 0x6D or 0x6F or 0x75 or 0x7D or
            0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA8 or 0xA9 or 0xAA or 0xAB or
            0xB0 or 0xB1 or 0xB2 or 0xB3 or 0xB8 or 0xB9 or 0xBA or 0xBB;

    private static byte NegFlags(byte value)
    {
        var result = (byte)(0 - value);
        var flags = (byte)(Z80Flags.SignZero(result) | Z80Flags.AddSubtract | (result & (Z80Flags.X | Z80Flags.Y)));
        if ((value & 0x0F) != 0) flags |= Z80Flags.HalfCarry;
        if (value == 0x80) flags |= Z80Flags.ParityOverflow;
        if (value != 0) flags |= Z80Flags.Carry;
        return flags;
    }

    private static byte AddWithCarryFlags(ushort left, ushort right, bool carry, bool subtract)
    {
        var result = subtract ? left - right - (carry ? 1 : 0) : left + right + (carry ? 1 : 0);
        var value = (ushort)result;
        var flags = (byte)((value & 0x8000) != 0 ? Z80Flags.Sign : 0);
        if (value == 0) flags |= Z80Flags.Zero;
        if (subtract) flags |= Z80Flags.AddSubtract;
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        var overflow = subtract ? (left ^ right) & (left ^ result) : ~(left ^ right) & (left ^ result);
        if ((overflow & 0x8000) != 0) flags |= Z80Flags.ParityOverflow;
        if (result < 0 || result > ushort.MaxValue) flags |= Z80Flags.Carry;
        return (byte)(flags | (value >> 8 & (Z80Flags.X | Z80Flags.Y)));
    }

    private static ushort GetPair(Z80Registers registers, int pair) => pair switch { 0 => registers.BC, 1 => registers.DE, 2 => registers.HL, _ => registers.SP };
    private static void SetPair(Z80Registers registers, int pair, ushort value)
    {
        switch (pair) { case 0: registers.BC = value; break; case 1: registers.DE = value; break; case 2: registers.HL = value; break; default: registers.SP = value; break; }
    }

    private static byte GetRegister(Z80Registers registers, int register) => register switch { 0 => registers.B, 1 => registers.C, 2 => registers.D, 3 => registers.E, 4 => registers.H, 5 => registers.L, _ => registers.A };
    private static void SetRegister(Z80Registers registers, int register, byte value)
    {
        switch (register) { case 0: registers.B = value; break; case 1: registers.C = value; break; case 2: registers.D = value; break; case 3: registers.E = value; break; case 4: registers.H = value; break; case 5: registers.L = value; break; case 7: registers.A = value; break; }
    }

    private static (Z80Cpu Cpu, TestBus Bus) CreateCpu(params byte[] edOperands)
    {
        var bus = new TestBus();
        bus.Memory[0] = 0xED;
        Array.Copy(edOperands, 0, bus.Memory, 1, edOperands.Length);
        return (new Z80Cpu(bus, new InterruptLines()), bus);
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte PortValue { get; set; }
        public ushort LastReadPort { get; private set; }
        public ushort LastWrittenPort { get; private set; }
        public byte LastWrittenValue { get; private set; }
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => ReadPort((ushort)port);
        public byte ReadPort(ushort port) { LastReadPort = port; return PortValue; }
        public void WritePort(byte port, byte value) => WritePort((ushort)port, value);
        public void WritePort(ushort port, byte value) { LastWrittenPort = port; LastWrittenValue = value; }
    }
}

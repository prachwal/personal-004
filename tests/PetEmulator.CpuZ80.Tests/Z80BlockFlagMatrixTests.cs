using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80BlockFlagMatrixTests
{
    [Fact]
    public void LdiAndLddFlagsMatchIndependentTransferRules()
    {
        foreach (var opcode in new byte[] { 0xA0, 0xA8 })
        foreach (var accumulator in Enumerable.Range(0, 256).Select(static value => (byte)value))
        foreach (var value in Enumerable.Range(0, 256).Select(static value => (byte)value))
        {
            var cpu = CreateCpu(0xED, opcode);
            cpu.Registers.A = accumulator;
            cpu.Registers.BC = 2;
            cpu.Registers.HL = opcode == 0xA0 ? (ushort)0x4000 : (ushort)0x4001;
            cpu.Registers.DE = opcode == 0xA0 ? (ushort)0x4100 : (ushort)0x4101;
            cpu.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.Carry;
            cpu.Bus.Memory[cpu.Registers.HL] = value;

            cpu.Step();

            var expected = (byte)(Z80Flags.Sign | Z80Flags.Zero | Z80Flags.Carry |
                Z80Flags.ParityOverflow | ((accumulator + value) & (Z80Flags.X | Z80Flags.Y)));
            Assert.Equal(expected, cpu.Registers.F);
        }
    }

    [Fact]
    public void CpiAndCpdFlagsMatchIndependentCompareRules()
    {
        foreach (var opcode in new byte[] { 0xA1, 0xA9 })
        foreach (var accumulator in Enumerable.Range(0, 256).Select(static value => (byte)value))
        foreach (var value in Enumerable.Range(0, 256).Select(static value => (byte)value))
        {
            var cpu = CreateCpu(0xED, opcode);
            cpu.Registers.A = accumulator;
            cpu.Registers.BC = 2;
            cpu.Registers.HL = 0x4000;
            cpu.Registers.F = Z80Flags.Carry;
            cpu.Bus.Memory[0x4000] = value;

            cpu.Step();

            var result = accumulator - value;
            var halfCarry = ((accumulator ^ value ^ result) & 0x10) != 0;
            var adjusted = result - (halfCarry ? 1 : 0);
            var expected = (byte)(Z80Flags.AddSubtract | Z80Flags.Carry |
                Z80Flags.SignZero((byte)result) | (halfCarry ? Z80Flags.HalfCarry : 0) |
                Z80Flags.ParityOverflow | (adjusted & (Z80Flags.X | Z80Flags.Y)));
            Assert.Equal(expected, cpu.Registers.F);
        }
    }

    [Fact]
    public void BlockIoSetsSubtractFromTransferredByteAndRepeats()
    {
        foreach (var opcode in new byte[] { 0xB2, 0xBA, 0xB3, 0xBB })
        foreach (var value in Enumerable.Range(0, 256).Select(static value => (byte)value))
        {
            var cpu = CreateCpu(0xED, opcode);
            cpu.Registers.BC = 0x0201;
            cpu.Registers.HL = opcode is 0xA2 or 0xA3 ? (ushort)0x4000 : (ushort)0x4001;
            cpu.Bus.PortValue = value;
            cpu.Bus.LastPort = 0;
            cpu.Bus.Memory[0x4000] = value;
            cpu.Bus.Memory[0x4001] = value;

            cpu.Step();

            var expectedSubtract = (value & 0x80) != 0 ? Z80Flags.AddSubtract : (byte)0;
            Assert.True(expectedSubtract == (byte)(cpu.Registers.F & Z80Flags.AddSubtract),
                $"opcode={opcode:X2} value={value:X2} expected={expectedSubtract:X2} actual={cpu.Registers.F:X2}");
            Assert.Equal((byte)0x01, cpu.Registers.B);
            Assert.Equal((ushort)0, cpu.Registers.PC);
            Assert.Equal((ushort)0x0201, cpu.Bus.LastPort);
        }
    }

    [Theory]
    [InlineData(0xA2, false)]
    [InlineData(0xAA, false)]
    [InlineData(0xB2, true)]
    [InlineData(0xBA, true)]
    [InlineData(0xA3, false)]
    [InlineData(0xAB, false)]
    [InlineData(0xB3, true)]
    [InlineData(0xBB, true)]
    public void BlockIoVariantsSetUndocumentedBitsFromDecrementedB(byte opcode, bool repeat)
    {
        var cpu = CreateCpu(0xED, opcode);
        cpu.Registers.BC = 0x2901;
        cpu.Registers.HL = opcode is 0xAA or 0xBA or 0xAB or 0xBB ? (ushort)0x4001 : (ushort)0x4000;
        cpu.Bus.PortValue = 0x28;
        cpu.Bus.Memory[0x4000] = 0x28;
        cpu.Bus.Memory[0x4001] = 0x28;

        Assert.Equal(repeat ? 21 : 16, cpu.Step());
        Assert.True((cpu.Registers.F & (Z80Flags.X | Z80Flags.Y)) == (Z80Flags.X | Z80Flags.Y));
    }

    [Fact]
    public void BlockIoUndocumentedBitsMatchIndependentKFlagFormula()
    {
        // Independent oracle for INI/IND/OUTI/OUTD S/Z/H/P-V/N/X/Y, derived from
        // the redcode/Z80 reference core (Sean Young's documented formula):
        // t = io + ((C+1)&0xFF) for INI, io + ((C-1)&0xFF) for IND, or
        // io + L(after HL move) for OUTI/OUTD; H,C = t>255; P/V = parity((t&7)^Bfinal);
        // N = bit 7 of io; S/Z/X/Y = from Bfinal (B after the decrement).
        var bValues = new byte[] { 0x01, 0x02, 0x29, 0x80, 0xFF };
        var boundaryOffsets = new byte[] { 0x00, 0x01, 0x7F, 0xFE, 0xFF };

        foreach (var opcode in new byte[] { 0xA2, 0xAA, 0xA3, 0xAB })
        foreach (var bInitial in bValues)
        foreach (var offset in boundaryOffsets)
        for (var io = 0; io <= byte.MaxValue; io++)
        {
            var cpu = CreateCpu(0xED, opcode);
            var increment = opcode is 0xA2 or 0xA3;
            var isInput = opcode is 0xA2 or 0xAA;
            cpu.Registers.B = bInitial;
            int addend;
            if (isInput)
            {
                cpu.Registers.C = offset;
                cpu.Registers.HL = 0x4000;
                cpu.Bus.PortValue = (byte)io;
                addend = offset + (increment ? 1 : -1);
            }
            else
            {
                var startLow = increment ? (byte)(offset - 1) : (byte)(offset + 1);
                cpu.Registers.HL = (ushort)(0x4000 | startLow);
                cpu.Bus.Memory[cpu.Registers.HL] = (byte)io;
                addend = offset;
            }

            var finalB = (byte)(bInitial - 1);
            var expectedFlags = ExpectedBlockIoFlags((byte)io, finalB, addend);

            cpu.Step();

            var label = $"opcode={opcode:X2} B={bInitial:X2} offset={offset:X2} io={io:X2} " +
                $"expected={expectedFlags:X2} actual={cpu.Registers.F:X2}";
            Assert.True(expectedFlags == cpu.Registers.F, label);
            Assert.True(finalB == cpu.Registers.B, label);
        }
    }

    private static byte ExpectedBlockIoFlags(byte value, byte finalB, int addend)
    {
        var sum = value + (addend & 0xFF);
        var flags = (byte)(Z80Flags.SignZero(finalB) | (finalB & (Z80Flags.X | Z80Flags.Y)) |
            ((value & Z80Flags.Sign) != 0 ? Z80Flags.AddSubtract : 0));
        if (sum > 0xFF) flags |= Z80Flags.HalfCarry | Z80Flags.Carry;
        if (Z80Flags.Parity((byte)((sum & 7) ^ finalB)) != 0) flags |= Z80Flags.ParityOverflow;
        return flags;
    }

    private static TestCpu CreateCpu(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        return new TestCpu(new Z80Cpu(bus, new InterruptLines()), bus);
    }

    private sealed record TestCpu(Z80Cpu Cpu, TestBus Bus)
    {
        public Z80Registers Registers => Cpu.Registers;
        public int Step() => Cpu.Step();
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte PortValue { get; set; }
        public ushort LastPort { get; set; }
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => PortValue;
        public byte ReadPort(ushort port)
        {
            LastPort = port;
            return PortValue;
        }
        public void WritePort(byte port, byte value) { }
        public void WritePort(ushort port, byte value) => LastPort = port;
    }
}

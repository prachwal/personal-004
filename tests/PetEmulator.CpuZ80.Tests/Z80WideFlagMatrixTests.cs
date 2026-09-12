using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80WideFlagMatrixTests
{
    [Fact]
    public void AddHlAndIndexedAddMatchIndependentFlagEquations()
    {
        var values = new ushort[] { 0x0000, 0x0001, 0x7FFF, 0x8000, 0xFFFF };
        foreach (var left in values)
        foreach (var right in values)
        {
            var add = CreateCpu(0x09);
            add.Registers.HL = left;
            add.Registers.BC = right;
            add.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow | Z80Flags.Carry;
            var originalFlags = add.Registers.F;
            add.Step();
            Assert.Equal(Add16Flags(left, right, originalFlags), (byte)(add.Registers.F &
                (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow | Z80Flags.HalfCarry |
                 Z80Flags.X | Z80Flags.Y | Z80Flags.Carry)));

            var indexed = CreateCpu(0xDD, 0x09);
            indexed.Registers.IX = left;
            indexed.Registers.BC = right;
            indexed.Step();
            Assert.Equal((ushort)(left + right), indexed.Registers.IX);
            Assert.Equal(Add16Flags(left, right, 0), (byte)(indexed.Registers.F &
                (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow | Z80Flags.HalfCarry |
                 Z80Flags.X | Z80Flags.Y | Z80Flags.Carry)));
        }
    }

    [Fact]
    public void AdcAndSbcHlMatchIndependentFlagEquations()
    {
        var values = new ushort[] { 0x0000, 0x0001, 0x7FFF, 0x8000, 0xFFFF };
        foreach (var left in values)
        foreach (var right in values)
        foreach (var carry in new[] { false, true })
        {
            foreach (var opcode in new byte[] { 0x4A, 0x42 })
            {
                var cpu = CreateCpu(0xED, opcode);
                cpu.Registers.HL = left;
                cpu.Registers.BC = right;
                cpu.Registers.F = carry ? Z80Flags.Carry : (byte)0;
                cpu.Step();

                var expected = opcode == 0x4A
                    ? Adc16Flags(left, right, carry)
                    : Sbc16Flags(left, right, carry);
                Assert.True(expected == cpu.Registers.F, $"opcode={opcode:X2} left={left:X4} right={right:X4} carry={carry} expected={expected:X2} actual={cpu.Registers.F:X2}");
            }
        }
    }

    [Fact]
    public void AccumulatorRotationsMatchCarryAndPreservedFlags()
    {
        foreach (var value in Enumerable.Range(0, 256).Select(static value => (byte)value))
        foreach (var carry in new[] { false, true })
        foreach (var opcode in new byte[] { 0x07, 0x0F, 0x17, 0x1F })
        {
            var cpu = CreateCpu(opcode);
            cpu.Registers.A = value;
            cpu.Registers.F = (byte)((Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow) |
                (carry ? Z80Flags.Carry : 0));
            cpu.Step();

            var expected = opcode switch
            {
                0x07 => (byte)((value << 1) | (value >> 7)),
                0x0F => (byte)((value >> 1) | (value << 7)),
                0x17 => (byte)((value << 1) | (carry ? 1 : 0)),
                _ => (byte)((value >> 1) | (carry ? 0x80 : 0))
            };
            var expectedCarry = opcode is 0x07 or 0x17 ? (value & 0x80) != 0 : (value & 1) != 0;
            var expectedFlags = (byte)((Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow) |
                (expected & (Z80Flags.X | Z80Flags.Y)) | (expectedCarry ? Z80Flags.Carry : 0));
            Assert.Equal(expected, cpu.Registers.A);
            Assert.True(expectedFlags == cpu.Registers.F, $"opcode={opcode:X2} value={value:X2} carry={carry} expected={expectedFlags:X2} actual={cpu.Registers.F:X2}");
        }
    }

    [Fact]
    public void ScfCcfAndCplMatchIndependentFlagsForEveryAccumulatorAndFlagValue()
    {
        foreach (var opcode in new byte[] { 0x37, 0x3F, 0x2F })
        for (var accumulator = 0; accumulator <= byte.MaxValue; accumulator++)
        for (var originalFlags = 0; originalFlags <= byte.MaxValue; originalFlags++)
        {
            var cpu = CreateCpu(opcode);
            cpu.Registers.A = (byte)accumulator;
            cpu.Registers.F = (byte)originalFlags;
            cpu.Step();

            var expectedAccumulator = opcode == 0x2F ? (byte)~accumulator : (byte)accumulator;
            var expectedFlags = opcode switch
            {
                0x37 => (byte)((originalFlags & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow)) |
                    Z80Flags.Carry | (expectedAccumulator & (Z80Flags.X | Z80Flags.Y))),
                0x3F => (byte)((originalFlags & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow)) |
                    ((originalFlags & Z80Flags.Carry) != 0 ? Z80Flags.HalfCarry : Z80Flags.Carry) |
                    (expectedAccumulator & (Z80Flags.X | Z80Flags.Y))),
                _ => (byte)((originalFlags & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow | Z80Flags.Carry)) |
                    Z80Flags.HalfCarry | Z80Flags.AddSubtract | (expectedAccumulator & (Z80Flags.X | Z80Flags.Y)))
            };

            Assert.Equal(expectedAccumulator, cpu.Registers.A);
            Assert.Equal(expectedFlags, cpu.Registers.F);
        }
    }

    [Fact]
    public void BlockAndImmediateIoFlagsMatchTheirIndependentRules()
    {
        var ldi = CreateCpu(0xED, 0xA0);
        ldi.Registers.A = 0x18;
        ldi.Registers.HL = 0x4000;
        ldi.Registers.DE = 0x4100;
        ldi.Registers.BC = 2;
        ldi.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.Carry;
        ldi.Bus.Memory[0x4000] = 0x07;
        ldi.Step();
        Assert.Equal((byte)(Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.Carry | Z80Flags.X), ldi.Registers.F);

        var cpi = CreateCpu(0xED, 0xA1);
        cpi.Registers.A = 0x10;
        cpi.Registers.HL = 0x4000;
        cpi.Registers.BC = 2;
        cpi.Registers.F = Z80Flags.Carry;
        cpi.Bus.Memory[0x4000] = 0x01;
        cpi.Step();
        Assert.Equal((byte)(Z80Flags.AddSubtract | Z80Flags.HalfCarry |
            Z80Flags.ParityOverflow | Z80Flags.Carry | Z80Flags.X), cpi.Registers.F);

        var input = CreateCpu(0xDB, 0x10);
        input.Registers.F = byte.MaxValue;
        input.Bus.PortValue = 0x00;
        input.Step();
        Assert.Equal(byte.MaxValue, input.Registers.F);
    }

    private static byte Add16Flags(ushort left, ushort right, byte originalFlags)
    {
        var result = left + right;
        var flags = (byte)(originalFlags & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow));
        flags |= (byte)((result >> 8) & (Z80Flags.X | Z80Flags.Y));
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        if (result > ushort.MaxValue) flags |= Z80Flags.Carry;
        return flags;
    }

    private static byte Adc16Flags(ushort left, ushort right, bool carry)
    {
        var result = left + right + (carry ? 1 : 0);
        var value = (ushort)result;
        var flags = (byte)(((value & 0x8000) != 0 ? Z80Flags.Sign : 0) |
            (value == 0 ? Z80Flags.Zero : 0) | (value >> 8 & (Z80Flags.X | Z80Flags.Y)));
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        if (((~(left ^ right) & (left ^ result)) & 0x8000) != 0) flags |= Z80Flags.ParityOverflow;
        if (result > ushort.MaxValue) flags |= Z80Flags.Carry;
        return flags;
    }

    private static byte Sbc16Flags(ushort left, ushort right, bool carry)
    {
        var result = left - right - (carry ? 1 : 0);
        var value = (ushort)result;
        var flags = (byte)(((value & 0x8000) != 0 ? Z80Flags.Sign : 0) |
            (value == 0 ? Z80Flags.Zero : 0) | Z80Flags.AddSubtract |
            (value >> 8 & (Z80Flags.X | Z80Flags.Y)));
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        if ((((left ^ right) & (left ^ result)) & 0x8000) != 0) flags |= Z80Flags.ParityOverflow;
        if (result < 0) flags |= Z80Flags.Carry;
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
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => PortValue;
        public void WritePort(byte port, byte value) { }
    }
}

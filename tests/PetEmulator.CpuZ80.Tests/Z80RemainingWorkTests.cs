using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80RemainingWorkTests
{
    [Fact]
    public void Im0ExecutesAcknowledgedRstWithoutAnExtraStackPush()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = 0xFF };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        EnableInterruptsInMode(cpu, bus, 0);

        Assert.Equal(13, cpu.Step());
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        Assert.Equal((ushort)0x0004, ReadWord(bus.Memory, 0x3FFE));
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Fact]
    public void Im0ExecutesAcknowledgedNopWithoutChangingPcOrStack()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = 0x00 };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        cpu.Registers.PC = 0x1234;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;

        Assert.Equal(6, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Fact]
    public void Im0ExecutesAcknowledgedAbsoluteJumpUsingMemoryOperands()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = 0xC3 };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;
        bus.Memory[0x1234] = 0x78;
        bus.Memory[0x1235] = 0x56;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x07)]
    [InlineData(0x08)]
    [InlineData(0x0F)]
    [InlineData(0x17)]
    [InlineData(0x1F)]
    [InlineData(0x27)]
    [InlineData(0x2F)]
    public void Im0ExecutesAcknowledgedSingleByteOpcodes(byte interruptOpcode)
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = interruptOpcode };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        EnableInterruptsInMode(cpu, bus, 0);
        lines.IntAsserted = true;

        Assert.Equal(6, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Theory]
    [InlineData(0x06, 9)]
    [InlineData(0x0E, 9)]
    [InlineData(0x16, 9)]
    [InlineData(0x1E, 9)]
    [InlineData(0x26, 9)]
    [InlineData(0x2E, 9)]
    [InlineData(0x36, 12)]
    [InlineData(0x3E, 9)]
    public void Im0ExecutesAcknowledgedImmediateLoadOpcodes(byte interruptOpcode, int expectedTStates)
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = interruptOpcode };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.HL = 0x4000;
        EnableInterruptsInMode(cpu, bus, 0);
        bus.Memory[0x0004] = 0xA5;
        lines.IntAsserted = true;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal((ushort)0x0005, cpu.Registers.PC);
        Assert.Equal(1, bus.AcknowledgeCount);
        if (interruptOpcode == 0x36)
            Assert.Equal((byte)0xA5, bus.Memory[0x4000]);
    }

    [Fact]
    public void Im2UsesTheAcknowledgedVectorByte()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = 0x34 };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.I = 0x12;
        bus.Memory[0x1234] = 0x78;
        bus.Memory[0x1235] = 0x56;
        EnableInterruptsInMode(cpu, bus, 2);

        Assert.Equal(19, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.PC);
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Theory]
    [InlineData(0xCB, 0x00, 10, 0x1235)]
    [InlineData(0xED, 0x47, 11, 0x1235)]
    [InlineData(0xDD, 0x21, 16, 0x1237)]
    public void Im0ExecutesPrefixedAcknowledgedOpcodes(byte interruptOpcode, byte operand, int expectedTStates, ushort expectedPc)
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = interruptOpcode };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        bus.Memory[0x1234] = operand;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;
        lines.IntAsserted = true;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal(expectedPc, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Fact]
    public void Im0ExecutesAcknowledgedCallWithoutAnExtraInterruptStackPush()
    {
        var lines = new TestInterruptLines { IntAsserted = true };
        var bus = new TestBus { InterruptOpcode = 0xCD };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        bus.Memory[0x1234] = 0x78;
        bus.Memory[0x1235] = 0x56;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;
        lines.IntAsserted = true;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        Assert.Equal((ushort)0x1236, ReadWord(bus.Memory, 0x3FFE));
    }

    [Fact]
    public void Im0ExecutesAcknowledgedRelativeJumpWithoutChangingTheStack()
    {
        var lines = new TestInterruptLines();
        var bus = new TestBus { InterruptOpcode = 0x18, Memory = { [0x1234] = 0xFE } };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;
        lines.IntAsserted = true;

        Assert.Equal(14, cpu.Step());
        Assert.Equal((ushort)0x1233, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
    }

    [Fact]
    public void Im0ExecutesAcknowledgedReturnUsingTheExistingStack()
    {
        var lines = new TestInterruptLines();
        var bus = new TestBus { InterruptOpcode = 0xC9, Memory = { [0x4000] = 0x5A, [0x4001] = 0xA5 } };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.SP = 0x4000;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;
        lines.IntAsserted = true;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((ushort)0xA55A, cpu.Registers.PC);
        Assert.Equal((ushort)0x4002, cpu.Registers.SP);
    }

    [Fact]
    public void Im0ExecutesAcknowledgedHaltAtTheCurrentProgramCounter()
    {
        var lines = new TestInterruptLines();
        var bus = new TestBus { InterruptOpcode = 0x76 };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Registers.PC = 0x1234;
        EnableInterruptsInMode(cpu, bus, 0);
        cpu.Registers.PC = 0x1234;
        lines.IntAsserted = true;

        Assert.Equal(6, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.PC);
        Assert.True(cpu.Halted);
    }

    [Fact]
    public void InterruptAfterHaltReturnsToTheInstructionFollowingHalt()
    {
        var lines = new TestInterruptLines();
        var bus = new TestBus { Memory = { [4] = 0x76 } };
        var cpu = new Z80Cpu(bus, lines);

        EnableInterruptsInMode(cpu, bus, 1);
        cpu.Step();
        Assert.Equal((ushort)5, cpu.Registers.PC);
        lines.IntAsserted = true;
        cpu.Step();
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
        Assert.Equal((ushort)5, ReadWord(bus.Memory, cpu.Registers.SP));
    }

    [Fact]
    public void RefreshPreservesBitSevenAndCountsPrefixesAndHaltCycles()
    {
        var (cpu, _) = CreateCpu(0xDD, 0x00, 0x76);
        cpu.Registers.R = 0xFE;

        cpu.Step();
        Assert.Equal((byte)0x80, cpu.Registers.R);
        cpu.Step();
        cpu.Step();
        Assert.Equal((byte)0x82, cpu.Registers.R);
    }

    [Fact]
    public void DoubleIndexedBitRefreshesTwiceForTwoPrefixes()
    {
        var (cpu, bus) = CreateCpu(0xDD, 0xCB, 0x00, 0x46);
        cpu.Registers.IX = 0x4000;
        bus.Memory[0x4000] = 0x01;

        cpu.Step();

        Assert.Equal((byte)2, cpu.Registers.R);
    }

    [Fact]
    public void CpirRepeatsAtTheEdPrefixUntilItFindsAMatch()
    {
        var (cpu, bus) = CreateCpu(0xED, 0xB1);
        cpu.Registers.A = 0x22;
        cpu.Registers.HL = 0x4000;
        cpu.Registers.BC = 2;
        bus.Memory[0x4000] = 0x11;
        bus.Memory[0x4001] = 0x22;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0, cpu.Registers.PC);
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)2, cpu.Registers.PC);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
    }

    [Fact]
    public void IndexedAluAndDdcbDestinationRegisterUseTheIndexedValue()
    {
        var (cpu, bus) = CreateCpu(0xDD, 0x21, 0x00, 0x40, 0x3E, 0x01,
            0xDD, 0x86, 0x02, 0xDD, 0xCB, 0x02, 0x00);
        bus.Memory[0x4002] = 0x81;

        cpu.Step();
        cpu.Step();
        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x82, cpu.Registers.A);
        Assert.Equal(23, cpu.Step());
        Assert.Equal((byte)0x03, bus.Memory[0x4002]);
        Assert.Equal((byte)0x03, cpu.Registers.B);
    }

    [Fact]
    public void InImmediatePreservesFlagsAndLdAFromIIncludesUndocumentedBits()
    {
        var (cpu, bus) = CreateCpu(0xDB, 0x10, 0x3E, 0x28, 0xED, 0x47, 0xED, 0x57);
        cpu.Registers.F = Z80Flags.Carry | Z80Flags.AddSubtract;
        bus.PortValue = 0x80;

        cpu.Step();
        Assert.Equal((byte)(Z80Flags.Carry | Z80Flags.AddSubtract), cpu.Registers.F);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.X));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Y));
    }

    [Fact]
    public void ArithmeticFlagFixesCoverIncSbcAndIndexedAdd()
    {
        var (cpu, _) = CreateCpu(0x06, 0x00, 0x04, 0x21, 0x00, 0x80, 0x01, 0x01, 0x00,
            0x37, 0xED, 0x42, 0xDD, 0x21, 0xFF, 0x0F, 0x01, 0x01, 0x00, 0xDD, 0x09);

        cpu.Step();
        cpu.Step();
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.X));
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.ParityOverflow));
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.HalfCarry));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.AddSubtract));
    }

    [Fact]
    public void RrdAndRldSetUndocumentedFlagsFromTheNewAccumulator()
    {
        var (cpu, bus) = CreateCpu(0x21, 0x00, 0x40, 0x3E, 0x00, 0xED, 0x67, 0xED, 0x6F);
        bus.Memory[0x4000] = 0x08;

        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.X));
        cpu.Step();
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.X));
    }

    [Fact]
    public void EdBlockIoFamiliesTransferAndRepeat()
    {
        var (cpu, bus) = CreateCpu(0xED, 0xB2);
        cpu.Registers.BC = 0x0201;
        cpu.Registers.HL = 0x4000;
        bus.PortValue = 0x5A;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((byte)0x5A, bus.Memory[0x4000]);
        Assert.Equal((ushort)0, cpu.Registers.PC);
        Assert.Equal(16, cpu.Step());
        Assert.Equal((byte)0x5A, bus.Memory[0x4001]);
    }

    [Fact]
    public void DdAndFdSubstituteIndexHalvesForRegisterFamilies()
    {
        var (cpu, _) = CreateCpu(
            0xDD, 0x21, 0x34, 0x12,
            0xDD, 0x26, 0xAB,
            0xDD, 0x2E, 0xCD,
            0xDD, 0x44,
            0xDD, 0x65,
            0xDD, 0x24,
            0xDD, 0x84,
            0xFD, 0x21, 0x00, 0x80,
            0xFD, 0x2D,
            0xFD, 0x4D);

        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.Equal((byte)0xAB, cpu.Registers.B);
        cpu.Step();
        cpu.Step();
        Assert.Equal((ushort)0xCECD, cpu.Registers.IX);
        cpu.Step();
        Assert.Equal((byte)0xCE, cpu.Registers.A);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.Equal((byte)0xFF, cpu.Registers.C);
    }

    [Fact]
    public void IndexedAddUsesTheSelectedIndexAndChainedPrefix()
    {
        var (cpu, _) = CreateCpu(0xDD, 0x21, 0x01, 0x80, 0xFD, 0x21, 0x02, 0x40, 0xDD, 0xFD, 0x29);

        cpu.Step();
        cpu.Step();
        Assert.Equal(19, cpu.Step());
        Assert.Equal((ushort)0x8004, cpu.Registers.IY);
        Assert.Equal((byte)7, cpu.Registers.R);
    }

    [Fact]
    public void BlockIoUsesDocumentedCounterDataAndArithmeticFlags()
    {
        var (cpu, bus) = CreateCpu(0xED, 0xA2, 0xED, 0xAB);
        cpu.Registers.BC = 0x01FF;
        cpu.Registers.HL = 0x4000;
        bus.PortValue = 0x01;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((byte)0x01, bus.Memory[0x4000]);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        // C wraps 0xFF -> 0x00 here (increment direction): (C+1)&0xFF = 0, so
        // t = value + 0 = 1, which never exceeds 0xFF - H/C stay clear. Prior
        // to fixing SetBlockIoFlags's missing byte mask on C+/-1, this
        // asserted HalfCarry/Carry true, which was the bug, not the spec
        // (verified against the redcode/Z80 reference formula).
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.HalfCarry));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.AddSubtract));

        cpu.Registers.BC = 0x0101;
        bus.Memory[0x4001] = 0x80;
        Assert.Equal(16, cpu.Step());
        Assert.Equal((byte)0x01, bus.LastWrittenPort);
        Assert.Equal((byte)0x80, bus.LastWrittenValue);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.AddSubtract));
    }

    [Fact]
    public void ScfAndCcfPreserveAndUpdateTheCorrectFlags()
    {
        var (scf, _) = CreateCpu(0x37);
        scf.Registers.A = 0xA8;
        scf.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.HalfCarry | Z80Flags.AddSubtract;

        scf.Step();

        Assert.Equal((byte)(Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.Carry | Z80Flags.X | Z80Flags.Y), scf.Registers.F);

        var (ccf, _) = CreateCpu(0x3F);
        ccf.Registers.A = 0xA8;
        ccf.Registers.F = Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.Carry;

        ccf.Step();

        Assert.Equal((byte)(Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow |
            Z80Flags.HalfCarry | Z80Flags.X | Z80Flags.Y), ccf.Registers.F);
    }

    private static void EnableInterruptsInMode(Z80Cpu cpu, TestBus bus, byte mode)
    {
        bus.Memory[0] = 0xED;
        bus.Memory[1] = mode switch { 0 => (byte)0x46, 1 => (byte)0x56, _ => (byte)0x5E };
        bus.Memory[2] = 0xFB;
        bus.Memory[3] = 0x00;
        cpu.Registers.PC = 0;
        cpu.Step();
        cpu.Step();
        cpu.Step();
    }

    private static (Z80Cpu Cpu, TestBus Bus) CreateCpu(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        return (new Z80Cpu(bus, new InterruptLines()), bus);
    }

    private static ushort ReadWord(byte[] memory, ushort address) =>
        (ushort)(memory[address] | (memory[(ushort)(address + 1)] << 8));

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte PortValue { get; set; }
        public byte InterruptOpcode { get; set; } = 0xFF;
        public int AcknowledgeCount { get; private set; }
        public byte LastWrittenPort { get; private set; }
        public byte LastWrittenValue { get; private set; }
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => PortValue;
        public void WritePort(byte port, byte value)
        {
            LastWrittenPort = port;
            LastWrittenValue = value;
        }
        public byte AcknowledgeInterrupt()
        {
            AcknowledgeCount++;
            return InterruptOpcode;
        }
    }

    private sealed class TestInterruptLines : IInterruptLines
    {
        public bool IntAsserted { get; set; }
        public bool NmiAsserted { get; set; }
        public bool WaitAsserted { get; set; }
        public void Clear()
        {
            IntAsserted = false;
            NmiAsserted = false;
        }
    }
}

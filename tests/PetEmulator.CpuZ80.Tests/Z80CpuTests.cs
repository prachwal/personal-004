using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;
using PetEmulator.CpuZ80.Memory;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80CpuTests
{
    [Fact]
    public void ResetInitializesRegistersAndInterrupts()
    {
        var (cpu, _, _) = CreateCpu();

        cpu.Registers.A = 0xFF;
        cpu.Registers.PC = 0x1234;
        cpu.Reset();

        Assert.Equal((byte)0, cpu.Registers.A);
        Assert.Equal((ushort)0, cpu.Registers.PC);
        Assert.Equal((ushort)0xFFFF, cpu.Registers.SP);
        Assert.False(cpu.Iff1);
        Assert.False(cpu.Halted);
        Assert.Equal((byte)0, cpu.InterruptMode);
    }

    [Fact]
    public void StepExecutesLoadsArithmeticAndMemoryWrite()
    {
        var (cpu, memory, _) = CreateCpu(0x3E, 0x05, 0xC6, 0x03, 0x32, 0x00, 0x40, 0x76);

        Assert.Equal(7, cpu.Step());
        Assert.Equal(7, cpu.Step());
        Assert.Equal(13, cpu.Step());
        Assert.Equal(8, memory.Read(0x4000));
        Assert.Equal(4, cpu.Step());

        Assert.True(cpu.Halted);
        Assert.Equal((ushort)0x0008, cpu.Registers.PC);
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
    }

    [Fact]
    public void CallAndReturnRestoreStackAndProgramCounter()
    {
        var (cpu, _, _) = CreateCpu(0xCD, 0x06, 0x00, 0x76, 0x00, 0x00, 0x3E, 0x2A, 0xC9);

        Assert.Equal(17, cpu.Step());
        Assert.Equal((ushort)0x0006, cpu.Registers.PC);
        Assert.Equal((ushort)0xFFFD, cpu.Registers.SP);

        cpu.Step();
        Assert.Equal((byte)0x2A, cpu.Registers.A);
        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        Assert.Equal((ushort)0xFFFF, cpu.Registers.SP);
        cpu.Step();
        Assert.True(cpu.Halted);
    }

    [Fact]
    public void IoInstructionsUseTheConfiguredPortBus()
    {
        var (cpu, _, io) = CreateCpu(0xDB, 0xA0, 0xD3, 0xA1, 0x76);

        Assert.Equal(11, cpu.Step());
        Assert.Equal((byte)0x5A, cpu.Registers.A);
        Assert.Equal(11, cpu.Step());
        Assert.Equal((byte)0xA1, io.LastPort);
        Assert.Equal((byte)0x5A, io.LastValue);
    }

    [Fact]
    public void EiDelaysMaskableInterruptUntilFollowingInstruction()
    {
        var (cpu, _, io) = CreateCpu(0xED, 0x56, 0xFB, 0x00, 0x76);
        io.IntAsserted = true;

        cpu.Step();
        cpu.Step();
        Assert.True(cpu.Iff1);
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);

        cpu.Step();
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);

        Assert.Equal(13, cpu.Step());
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
        Assert.False(cpu.Iff1);
    }

    [Fact]
    public void LdirCopiesBlockAndReportsRepeatTiming()
    {
        var (cpu, memory, _) = CreateCpu(0xED, 0xB0);
        cpu.Registers.HL = 0x1000;
        cpu.Registers.DE = 0x2000;
        cpu.Registers.BC = 2;
        memory.Write(0x1000, 0x12);
        memory.Write(0x1001, 0x34);

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((byte)0x12, memory.Read(0x2000));
        Assert.Equal(16, cpu.Step());
        Assert.Equal((byte)0x34, memory.Read(0x2001));
        Assert.Equal((ushort)0, cpu.Registers.BC);
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
    }

    [Fact]
    public void NmiIsTriggeredOnTheLineEdge()
    {
        var (cpu, _, io) = CreateCpu(0x00);
        io.NmiAsserted = true;

        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x0066, cpu.Registers.PC);
        Assert.False(cpu.Iff1);

        Assert.Equal(4, cpu.Step());
    }

    [Fact]
    public void HeldNmiDoesNotRetriggerUntilTheLineFalls()
    {
        var (cpu, _, io) = CreateCpu(0x00);
        io.NmiAsserted = true;

        cpu.Step();
        Assert.Equal((ushort)0x0066, cpu.Registers.PC);
        cpu.Step();
        Assert.Equal((ushort)0x0067, cpu.Registers.PC);

        io.NmiAsserted = false;
        cpu.Step();
        Assert.Equal((ushort)0x0068, cpu.Registers.PC);
        io.NmiAsserted = true;
        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x0066, cpu.Registers.PC);
    }

    [Fact]
    public void Im0ExecutesTheOpcodeSuppliedByTheInterruptingDevice()
    {
        var (cpu, _, io) = CreateCpu(0xED, 0x46, 0xFB, 0x00);
        io.InterruptValue = 0xFF;
        io.IntAsserted = true;

        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal(13, cpu.Step());
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
        Assert.Equal((ushort)0xFFFD, cpu.Registers.SP);
        Assert.Equal((byte)5, cpu.Registers.R);
    }

    /// <summary>Real Z80 WAIT: frozen mid-bus-cycle, no fetch/execute - see Fd1793Controller's own doc trail for the one real hardware source this models.</summary>
    [Fact]
    public void WaitAssertedFreezesTheCpuWithoutExecutingAnything()
    {
        var (cpu, _, io) = CreateCpu(0x3E, 0x05); // LD A,5
        io.WaitAsserted = true;

        var consumed = cpu.Step();

        Assert.Equal(1, consumed);
        Assert.Equal((ushort)0x0000, cpu.Registers.PC); // never fetched
        Assert.Equal((byte)0, cpu.Registers.A); // never executed
    }

    [Fact]
    public void WaitReleasingLetsTheCpuResumeExactlyWhereItLeftOff()
    {
        var (cpu, _, io) = CreateCpu(0x3E, 0x05, 0x76); // LD A,5 / HALT
        io.WaitAsserted = true;

        cpu.Step();
        cpu.Step();
        io.WaitAsserted = false;

        Assert.Equal(7, cpu.Step()); // the LD A,5 finally runs, full cost, nothing lost
        Assert.Equal((byte)5, cpu.Registers.A);
    }

    /// <summary>Real hardware: WAIT blocks everything, including interrupt service, until it releases - a pending NMI/INT does not sneak in during the freeze.</summary>
    [Fact]
    public void WaitBlocksNmiServiceUntilReleased()
    {
        var (cpu, _, io) = CreateCpu(0x00);
        io.WaitAsserted = true;
        io.NmiAsserted = true;

        cpu.Step();

        Assert.Equal((ushort)0x0000, cpu.Registers.PC); // NMI not serviced while frozen

        io.WaitAsserted = false;
        cpu.Step();

        Assert.Equal((ushort)0x0066, cpu.Registers.PC); // now it is
    }

    [Fact]
    public void DerivedCpuCanOverrideAndAddOpcodesDuringInitialization()
    {
        var memory = new RamMemory();
        memory.Write(0x0000, 0x00);
        memory.Write(0x0001, 0x08);
        var io = new TestIoBus();
        var cpu = new ExtendedZ80Cpu(new SystemBus(memory, io), io);

        Assert.Equal(5, cpu.Step());
        Assert.True(cpu.OverrideExecuted);
        Assert.Equal(4, cpu.Step());
        Assert.True(cpu.AddedOpcodeExecuted);
        Assert.Equal((byte)0x42, cpu.Registers.A);
    }

    private static (Z80Cpu Cpu, RamMemory Memory, TestIoBus Io) CreateCpu(params byte[] program)
    {
        var memory = new RamMemory();
        for (var index = 0; index < program.Length; index++)
            memory.Write((ushort)index, program[index]);

        var io = new TestIoBus();
        var cpu = new Z80Cpu(new SystemBus(memory, io), io);
        return (cpu, memory, io);
    }

    private sealed class TestIoBus : IIoBus, IInterruptLines
    {
        public byte LastPort { get; private set; }
        public byte LastValue { get; private set; }
        public bool IntAsserted { get; set; }
        public bool NmiAsserted { get; set; }
        public bool WaitAsserted { get; set; }
        public byte InterruptValue { get; set; } = 0xFF;

        public byte Read(byte port)
        {
            LastPort = port;
            return port == 0 ? InterruptValue : (byte)0x5A;
        }

        public void Write(byte port, byte value)
        {
            LastPort = port;
            LastValue = value;
        }

        public void Clear()
        {
            IntAsserted = false;
            NmiAsserted = false;
        }
    }

    private sealed class ExtendedZ80Cpu(IBus bus, IInterruptLines interruptLines) : Z80Cpu(bus, interruptLines)
    {
        public bool OverrideExecuted { get; private set; }
        public bool AddedOpcodeExecuted { get; private set; }

        protected override void InitializeOpcodes()
        {
            base.InitializeOpcodes();
            RegisterOpcode(0x00, OverrideNop);
            RegisterOpcode(0x08, AddedOpcode);
        }

        private int OverrideNop()
        {
            OverrideExecuted = true;
            return 5;
        }

        private int AddedOpcode()
        {
            AddedOpcodeExecuted = true;
            Registers.A = 0x42;
            return 4;
        }
    }
}

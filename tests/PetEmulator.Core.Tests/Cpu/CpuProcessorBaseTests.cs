using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Core.Tests;

[TestFixture]
public sealed class CpuProcessorBaseTests
{
    [Test]
    public void Base_lifecycle_accounts_cycles_and_completed_instructions()
    {
        var cpu = new TestCpu();

        cpu.StepInstruction();
        cpu.Step();

        cpu.CycleCount.Should().Be(6);
        cpu.InstructionCount.Should().Be(2);
        cpu.Executed.Should().Be(200);
    }

    [Test]
    public void Reset_resets_state_clock_and_instruction_count()
    {
        var cpu = new TestCpu();

        cpu.StepInstruction();
        cpu.SetProgramCounter(0x1234);
        cpu.Reset();

        cpu.CycleCount.Should().Be(0);
        cpu.InstructionCount.Should().Be(0);
        cpu.GetRegisters()["PC"].Should().Be(0);
    }

    [Test]
    public void Derived_configuration_can_add_and_replace_opcodes()
    {
        var cpu = new TestCpu();

        cpu.RegisteredOpcodes.Should().ContainSingle(x => x.Key == OpcodeKey.Base(0x42));
        cpu.StepInstruction();

        cpu.Executed.Should().Be(100);
    }

    [Test]
    public void Opcode_table_rejects_duplicate_add_and_allows_explicit_replace()
    {
        var table = new OpcodeTable<TestState>();
        var original = Definition(1);
        var replacement = Definition(2);

        table.Add(original);
        var action = () => table.Add(original);

        action.Should().Throw<InvalidOperationException>();
        table.Replace(replacement);
        table.Get(OpcodeKey.Base(0x42)).Execute(new TestState(), new CpuExecutionContext(new TestMemory(), new EmulationClock())).Cycles.Should().Be(2);
    }

    private static OpcodeDefinition<TestState> Definition(ulong cycles)
        => new(OpcodeKey.Base(0x42), "TST", 1, (byte)cycles, "Implied", (_, _) => CpuStepResult.Completed(cycles));

    private sealed class TestCpu : CpuProcessorBase<TestState>
    {
        public TestCpu()
            : base(new TestState(), new TestMemory())
        {
            InitializeOpcodes();
        }

        public int Executed { get; private set; }

        public IReadOnlyCollection<OpcodeDefinition<TestState>> RegisteredOpcodes => Opcodes.Entries;

        public void SetProgramCounter(ushort value) => State.ProgramCounter = value;

        protected override void ConfigureOpcodes(OpcodeTable<TestState> table)
        {
            table.Add(new OpcodeDefinition<TestState>(
                OpcodeKey.Base(0x42), "TST", 1, 3, "Implied",
                (_, _) =>
                {
                    Executed += 100;
                    return CpuStepResult.Completed(3);
                }));
        }

        protected override OpcodeKey FetchOpcode() => OpcodeKey.Base(0x42);

        protected override OpcodeDefinition<TestState> DecodeOpcode(OpcodeKey key) => Opcodes.Get(key);

        protected override CpuStepResult ExecuteOpcode(OpcodeDefinition<TestState> definition)
            => definition.Execute(State, ExecutionContext);
    }

    private sealed class TestState : CpuState
    {
        public ushort ProgramCounter { get; set; }

        public override void Reset()
        {
            base.Reset();
            ProgramCounter = 0;
        }

        public override IReadOnlyDictionary<string, ulong> GetRegisters()
            => new Dictionary<string, ulong> { ["PC"] = ProgramCounter };
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte Read(ushort address) => 0;

        public void Write(ushort address, byte value)
        {
        }
    }
}

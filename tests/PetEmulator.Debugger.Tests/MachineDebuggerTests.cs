using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Debugger.Tests;

[TestFixture]
public sealed class MachineDebuggerTests
{
    [Test]
    public void Trace_reports_cycle_and_instruction_counts_per_step()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("trace 3");

        output.Should().Contain("[0] cycles=1 instructions=1 halted=False");
        output.Should().Contain("[1] cycles=2 instructions=2 halted=False");
        output.Should().Contain("[2] cycles=3 instructions=3 halted=False");
        machine.FakeProcessor.InstructionCount.Should().Be(3);
    }

    [Test]
    public void Trace_stops_at_processor_halted()
    {
        var machine = new FakeMachine();
        machine.FakeProcessor.HaltAfterInstructions = 2;
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("trace 10");

        machine.FakeProcessor.InstructionCount.Should().Be(2);
        output.Should().Contain("halted=True");
        output.Should().Contain("[1] halted");
    }

    [Test]
    public void BreakCycle_stops_stepping_exactly_at_target()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);
        debugger.Execute("break-cycle 5");

        var output = debugger.Execute("trace 100");

        machine.FakeProcessor.CycleCount.Should().Be(5);
        output.Should().Contain("break-cycle hit: cycles=5 >= 5");
    }

    [Test]
    public void BreakInstructionCount_stops_stepping_exactly_at_target()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);
        debugger.Execute("break-instruction-count 4");

        var output = debugger.Execute("trace 100");

        machine.FakeProcessor.InstructionCount.Should().Be(4);
        output.Should().Contain("break-instruction-count hit: instructions=4 >= 4");
    }

    [Test]
    public void Watch_reports_value_changes_and_unwatch_stops_reporting()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);
        debugger.Execute("watch 1000");

        machine.FakeMemory.Data[0x1000] = 0x42;
        var output = debugger.Execute("trace 1");
        output.Should().Contain("1000h 00h -> 42h");

        debugger.Execute("unwatch");
        machine.FakeMemory.Data[0x1000] = 0x99;
        var output2 = debugger.Execute("trace 1");
        output2.Should().NotContain("1000h");
    }

    [Test]
    public void WatchRange_watches_every_address_in_the_range_but_not_the_end()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);
        debugger.Execute("watch-range 2000 2003");

        machine.FakeMemory.Data[0x2000] = 1;
        machine.FakeMemory.Data[0x2002] = 2;
        machine.FakeMemory.Data[0x2003] = 3;
        var output = debugger.Execute("trace 1");

        output.Should().Contain("2000h 00h -> 01h");
        output.Should().Contain("2002h 00h -> 02h");
        output.Should().NotContain("2003h");
    }

    [Test]
    public void Dump_renders_the_requested_byte_range()
    {
        var machine = new FakeMachine();
        for (byte i = 0; i < 16; i++)
            machine.FakeMemory.Data[0x3000 + i] = i;
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("dump 3000 3010");

        output.Should().Contain("3000: 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
    }

    [Test]
    public void Trace_includes_a_register_line_when_the_processor_is_debuggable()
    {
        var machine = new FakeDebuggableMachine();
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("trace 1");

        output.Should().Contain("[0]   PC=1235 A=00 X=00 Y=00 SP=00 P=00");
    }

    [Test]
    public void Trace_omits_the_register_line_when_the_processor_is_not_debuggable()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("trace 1");

        output.Should().NotContain("PC=");
    }

    [Test]
    public void BreakPc_stops_stepping_once_pc_reaches_the_target()
    {
        var machine = new FakeDebuggableMachine();
        var debugger = new MachineDebugger(machine);
        debugger.Execute("break-pc 1237");

        var output = debugger.Execute("trace 100");

        machine.FakeProcessor.InstructionCount.Should().Be(3);
        output.Should().Contain("break-pc hit: PC=1237 == 1237");
    }

    [Test]
    public void BreakPc_on_a_non_debuggable_processor_returns_an_error_instead_of_a_silent_no_op()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("break-pc 1234");

        output.Should().Contain("error").And.Contain("IDebuggableProcessor");
    }

    [Test]
    public void Unknown_command_returns_an_error_string_instead_of_throwing()
    {
        var machine = new FakeMachine();
        var debugger = new MachineDebugger(machine);

        var act = () => debugger.Execute("frobnicate 1 2 3");

        act.Should().NotThrow();
        debugger.Execute("frobnicate").Should().Contain("unknown command");
    }

    private sealed class FakeMachine : IMachine
    {
        public string Name => "fake";
        public bool IsReady => true;
        public FakeProcessor FakeProcessor { get; } = new();
        public FakeMemoryBus FakeMemory { get; } = new();
        public IProcessor Processor => FakeProcessor;
        public IMemoryBus Memory => FakeMemory;
        public ulong CycleCount => FakeProcessor.CycleCount;

        public void Reset() => FakeProcessor.Reset();

        public void StepInstruction() => FakeProcessor.StepInstruction();

        public void Run(ulong instructionCount)
        {
            for (var i = 0UL; i < instructionCount; i++)
                StepInstruction();
        }
    }

    private sealed class FakeProcessor : IProcessor
    {
        public bool Halted { get; private set; }
        public ulong CycleCount { get; private set; }
        public ulong InstructionCount { get; private set; }
        public ulong? HaltAfterInstructions { get; set; }

        public void Reset()
        {
            Halted = false;
            CycleCount = 0;
            InstructionCount = 0;
        }

        public void StepInstruction()
        {
            CycleCount++;
            InstructionCount++;
            if (HaltAfterInstructions is { } halt && InstructionCount >= halt)
                Halted = true;
        }

        public void SetIRQ(bool active) { }

        public void SetNMI(bool active) { }
    }

    private sealed class FakeMemoryBus : IMemoryBus
    {
        public byte[] Data { get; } = new byte[65536];

        public byte Read(ushort address) => Data[address];

        public void Write(ushort address, byte value) => Data[address] = value;
    }

    /// <summary>Same shape as <see cref="FakeMachine"/>, but its processor also implements
    /// <see cref="IDebuggableProcessor"/> - exercises <c>trace</c>'s register line and
    /// <c>break-pc</c>, both gated on that optional capability.</summary>
    private sealed class FakeDebuggableMachine : IMachine
    {
        public string Name => "fake-debuggable";
        public bool IsReady => true;
        public FakeDebuggableProcessor FakeProcessor { get; } = new();
        public FakeMemoryBus FakeMemory { get; } = new();
        public IProcessor Processor => FakeProcessor;
        public IMemoryBus Memory => FakeMemory;
        public ulong CycleCount => FakeProcessor.CycleCount;

        public void Reset() => FakeProcessor.Reset();

        public void StepInstruction() => FakeProcessor.StepInstruction();

        public void Run(ulong instructionCount)
        {
            for (var i = 0UL; i < instructionCount; i++)
                StepInstruction();
        }
    }

    private sealed class FakeDebuggableProcessor : IProcessor, IDebuggableProcessor
    {
        public bool Halted { get; private set; }
        public ulong CycleCount { get; private set; }
        public ulong InstructionCount { get; private set; }
        private ushort _pc = 0x1234;

        public void Reset()
        {
            Halted = false;
            CycleCount = 0;
            InstructionCount = 0;
            _pc = 0x1234;
        }

        public void StepInstruction()
        {
            CycleCount++;
            InstructionCount++;
            _pc++;
        }

        public void SetIRQ(bool active) { }

        public void SetNMI(bool active) { }

        public IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
        {
            ["PC"] = _pc, ["A"] = 0, ["X"] = 0, ["Y"] = 0, ["SP"] = 0, ["P"] = 0,
        };
    }
}

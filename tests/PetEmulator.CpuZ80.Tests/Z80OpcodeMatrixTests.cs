using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80OpcodeMatrixTests
{
    [Fact]
    public void EveryCbOpcodeExecutesWithoutAnUnsupportedOpcodeFailure()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var bus = new TestBus { Memory = { [0] = 0xCB, [1] = (byte)opcode, [0x4000] = 0xA5 } };
            var cpu = new Z80Cpu(bus, new InterruptLines());
            cpu.Registers.HL = 0x4000;

            var tStates = cpu.Step();

            AssertConsumedCycles(0xCB, (byte)opcode, tStates);
        }
    }

    [Fact]
    public void EveryBaseOpcodeExecutesWithoutAnUnsupportedOpcodeFailure()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var bus = new TestBus { Memory = { [0] = (byte)opcode } };
            var cpu = new Z80Cpu(bus, new InterruptLines());

            var tStates = cpu.Step();

            AssertConsumedCycles(null, (byte)opcode, tStates);
        }
    }

    [Fact]
    public void EveryEdOpcodeExecutesWithoutAnUnsupportedOpcodeFailure()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var bus = new TestBus { Memory = { [0] = 0xED, [1] = (byte)opcode } };
            var cpu = new Z80Cpu(bus, new InterruptLines());

            var tStates = cpu.Step();

            AssertConsumedCycles(0xED, (byte)opcode, tStates);
        }
    }

    [Theory]
    [InlineData(0xDD)]
    [InlineData(0xFD)]
    public void EveryIndexedOpcodeExecutesWithoutAnUnsupportedOpcodeFailure(byte prefix)
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var bus = new TestBus { Memory = { [0] = prefix, [1] = (byte)opcode } };
            var cpu = new Z80Cpu(bus, new InterruptLines());

            var tStates = cpu.Step();

            AssertConsumedCycles(prefix, (byte)opcode, tStates);
        }
    }

    /// <summary>
    /// Minimal, universally-true invariant for every one of the 1280 opcode
    /// slots (base/CB/ED/DD/FD): a real Z80 machine cycle always consumes at
    /// least 4 T-state (one M1 fetch) and never returns zero/negative. This
    /// is a floor, not a full timing oracle - exact per-opcode T-state
    /// counts are covered case-by-case in Z80TimingTests.cs, not here.
    /// </summary>
    private static void AssertConsumedCycles(byte? prefix, byte opcode, int tStates)
    {
        var label = prefix is { } p ? $"{p:X2} {opcode:X2}" : $"{opcode:X2}";
        Assert.True(tStates >= 4, $"opcode {label} returned {tStates} T-state, expected >= 4");
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

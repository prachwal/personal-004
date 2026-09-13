using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080OpcodeMatrixTests
{
    [Test]
    public void Mov_matrix_executes_every_register_and_memory_combination()
    {
        for (var destination = 0; destination < 8; destination++)
        {
            for (var source = 0; source < 8; source++)
            {
                if (destination == 6 && source == 6)
                    continue; // 0x76 is HLT, not MOV M,M.

                var memory = new TestMemory { Bytes = { [0] = (byte)(0x40 | (destination << 3) | source), [0x2000] = 0x5A } };
                var cpu = new Cpu8080(memory);
                SetRegisters(cpu.CpuState, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77);
                cpu.CpuState.HL = 0x2000;
                var expected = ReadRegister(cpu.CpuState, source, memory);

                cpu.StepInstruction();

                ReadRegister(cpu.CpuState, destination, memory).Should().Be(expected,
                    $"MOV destination={destination}, source={source}");
            }
        }
    }

    [Test]
    public void Register_alu_matrix_executes_all_eight_operations_and_sources()
    {
        for (var operation = 0; operation < 8; operation++)
        {
            for (var source = 0; source < 8; source++)
            {
                var memory = new TestMemory { Bytes = { [0] = (byte)(0x80 | (operation << 3) | source), [0x2000] = 1 } };
                var cpu = new Cpu8080(memory);
                cpu.CpuState.A = 0xF0;
                cpu.CpuState.B = cpu.CpuState.C = cpu.CpuState.D = cpu.CpuState.E = cpu.CpuState.H = cpu.CpuState.L = 1;
                cpu.CpuState.HL = 0x2000;
                var value = ReadRegister(cpu.CpuState, source, memory);

                cpu.StepInstruction();

                cpu.CpuState.A.Should().Be(operation switch
                {
                    0 or 1 => (byte)(0xF0 + value),
                    2 or 3 => (byte)(0xF0 - value),
                    4 => (byte)(0xF0 & value),
                    5 => (byte)(0xF0 ^ value),
                    6 => (byte)(0xF0 | value),
                    7 => 0xF0,
                    _ => throw new AssertionException("Unknown ALU operation."),
                }, $"ALU operation={operation}, source={source}");
            }
        }
    }

    [Test]
    public void Conditional_jump_matrix_takes_all_eight_conditions()
    {
        for (var condition = 0; condition < 8; condition++)
        {
            var opcode = (byte)(0xC2 | (condition << 3));
            var memory = new TestMemory { Bytes = { [0] = opcode, [1] = 0x34, [2] = 0x12 } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.Flags = condition switch
            {
                0 => 0,
                1 => 0x40,
                2 => 0,
                3 => 0x01,
                4 => 0,
                5 => 0x04,
                6 => 0,
                7 => 0x80,
                _ => 0,
            };

            cpu.StepInstruction();

            cpu.CpuState.PC.Should().Be(0x1234, $"condition={condition}");
        }
    }

    [Test]
    public void Rst_matrix_pushes_return_address_and_selects_all_vectors()
    {
        for (var vector = 0; vector < 8; vector++)
        {
            var memory = new TestMemory { Bytes = { [0] = (byte)(0xC7 | (vector << 3)) } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.SP = 0x1000;

            cpu.StepInstruction();

            cpu.CpuState.PC.Should().Be((ushort)(vector * 8));
            cpu.CpuState.SP.Should().Be(0x0FFE);
            memory.Bytes[0x0FFE].Should().Be(1);
            memory.Bytes[0x0FFF].Should().Be(0);
        }
    }

    [Test]
    public void Push_pop_matrix_round_trips_all_register_pairs_and_psw()
    {
        var pairs = new[]
        {
            (Push: (byte)0xC5, Pop: (byte)0xC1, Name: "BC"),
            (Push: (byte)0xD5, Pop: (byte)0xD1, Name: "DE"),
            (Push: (byte)0xE5, Pop: (byte)0xE1, Name: "HL"),
            (Push: (byte)0xF5, Pop: (byte)0xF1, Name: "PSW"),
        };

        foreach (var pair in pairs)
        {
            var memory = new TestMemory { Bytes = { [0] = pair.Push, [1] = pair.Pop } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.BC = 0x1234;
            cpu.CpuState.DE = 0x5678;
            cpu.CpuState.HL = 0x9ABC;
            cpu.CpuState.A = 0xD5;
            cpu.CpuState.Flags = 0x95;
            cpu.CpuState.SP = 0x1000;

            cpu.StepInstruction();
            cpu.CpuState.BC = cpu.CpuState.DE = cpu.CpuState.HL = 0;
            cpu.CpuState.A = 0;
            cpu.CpuState.Flags = 0;
            cpu.StepInstruction();

            switch (pair.Name)
            {
                case "BC": cpu.CpuState.BC.Should().Be(0x1234); break;
                case "DE": cpu.CpuState.DE.Should().Be(0x5678); break;
                case "HL": cpu.CpuState.HL.Should().Be(0x9ABC); break;
                case "PSW":
                    cpu.CpuState.A.Should().Be(0xD5);
                    (cpu.CpuState.Flags & 0xD5).Should().Be(0x95);
                    break;
            }
        }
    }

    private static void SetRegisters(Cpu8080State state, byte a, byte b, byte c, byte d, byte e, byte h, byte l)
    {
        state.A = a;
        state.B = b;
        state.C = c;
        state.D = d;
        state.E = e;
        state.H = h;
        state.L = l;
    }

    private static byte ReadRegister(Cpu8080State state, int index, TestMemory memory)
        => index switch
        {
            0 => state.B,
            1 => state.C,
            2 => state.D,
            3 => state.E,
            4 => state.H,
            5 => state.L,
            6 => memory.Bytes[state.HL],
            7 => state.A,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}

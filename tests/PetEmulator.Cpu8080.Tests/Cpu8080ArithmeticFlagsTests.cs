using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080ArithmeticFlagsTests
{
    [Test]
    public void Parity_flag_is_even_for_every_8_bit_result()
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var memory = new TestMemory { Bytes = { [0] = 0xF6, [1] = (byte)value } };
            var cpu = new Cpu8080(memory);

            cpu.StepInstruction();

            var expected = (BitOperations.PopCount((uint)value) & 1) == 0;
            (cpu.CpuState.Flags & 0x04).Should().Be(expected ? 0x04 : 0,
                $"value=0x{value:X2}");
        }
    }

    [Test]
    public void Inr_and_dcr_matrix_updates_auxiliary_carry_but_preserves_carry()
    {
        for (var target = 0; target < 8; target++)
        {
            var inrMemory = new TestMemory { Bytes = { [0] = (byte)(0x04 | (target << 3)), [0x2000] = 0x0F } };
            var inrCpu = new Cpu8080(inrMemory);
            inrCpu.CpuState.HL = 0x2000;
            SetRegister(inrCpu.CpuState, target, 0x0F, inrMemory);
            inrCpu.CpuState.Flags = 0x01;

            inrCpu.StepInstruction();

            ReadRegister(inrCpu.CpuState, target, inrMemory).Should().Be(0x10, $"INR target={target}");
            (inrCpu.CpuState.Flags & 0x11).Should().Be(0x11, $"INR target={target}");

            var dcrMemory = new TestMemory { Bytes = { [0] = (byte)(0x05 | (target << 3)), [0x2000] = 0x10 } };
            var dcrCpu = new Cpu8080(dcrMemory);
            dcrCpu.CpuState.HL = 0x2000;
            SetRegister(dcrCpu.CpuState, target, 0x10, dcrMemory);
            dcrCpu.CpuState.Flags = 0x01;

            dcrCpu.StepInstruction();

            ReadRegister(dcrCpu.CpuState, target, dcrMemory).Should().Be(0x0F, $"DCR target={target}");
            (dcrCpu.CpuState.Flags & 0x11).Should().Be(0x11, $"DCR target={target}");
        }
    }

    [Test]
    public void Dad_matrix_changes_only_carry_among_the_8080_flags()
    {
        var opcodes = new byte[] { 0x09, 0x19, 0x29, 0x39 };
        var operands = new ushort[] { 1, 1, 0xFFFF, 1 };

        for (var pair = 0; pair < opcodes.Length; pair++)
        {
            var memory = new TestMemory { Bytes = { [0] = opcodes[pair] } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.HL = 0xFFFF;
            cpu.CpuState.BC = 1;
            cpu.CpuState.DE = 1;
            cpu.CpuState.SP = 1;
            cpu.CpuState.Flags = 0xD4;

            cpu.StepInstruction();

            cpu.CpuState.HL.Should().Be((ushort)(0xFFFF + operands[pair]), $"DAD pair={pair}");
            (cpu.CpuState.Flags & 0xD4).Should().Be(0xD4, $"DAD pair={pair}");
            (cpu.CpuState.Flags & 0x01).Should().Be(0x01, $"DAD pair={pair}");
        }
    }

    [Test]
    public void Rotates_change_only_carry_and_use_the_previous_carry_when_required()
    {
        var cases = new[]
        {
            (Opcode: (byte)0x07, Accumulator: (byte)0x81, Carry: false, Result: (byte)0x03),
            (Opcode: (byte)0x0F, Accumulator: (byte)0x81, Carry: false, Result: (byte)0xC0),
            (Opcode: (byte)0x17, Accumulator: (byte)0x80, Carry: true, Result: (byte)0x01),
            (Opcode: (byte)0x1F, Accumulator: (byte)0x01, Carry: true, Result: (byte)0x80),
        };

        foreach (var test in cases)
        {
            var memory = new TestMemory { Bytes = { [0] = test.Opcode } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.A = test.Accumulator;
            cpu.CpuState.Flags = (byte)(0xD4 | (test.Carry ? 0x01 : 0));

            cpu.StepInstruction();

            cpu.CpuState.A.Should().Be(test.Result, $"opcode=0x{test.Opcode:X2}");
            (cpu.CpuState.Flags & 0xD4).Should().Be(0xD4, $"opcode=0x{test.Opcode:X2}");
            (cpu.CpuState.Flags & 0x01).Should().Be(0x01, $"opcode=0x{test.Opcode:X2}");
        }
    }

    [Test]
    public void Cma_does_not_change_flags_and_stc_cmc_change_only_carry()
    {
        var cmaMemory = new TestMemory { Bytes = { [0] = 0x2F } };
        var cmaCpu = new Cpu8080(cmaMemory);
        cmaCpu.CpuState.A = 0x5A;
        cmaCpu.CpuState.Flags = 0xD4;
        cmaCpu.StepInstruction();
        cmaCpu.CpuState.A.Should().Be(0xA5);
        cmaCpu.CpuState.Flags.Should().Be(0xD4);

        var stcMemory = new TestMemory { Bytes = { [0] = 0x37, [1] = 0x3F } };
        var stcCpu = new Cpu8080(stcMemory);
        stcCpu.CpuState.Flags = 0xD4;
        stcCpu.StepInstruction();
        stcCpu.CpuState.Flags.Should().Be(0xD5);
        stcCpu.StepInstruction();
        stcCpu.CpuState.Flags.Should().Be(0xD4);
    }

    [TestCase(0x0A, 0x00, 0x10, 0x10, 0x00)]
    [TestCase(0x99, 0x00, 0x99, 0x00, 0x00)]
    [TestCase(0x9A, 0x00, 0x00, 0x10, 0x01)]
    [TestCase(0xA0, 0x01, 0x00, 0x00, 0x01)]
    public void Daa_applies_decimal_correction_and_preserves_8080_flag_rules(
        byte accumulator, byte initialFlags, byte expectedAccumulator, byte expectedAuxiliaryCarry, byte expectedCarry)
    {
        var memory = new TestMemory { Bytes = { [0] = 0x27 } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.A = accumulator;
        cpu.CpuState.Flags = initialFlags;

        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(expectedAccumulator);
        (cpu.CpuState.Flags & 0x10).Should().Be(expectedAuxiliaryCarry);
        (cpu.CpuState.Flags & 0x01).Should().Be(expectedCarry);
    }

    [Test]
    public void Unused_flag_bits_are_cleared_from_the_execution_state()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x00 } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.Flags = 0xFF;

        cpu.StepInstruction();

        cpu.CpuState.Flags.Should().Be(0xD5);
    }

    private static void SetRegister(Cpu8080State state, int index, byte value, TestMemory memory)
    {
        switch (index)
        {
            case 0: state.B = value; break;
            case 1: state.C = value; break;
            case 2: state.D = value; break;
            case 3: state.E = value; break;
            case 4: state.H = value; break;
            case 5: state.L = value; break;
            case 6: memory.Bytes[state.HL] = value; break;
            case 7: state.A = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
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

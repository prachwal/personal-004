using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080AluDifferentialTests
{
    [Test]
    public void Immediate_alu_matches_an_independent_8080_reference_model()
    {
        var random = new Random(0x8080);
        var opcodes = new byte[] { 0xC6, 0xCE, 0xD6, 0xDE, 0xE6, 0xEE, 0xF6, 0xFE };

        for (var iteration = 0; iteration < 4096; iteration++)
        {
            var operation = iteration % opcodes.Length;
            var accumulator = (byte)random.Next(256);
            var operand = (byte)random.Next(256);
            var carryIn = random.Next(2) != 0;
            var expected = ReferenceModel(operation, accumulator, operand, carryIn);
            var memory = new TestMemory { Bytes = { [0] = opcodes[operation], [1] = operand } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.A = accumulator;
            cpu.CpuState.Flags = carryIn ? (byte)0x01 : (byte)0;

            cpu.StepInstruction();

            cpu.CpuState.A.Should().Be(expected.Result, $"operation={operation}, iteration={iteration}");
            (cpu.CpuState.Flags & 0xD5).Should().Be(expected.Flags,
                $"operation={operation}, A=0x{accumulator:X2}, operand=0x{operand:X2}, carry={carryIn}");
        }
    }

    private static (byte Result, byte Flags) ReferenceModel(int operation, byte accumulator, byte operand, bool carryIn)
    {
        var carry = false;
        var auxiliaryCarry = false;
        var result = accumulator;

        switch (operation)
        {
            case 0 or 1:
            {
                var carryValue = operation == 1 && carryIn ? 1 : 0;
                var sum = accumulator + operand + carryValue;
                result = (byte)sum;
                auxiliaryCarry = ((accumulator & 0x0F) + (operand & 0x0F) + carryValue) > 0x0F;
                carry = sum > 0xFF;
                break;
            }
            case 2 or 3 or 7:
            {
                var borrow = operation == 3 && carryIn ? 1 : 0;
                var difference = accumulator - operand - borrow;
                result = (byte)difference;
                auxiliaryCarry = ((accumulator & 0x0F) - (operand & 0x0F) - borrow) < 0;
                carry = difference < 0;
                if (operation == 7)
                    result = accumulator;
                break;
            }
            case 4:
                result = (byte)(accumulator & operand);
                auxiliaryCarry = ((accumulator | operand) & 0x08) != 0;
                break;
            case 5:
                result = (byte)(accumulator ^ operand);
                break;
            case 6:
                result = (byte)(accumulator | operand);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        var flagsValue = operation == 7 ? (byte)(accumulator - operand) : result;

        var flags = (byte)0;
        if ((flagsValue & 0x80) != 0) flags |= 0x80;
        if (flagsValue == 0) flags |= 0x40;
        if (auxiliaryCarry) flags |= 0x10;
        if ((BitOperations.PopCount(flagsValue) & 1) == 0) flags |= 0x04;
        if (carry) flags |= 0x01;
        return (result, flags);
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}

using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Core.Tests;

[TestFixture]
public sealed class CpuCommonHelpersTests
{
    [TestCase(0x0F, 0x01, false, 0x10, false, true, false)]
    [TestCase(0xFF, 0x01, false, 0x00, true, true, false)]
    [TestCase(0x7F, 0x01, false, 0x80, false, true, true)]
    public void Add8_returns_shared_result_and_independent_flag_inputs(
        byte left,
        byte right,
        bool carryIn,
        byte result,
        bool carry,
        bool halfCarry,
        bool overflow)
    {
        CpuArithmetic.Add8(left, right, carryIn).Should().Be(new CpuArithmeticResult(result, carry, halfCarry, overflow));
    }

    [TestCase(0x00, 0x01, false, 0xFF, true, true, false)]
    [TestCase(0x80, 0x01, false, 0x7F, false, true, true)]
    [TestCase(0x00, 0x00, true, 0xFF, true, true, false)]
    public void Subtract8_returns_borrow_half_borrow_and_overflow(
        byte left,
        byte right,
        bool borrowIn,
        byte result,
        bool carry,
        bool halfCarry,
        bool overflow)
    {
        CpuArithmetic.Subtract8(left, right, borrowIn).Should().Be(new CpuArithmeticResult(result, carry, halfCarry, overflow));
    }

    [Test]
    public void Register_helpers_use_the_8080_and_z80_register_encoding()
    {
        var memory = new Dictionary<ushort, byte> { [0x1234] = 0xA5 };
        var value = CpuOperandHelpers.ReadRegister(6, 1, 2, 3, 4, 5, 6, 7, 0x1234, address => memory[address]);

        value.Should().Be(0xA5);

        var registers = new byte[7];
        CpuOperandHelpers.WriteRegister(4, 0xCC, value => registers[0] = value, value => registers[1] = value,
            value => registers[2] = value, value => registers[3] = value, value => registers[4] = value,
            value => registers[5] = value, value => registers[6] = value, 0x1234,
            (_, written) => memory[0x1234] = written);

        registers[5].Should().Be(0xCC);
        CpuOperandHelpers.ReadPair(3, 1, 2, 3, 4).Should().Be(4);
    }

    [Test]
    public void Condition_helpers_match_8080_and_z80_condition_order()
    {
        CpuOperandHelpers.EvaluateCondition(0, zero: true, carry: false, parity: true, sign: false).Should().BeFalse();
        CpuOperandHelpers.EvaluateCondition(1, zero: true, carry: false, parity: true, sign: false).Should().BeTrue();
        CpuOperandHelpers.EvaluateCondition(5, zero: false, carry: false, parity: true, sign: false).Should().BeTrue();
        CpuOperandHelpers.EvaluateCondition(7, zero: false, carry: false, parity: true, sign: true).Should().BeTrue();
    }
}

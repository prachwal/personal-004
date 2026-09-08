using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public sealed class OpcodeTableTests
{
    [Test]
    public void Nmos_table_has_one_entry_for_every_opcode()
    {
        var table = global::Cpu6502.OpcodeTables.CreateNmos();
        table.Definitions.Should().HaveCount(256);
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
            table[(byte)opcode].Opcode.Should().Be((byte)opcode);

        table[0xA9].Mnemonic.Should().Be("LDA");
        table[0xA9].BaseCycles.Should().Be(2);
        table[0xA9].AddressingMode.Should().Be(global::Cpu6502.AddressingMode.Immediate);
        table[0xA9].Length.Should().Be(2);
        table[0xAD].AddressingMode.Should().Be(global::Cpu6502.AddressingMode.Absolute);
        table[0xAD].Length.Should().Be(3);
        table[0x6C].AddressingMode.Should().Be(global::Cpu6502.AddressingMode.Indirect);
        table[0x00].Mnemonic.Should().Be("BRK");
        table[0x00].BaseCycles.Should().Be(7);
    }

    [Test]
    public void Nmos_table_maps_every_opcode_to_a_non_fallback_handler()
    {
        var table = global::Cpu6502.OpcodeTables.Nmos;

        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
            table[(byte)opcode].Handler.Method.Name.Should().NotBe("ExecuteUnmappedOpcodeCycle", $"opcode 0x{opcode:X2} must have a concrete handler");
    }

    [Test]
    public void Derived_table_can_remove_an_opcode_without_mutating_the_base()
    {
        var baseTable = global::Cpu6502.OpcodeTables.CreateNmos();
        global::Cpu6502.OpcodeTable derived = baseTable.Derive(table => table.Remove(0xEA));

        derived.Definitions.Should().HaveCount(255);
        baseTable.Definitions.Should().HaveCount(256);
        derived.IsSealed.Should().BeTrue();
        FluentActions.Invoking(() => derived.Set(baseTable[0xEA])).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => derived[0xEA]).Should().Throw<NotSupportedException>();
    }

    [Test]
    public void Nmos_table_is_shared_and_sealed()
    {
        global::Cpu6502.OpcodeTables.CreateNmos().Should().BeSameAs(global::Cpu6502.OpcodeTables.CreateNmos());
        global::Cpu6502.OpcodeTables.Nmos.IsSealed.Should().BeTrue();
    }

    [Test]
    public void Cpu_variant_uses_the_supplied_opcode_table()
    {
        var table = global::Cpu6502.OpcodeTables.CreateNmos();
        var cpu = new global::Cpu6502.Variants.Cpu6502Classic(new TestMemory(), table);

        cpu.Opcodes.Should().BeSameAs(table);
    }

    [Test]
    public void All_nmos_opcodes_have_correct_cycle_counts()
    {
        // Regression net against the OpcodeTables.BaseCyclesFor() table-construction switch.
        // Structured row-by-row for easier auditing. Default for unlisted opcodes is 2.
        byte[] expectedCycles =
        [
            // 0x00-0x0F
            7, 6, 1, 2, 3, 3, 5, 2, 3, 2, 2, 2, 4, 4, 6, 2,
            // 0x10-0x1F
            2, 5, 1, 2, 4, 4, 6, 2, 2, 4, 2, 2, 4, 4, 7, 2,
            // 0x20-0x2F
            6, 6, 1, 2, 3, 3, 5, 2, 4, 2, 2, 2, 4, 4, 6, 2,
            // 0x30-0x3F
            2, 5, 1, 2, 4, 4, 6, 2, 2, 4, 2, 2, 4, 4, 7, 2,
            // 0x40-0x4F
            6, 6, 1, 2, 3, 3, 5, 2, 3, 2, 2, 2, 3, 4, 6, 2,
            // 0x50-0x5F
            2, 5, 1, 2, 4, 4, 6, 2, 2, 4, 2, 2, 4, 4, 7, 2,
            // 0x60-0x6F
            6, 6, 1, 2, 3, 3, 5, 2, 4, 2, 2, 2, 5, 4, 6, 2,
            // 0x70-0x7F
            2, 5, 1, 2, 4, 4, 6, 2, 2, 4, 2, 2, 4, 4, 7, 2,
            // 0x80-0x8F
            2, 6, 2, 6, 3, 3, 3, 2, 2, 2, 2, 2, 4, 4, 4, 2,
            // 0x90-0x9F
            2, 6, 1, 6, 4, 4, 4, 2, 2, 5, 2, 5, 5, 5, 5, 5,
            // 0xA0-0xAF
            2, 6, 2, 2, 3, 3, 3, 2, 2, 2, 2, 2, 4, 4, 4, 2,
            // 0xB0-0xBF
            2, 5, 1, 2, 4, 4, 4, 2, 2, 4, 2, 4, 4, 4, 4, 2,
            // 0xC0-0xCF
            2, 6, 2, 2, 3, 3, 5, 2, 2, 2, 2, 2, 4, 4, 6, 2,
            // 0xD0-0xDF
            2, 5, 1, 2, 4, 4, 6, 2, 2, 4, 2, 2, 4, 4, 7, 2,
            // 0xE0-0xEF
            2, 6, 2, 2, 3, 3, 5, 2, 2, 2, 2, 2, 4, 4, 6, 2,
            // 0xF0-0xFF
            2, 5, 1, 2, 4, 4, 6, 2, 2, 4, 2, 2, 4, 4, 7, 2
        ];

        var table = global::Cpu6502.OpcodeTables.CreateNmos();
        for (int opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            table[(byte)opcode].BaseCycles.Should()
                .Be(expectedCycles[opcode],
                    $"Opcode 0x{opcode:X2} ({table[(byte)opcode].Mnemonic}) should have {expectedCycles[opcode]} cycles");
        }
    }

    private sealed class TestMemory : global::Cpu6502.IMemoryBus
    {
        public byte Read(ushort address) => 0;
        public void Write(ushort address, byte value) { }
    }
}

using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public sealed class OpcodeTableTests
{
    [Test]
    public void Nmos_table_has_one_entry_for_every_opcode()
    {
        var table = global::PetEmulator.Cpu6502.OpcodeTables.NmosCore;
        table.Entries.Should().HaveCount(256);
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
            table.Get(OpcodeKey.Base((byte)opcode)).Key.Opcode.Should().Be((byte)opcode);

        table.Get(OpcodeKey.Base(0xA9)).Mnemonic.Should().Be("LDA");
        table.Get(OpcodeKey.Base(0xA9)).BaseCycles.Should().Be(2);
        table.Get(OpcodeKey.Base(0xA9)).AddressingMode.Should().Be("Immediate");
        table.Get(OpcodeKey.Base(0xA9)).Length.Should().Be(2);
        table.Get(OpcodeKey.Base(0xAD)).AddressingMode.Should().Be("Absolute");
        table.Get(OpcodeKey.Base(0xAD)).Length.Should().Be(3);
        table.Get(OpcodeKey.Base(0x6C)).AddressingMode.Should().Be("Indirect");
        table.Get(OpcodeKey.Base(0x00)).Mnemonic.Should().Be("BRK");
        table.Get(OpcodeKey.Base(0x00)).BaseCycles.Should().Be(7);
    }

    [Test]
    public void Nmos_table_maps_every_opcode_to_a_non_fallback_handler()
    {
        var table = global::PetEmulator.Cpu6502.OpcodeTables.NmosCore;
        foreach (var definition in table.Entries)
            definition.ExecuteCycle.Should().NotBeNull();
    }

    [Test]
    public void Derived_table_can_remove_an_opcode_without_mutating_the_base()
    {
        var baseTable = global::PetEmulator.Cpu6502.OpcodeTables.NmosCore;
        var derived = baseTable.Derive(table => table.Remove(OpcodeKey.Base(0xEA)));

        derived.Entries.Should().HaveCount(255);
        baseTable.Entries.Should().HaveCount(256);
        derived.IsSealed.Should().BeTrue();
        FluentActions.Invoking(() => derived.Set(baseTable.Get(OpcodeKey.Base(0xEA)))).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => derived.Get(OpcodeKey.Base(0xEA))).Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Nmos_table_is_shared_and_sealed()
    {
        global::PetEmulator.Cpu6502.OpcodeTables.NmosCore.Should().BeSameAs(global::PetEmulator.Cpu6502.OpcodeTables.NmosCore);
        global::PetEmulator.Cpu6502.OpcodeTables.NmosCore.IsSealed.Should().BeTrue();
    }

    [Test]
    public void Cpu_variant_uses_the_supplied_opcode_table()
    {
        var table = global::PetEmulator.Cpu6502.OpcodeTables.NmosCore;
        var cpu = new global::PetEmulator.Cpu6502.Variants.Cpu6502Classic(new TestMemory(), table);

        cpu.Opcodes.Get(OpcodeKey.Base(0xEA)).Mnemonic.Should().Be(table.Get(OpcodeKey.Base(0xEA)).Mnemonic);
    }

    [Test]
    public void Every_variant_registers_its_opcode_set_in_the_shared_core_table()
    {
        var cpus = new Cpu6502[]
        {
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Classic(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Atari6507(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Commodore6510(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Nes(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Cmos65C02(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502WdcR65C02S(new TestMemory()),
        };

        foreach (var cpu in cpus)
        {
            cpu.Opcodes.Entries.Should().HaveCount(256);
            cpu.Opcodes.Entries.Select(definition => definition.Key.Page)
                .Should().OnlyContain(page => page == 0);
            cpu.Opcodes.Entries.Should().Contain(definition => definition.Key.Opcode == 0xA9);
        }
    }

    [Test]
    public void Default_variant_tables_are_published_as_sealed_core_tables()
    {
        global::PetEmulator.Cpu6502.OpcodeTables.NmosCore.IsSealed.Should().BeTrue();
        global::PetEmulator.Cpu6502.OpcodeTables.Cmos65C02Core.IsSealed.Should().BeTrue();
        global::PetEmulator.Cpu6502.OpcodeTables.R65C02SCore.IsSealed.Should().BeTrue();

        global::PetEmulator.Cpu6502.OpcodeTables.CreateNmosVariant().OpcodeTable
            .Should().BeSameAs(global::PetEmulator.Cpu6502.OpcodeTables.NmosCore);
    }

    [Test]
    public void Every_shared_opcode_definition_has_a_cycle_executor()
    {
        var cpus = new Cpu6502[]
        {
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Classic(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Atari6507(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Commodore6510(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Nes(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502Cmos65C02(new TestMemory()),
            new global::PetEmulator.Cpu6502.Variants.Cpu6502WdcR65C02S(new TestMemory()),
        };

        foreach (var cpu in cpus)
            foreach (var definition in cpu.Opcodes.Entries)
                definition.ExecuteCycle.Should().NotBeNull();
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

        var table = global::PetEmulator.Cpu6502.OpcodeTables.NmosCore;
        for (int opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var definition = table.Get(OpcodeKey.Base((byte)opcode));
            definition.BaseCycles.Should()
                .Be(expectedCycles[opcode],
                    $"Opcode 0x{opcode:X2} ({definition.Mnemonic}) should have {expectedCycles[opcode]} cycles");
        }
    }

    [Test]
    public void All_nmos_opcodes_have_correct_page_cross_penalty_flags()
    {
        // Regression test against OpcodeDefinition.HasPageCrossPenalty.
        // The 23 opcodes with page-cross penalties are those with AbsX or AbsY addressing modes
        // that access memory for reads (LDA, LDX, LDY, ADC, SBC, CMP, BIT operations on absolute,X/Y).
        byte[] expectedPageCrossOpcodes =
        [
            0xBD, 0xB9, 0xB1, 0xBE, 0xBC,  // LDA, LDA, LDA, LDX, LDY (AbsX/AbsY/IndY variants)
            0x7D, 0x79, 0x71,              // ADC abs,X; ADC abs,Y; ADC (ind),Y
            0xFD, 0xF9, 0xF1,              // SBC abs,X; SBC abs,Y; SBC (ind),Y
            0xDD, 0xD9, 0xD1,              // CMP abs,X; CMP abs,Y; CMP (ind),Y
            0x3D, 0x39, 0x31,              // AND abs,X; AND abs,Y; AND (ind),Y
            0x1D, 0x19, 0x11,              // ORA abs,X; ORA abs,Y; ORA (ind),Y
            0x5D, 0x59, 0x51               // EOR abs,X; EOR abs,Y; EOR (ind),Y
        ];
        var expectedSet = new HashSet<byte>(expectedPageCrossOpcodes);

        var table = global::PetEmulator.Cpu6502.OpcodeTables.NmosCore;
        for (int opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            bool shouldHavePenalty = expectedSet.Contains((byte)opcode);
            var definition = table.Get(OpcodeKey.Base((byte)opcode));
            definition.HasPageCrossPenalty.Should()
                .Be(shouldHavePenalty,
                    $"Opcode 0x{opcode:X2} ({definition.Mnemonic}) page-cross penalty flag is incorrect");
        }
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte Read(ushort address) => 0;
        public void Write(ushort address, byte value) { }
    }
}

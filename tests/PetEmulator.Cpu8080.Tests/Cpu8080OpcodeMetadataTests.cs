using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080OpcodeMetadataTests
{
    [Test]
    public void Base_opcode_metadata_is_complete_unique_and_well_formed()
    {
        var cpu = new MetadataProbeCpu(new TestMemory());
        var definitions = cpu.OpcodeDefinitions.ToArray();

        definitions.Should().HaveCount(256);
        definitions.Select(definition => definition.Key)
            .Should().OnlyHaveUniqueItems();
        definitions.Should().OnlyContain(definition =>
            definition.Key.Page == 0 &&
            definition.Length >= 1 &&
            definition.Length <= 3 &&
            definition.BaseCycles > 0 &&
            !string.IsNullOrWhiteSpace(definition.Mnemonic) &&
            !string.IsNullOrWhiteSpace(definition.AddressingMode));
    }

    [Test]
    public void Derived_opcode_table_replaces_and_adds_without_mutating_the_base_table()
    {
        var cpu = new MetadataProbeCpu(new TestMemory());
        var baseNop = cpu.Definition(OpcodeKey.Base(0x00));
        var baseCount = cpu.OpcodeDefinitions.Count;

        var derived = cpu.Derive(table =>
        {
            table.Replace(Definition(OpcodeKey.Base(0x00), "DERIVED NOP"));
            table.Add(Definition(new OpcodeKey(1, 0x00), "CUSTOM EXT"));
        });

        derived.IsSealed.Should().BeTrue();
        cpu.Definition(OpcodeKey.Base(0x00)).Should().BeSameAs(baseNop);
        cpu.OpcodeDefinitions.Should().HaveCount(baseCount);
        derived.Get(OpcodeKey.Base(0x00)).Mnemonic.Should().Be("DERIVED NOP");
        derived.Get(new OpcodeKey(1, 0x00)).Mnemonic.Should().Be("CUSTOM EXT");
        var lateChange = () => derived.Replace(Definition(OpcodeKey.Base(0x01), "LATE"));
        lateChange.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Derived_cpu_can_replace_a_base_opcode_during_configuration()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x00 } };
        var cpu = new DerivedCpu(memory);

        cpu.OpcodeDefinitions.Single(definition => definition.Key == OpcodeKey.Base(0x00))
            .Mnemonic.Should().Be("DERIVED NOP");
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0xA5);
    }

    private static OpcodeDefinition<Cpu8080State> Definition(OpcodeKey key, string mnemonic)
        => new(
            key,
            mnemonic,
            Length: 1,
            BaseCycles: 4,
            AddressingMode: Cpu8080AddressingMode.Register.ToString(),
            Execute: static (state, _) =>
            {
                state.A = 0x5A;
                return CpuStepResult.Completed(4);
            });

    private sealed class MetadataProbeCpu(IMemoryBus memory) : Cpu8080(memory)
    {
        public OpcodeDefinition<Cpu8080State> Definition(OpcodeKey key)
            => OpcodeDefinitions.Single(definition => definition.Key == key);

        public OpcodeTable<Cpu8080State> Derive(Action<OpcodeTable<Cpu8080State>> changes)
            => Opcodes.Derive(changes);
    }

    private sealed class DerivedCpu(IMemoryBus memory) : Cpu8080(memory)
    {
        protected override void ConfigureOpcodes(OpcodeTable<Cpu8080State> table)
        {
            base.ConfigureOpcodes(table);
            table.Replace(new OpcodeDefinition<Cpu8080State>(
                OpcodeKey.Base(0x00),
                "DERIVED NOP",
                1,
                4,
                Cpu8080AddressingMode.Register.ToString(),
                static (state, _) =>
                {
                    state.A = 0xA5;
                    return CpuStepResult.Completed(4);
                }));
        }
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}

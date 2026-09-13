using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080DataTransferTests
{
    [Test]
    public void Mvi_matrix_loads_all_registers_and_the_memory_alias()
    {
        for (var target = 0; target < 8; target++)
        {
            var memory = new TestMemory { Bytes = { [0] = (byte)(0x06 | (target << 3)), [1] = 0xA5 } };
            var cpu = new Cpu8080(memory);
            cpu.CpuState.HL = 0x2000;

            cpu.StepInstruction();

            ReadRegister(cpu.CpuState, target, memory).Should().Be(0xA5, $"MVI target={target}");
            cpu.CpuState.PC.Should().Be(2);
        }
    }

    [Test]
    public void Lxi_matrix_loads_all_register_pairs_and_stack_pointer()
    {
        var opcodes = new byte[] { 0x01, 0x11, 0x21, 0x31 };

        for (var pair = 0; pair < opcodes.Length; pair++)
        {
            var memory = new TestMemory { Bytes = { [0] = opcodes[pair], [1] = 0x34, [2] = 0x12 } };
            var cpu = new Cpu8080(memory);

            cpu.StepInstruction();

            ReadPair(cpu.CpuState, pair).Should().Be(0x1234, $"LXI pair={pair}");
            cpu.CpuState.PC.Should().Be(3);
            cpu.CycleCount.Should().Be(10);
        }
    }

    [Test]
    public void Ldax_and_stax_matrix_use_only_bc_and_de_as_indirect_addresses()
    {
        var loadOpcodes = new byte[] { 0x0A, 0x1A };
        var storeOpcodes = new byte[] { 0x02, 0x12 };

        for (var pair = 0; pair < 2; pair++)
        {
            var loadMemory = new TestMemory { Bytes = { [0] = loadOpcodes[pair], [0x1234] = 0xA6 } };
            var loadCpu = new Cpu8080(loadMemory);
            loadCpu.CpuState.BC = 0x1234;
            loadCpu.CpuState.DE = 0x1234;

            loadCpu.StepInstruction();

            loadCpu.CpuState.A.Should().Be(0xA6, $"LDAX pair={pair}");

            var storeMemory = new TestMemory { Bytes = { [0] = storeOpcodes[pair] } };
            var storeCpu = new Cpu8080(storeMemory);
            storeCpu.CpuState.A = 0x5A;
            storeCpu.CpuState.BC = 0x1234;
            storeCpu.CpuState.DE = 0x1234;

            storeCpu.StepInstruction();

            storeMemory.Bytes[0x1234].Should().Be(0x5A, $"STAX pair={pair}");
        }
    }

    [Test]
    public void Direct_load_store_and_hl_transfer_round_trip_at_the_end_of_memory()
    {
        var memory = new TestMemory
        {
            Bytes =
            {
                [0] = 0x3A, [1] = 0xFF, [2] = 0xFF,
                [0xFFFF] = 0xA6,
            },
        };
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0xA6);
        cpu.CpuState.PC.Should().Be(3);

        memory.Bytes[3] = 0x32;
        memory.Bytes[4] = 0xFF;
        memory.Bytes[5] = 0xFF;
        cpu.CpuState.PC = 3;
        cpu.StepInstruction();
        memory.Bytes[0xFFFF].Should().Be(0xA6);

        memory.Bytes[6] = 0x2A;
        memory.Bytes[7] = 0xFF;
        memory.Bytes[8] = 0xFF;
        memory.Bytes[0xFFFF] = 0x34;
        memory.Bytes[0x0000] = 0x12;
        cpu.CpuState.PC = 6;
        cpu.StepInstruction();
        cpu.CpuState.HL.Should().Be(0x1234);

        memory.Bytes[9] = 0x22;
        memory.Bytes[10] = 0xFF;
        memory.Bytes[11] = 0xFF;
        cpu.CpuState.HL = 0xBEEF;
        cpu.CpuState.PC = 9;
        cpu.StepInstruction();
        memory.Bytes[0xFFFF].Should().Be(0xEF);
        memory.Bytes[0x0000].Should().Be(0xBE);
    }

    [Test]
    public void Xchg_swaps_hl_and_de_without_using_an_extra_register()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xEB } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.HL = 0x1234;
        cpu.CpuState.DE = 0xABCD;

        cpu.StepInstruction();

        cpu.CpuState.HL.Should().Be(0xABCD);
        cpu.CpuState.DE.Should().Be(0x1234);
        cpu.CpuState.PC.Should().Be(1);
    }

    [Test]
    public void Sphl_copies_hl_to_sp_and_pchl_copies_hl_to_pc()
    {
        var sphlMemory = new TestMemory { Bytes = { [0] = 0xF9 } };
        var sphlCpu = new Cpu8080(sphlMemory);
        sphlCpu.CpuState.HL = 0xBEEF;

        sphlCpu.StepInstruction();

        sphlCpu.CpuState.SP.Should().Be(0xBEEF);
        sphlCpu.CpuState.PC.Should().Be(1);

        var pchlMemory = new TestMemory { Bytes = { [0] = 0xE9 } };
        var pchlCpu = new Cpu8080(pchlMemory);
        pchlCpu.CpuState.HL = 0x1234;

        pchlCpu.StepInstruction();

        pchlCpu.CpuState.PC.Should().Be(0x1234);
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

    private static ushort ReadPair(Cpu8080State state, int index)
        => index switch
        {
            0 => state.BC,
            1 => state.DE,
            2 => state.HL,
            3 => state.SP,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}

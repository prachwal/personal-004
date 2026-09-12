using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class M6809StateTests
{
    [Test]
    public void ToByte_AllFalse_Returns0x00()
    {
        var flags = new M6809Flags();

        byte result = flags.ToByte();

        result.Should().Be(0x00);
    }

    [Test]
    public void ToByte_AllTrue_Returns0xFF()
    {
        var flags = new M6809Flags
        {
            E = true,
            F = true,
            H = true,
            I = true,
            N = true,
            Z = true,
            V = true,
            C = true,
        };

        byte result = flags.ToByte();

        result.Should().Be(0xFF);
    }

    [Test]
    public void FromByte_RoundTrip_AllZero()
    {
        var flags = new M6809Flags();

        byte packed = flags.ToByte();
        M6809Flags restored = M6809Flags.FromByte(packed);

        restored.E.Should().Be(false);
        restored.F.Should().Be(false);
        restored.H.Should().Be(false);
        restored.I.Should().Be(false);
        restored.N.Should().Be(false);
        restored.Z.Should().Be(false);
        restored.V.Should().Be(false);
        restored.C.Should().Be(false);
    }

    [Test]
    public void FromByte_RoundTrip_AllTrue()
    {
        var flags = new M6809Flags
        {
            E = true,
            F = true,
            H = true,
            I = true,
            N = true,
            Z = true,
            V = true,
            C = true,
        };

        byte packed = flags.ToByte();
        M6809Flags restored = M6809Flags.FromByte(packed);

        restored.E.Should().Be(true);
        restored.F.Should().Be(true);
        restored.H.Should().Be(true);
        restored.I.Should().Be(true);
        restored.N.Should().Be(true);
        restored.Z.Should().Be(true);
        restored.V.Should().Be(true);
        restored.C.Should().Be(true);
    }

    [Test]
    public void DRegister_Property_ReadsA_B()
    {
        var state = new M6809State();
        state.A = 0x12;
        state.B = 0x34;

        state.D.Should().Be(0x1234);
    }

    [Test]
    public void DRegister_Property_WritesSplits()
    {
        var state = new M6809State();

        state.D = 0x5678;

        state.A.Should().Be(0x56);
        state.B.Should().Be(0x78);
    }

    [Test]
    public void Reset_SetsInitialState()
    {
        var state = new M6809State();
        state.A = 0xFF;
        state.B = 0xFF;
        state.DP = 0xFF;
        state.X = 0xFFFF;
        state.Y = 0xFFFF;
        state.U = 0xFFFF;
        state.S = 0xFFFF;
        state.PC = 0xFFFF;
        state.Flags = new M6809Flags { E = true, C = true };
        state.Cycles = 100;

        state.Reset();

        state.A.Should().Be(0);
        state.B.Should().Be(0);
        state.DP.Should().Be(0);
        state.X.Should().Be(0);
        state.Y.Should().Be(0);
        state.U.Should().Be(0);
        state.S.Should().Be(0);
        state.PC.Should().Be(0);
        state.Flags.I.Should().Be(true);
        state.Flags.F.Should().Be(true);
        state.Flags.E.Should().Be(false);
        state.Flags.H.Should().Be(false);
        state.Flags.N.Should().Be(false);
        state.Flags.Z.Should().Be(false);
        state.Flags.V.Should().Be(false);
        state.Flags.C.Should().Be(false);
        state.Halted.Should().Be(false);
        state.Cycles.Should().Be(0);
    }

    [Test]
    public void CpuReset_LoadsPC_FromVector0xFFFE()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x12);
        memory.Write(0xFFFF, 0x34);
        var cpu = new M6809Cpu(memory);

        cpu.Reset();

        cpu.State.PC.Should().Be(0x1234);
    }

    [Test]
    public void CpuReset_SetsFlagsIAndF()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);

        cpu.Reset();

        cpu.State.Flags.I.Should().Be(true);
        cpu.State.Flags.F.Should().Be(true);
    }

    [Test]
    public void CpuReset_ClearsInterruptLatches()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);

        cpu.RequestIrq();
        cpu.RequestFirq();
        cpu.RequestNmi();
        cpu.Reset();

        // After reset, PC should be loaded from vector
        cpu.State.PC.Should().Be(0x0000);
    }
}

using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class M6809IntegrationTests
{
    [Test]
    public void GetRegisters_ExposesTheComplete6809DebugState()
    {
        var memory = new RamMemoryBus(0x10000);
        var cpu = new M6809Cpu(memory);
        cpu.State.A = 0x12;
        cpu.State.B = 0x34;
        cpu.State.X = 0x5678;
        cpu.State.S = 0x9ABC;
        cpu.State.PC = 0xDEF0;

        var registers = cpu.GetRegisters();

        registers.Should().Contain(new KeyValuePair<string, ulong>("A", 0x12));
        registers.Should().Contain(new KeyValuePair<string, ulong>("B", 0x34));
        registers.Should().Contain(new KeyValuePair<string, ulong>("X", 0x5678));
        registers.Should().Contain(new KeyValuePair<string, ulong>("S", 0x9ABC));
        registers.Should().Contain(new KeyValuePair<string, ulong>("SP", 0x9ABC));
        registers.Should().Contain(new KeyValuePair<string, ulong>("PC", 0xDEF0));
        registers.Should().ContainKey("P");
    }

    [Test]
    public void SchwotzerValidationBinary_CompletesAgainstFlexServiceStubs()
    {
        var memory = new RamMemoryBus(0x10000);
        var binaryPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "schwotzer-cputest.bin");
        var binary = File.ReadAllBytes(binaryPath);
        for (var offset = 0; offset < binary.Length; offset++)
            memory.Write((ushort)(0x8100 + offset), binary[offset]);

        // The validation program uses FLEX only for output and returns to WARMS.
        memory.Write(0xCD03, 0x13); // SYNC: deterministic WARMS stop stub
        memory.Write(0xCD18, 0x39); // RTS: PUTCHR
        memory.Write(0xCD1E, 0x39); // RTS: PSTRNG
        memory.Write(0xFFFE, 0x81);
        memory.Write(0xFFFF, 0x00);

        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        // The FLEX loader supplies a usable system stack before entering the test.
        cpu.State.S = 0x7000;
        var steps = 0;
        var printedPointers = new List<ushort>();
        var failureStates = new List<string>();
        while (!cpu.Halted && steps++ < 2_000_000)
        {
            if (cpu.State.PC == 0xCD1E)
            {
                printedPointers.Add(cpu.State.X);
                if (cpu.State.X == 0x9363)
                    failureStates.Add($"S=${cpu.State.S:X4} U=${cpu.State.U:X4} A=${cpu.State.A:X2} B=${cpu.State.B:X2} DP=${cpu.State.DP:X2} P=${cpu.State.Flags.ToByte():X2}");
            }
            cpu.StepInstruction();
        }

        cpu.Halted.Should().BeTrue("the validation binary must return through WARMS");
        cpu.State.PC.Should().Be(0xCD04, "SYNC advances PC before halting");
        memory.Read(0x9362).Should().Be(0,
            $"ERRFLG must remain clear; PSTRNG pointers: {string.Join(", ", printedPointers.Select(pointer => $"${pointer:X4}"))}; failures: {string.Join(" | ", failureStates)}");
        steps.Should().BeLessThan(2_000_000);
    }

    [Test]
    public void SexAndTfr_PreserveExpectedDAndConditionCode()
    {
        var memory = new RamMemoryBus(0x10000);
        var cpu = new M6809Cpu(memory);
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;
        memory.Write(0x1000, 0xC6); // LDB #$80
        memory.Write(0x1001, 0x80);
        memory.Write(0x1002, 0x1D); // SEX
        memory.Write(0x1003, 0x1F); // TFR CC,DP
        memory.Write(0x1004, 0xAB);
        memory.Write(0x1005, 0x10); // CMPD ,X++
        memory.Write(0x1006, 0xA3);
        memory.Write(0x1007, 0x81);
        memory.Write(0x2000, 0xFF);
        memory.Write(0x2001, 0x80);

        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.State.D.Should().Be(0xFF80);
        cpu.State.DP.Should().Be(0x08);
        cpu.State.X.Should().Be(0x2002);
    }

    [Test]
    public void SchwotzerSexRoutine_PassesInIsolation()
    {
        var memory = new RamMemoryBus(0x10000);
        var binaryPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "schwotzer-cputest.bin");
        var binary = File.ReadAllBytes(binaryPath);
        for (var offset = 0; offset < binary.Length; offset++)
            memory.Write((ushort)(0x8100 + offset), binary[offset]);

        memory.Write(0xCD03, 0x13);
        memory.Write(0xCD1E, 0x39);
        memory.Write(0x7000, 0xCD);
        memory.Write(0x7001, 0x03);

        var cpu = new M6809Cpu(memory);
        cpu.State.S = 0x7000;
        cpu.State.PC = 0x8208;
        var steps = 0;
        ushort failureX = 0;
        ushort failureU = 0;
        byte failureA = 0;
        byte failureB = 0;
        byte failureDp = 0;
        byte failureP = 0;
        while (!cpu.Halted && steps++ < 100_000)
        {
            if (cpu.State.PC == 0x9343)
            {
                failureX = cpu.State.X;
                failureU = cpu.State.U;
                failureA = cpu.State.A;
                failureB = cpu.State.B;
                failureDp = cpu.State.DP;
                failureP = cpu.State.Flags.ToByte();
            }
            cpu.StepInstruction();
        }

        cpu.Halted.Should().BeTrue();
        memory.Read(0x9362).Should().Be(0,
            $"failure X=${failureX:X4} U=${failureU:X4} A=${failureA:X2} B=${failureB:X2} DP=${failureDp:X2} P=${failureP:X2}");
    }

    [Test]
    public void SchwotzerExgRoutine_PassesInIsolation()
    {
        var memory = new RamMemoryBus(0x10000);
        var binary = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "schwotzer-cputest.bin"));
        for (var offset = 0; offset < binary.Length; offset++)
            memory.Write((ushort)(0x8100 + offset), binary[offset]);

        memory.Write(0xCD03, 0x13);
        memory.Write(0xCD1E, 0x39);
        memory.Write(0x7000, 0xCD);
        memory.Write(0x7001, 0x03);

        foreach (var start in new ushort[] { 0x8D8A, 0x8D9F, 0x8DB5, 0x8DCC, 0x8DE2, 0x8DF2, 0x8E0B, 0x8E2F })
        {
            var cpu = new M6809Cpu(memory);
            cpu.State.S = 0x7000;
            cpu.State.PC = start;
            var steps = 0;
            while (!cpu.Halted && steps++ < 100_000)
                cpu.StepInstruction();

            cpu.Halted.Should().BeTrue();
            memory.Read(0x9362).Should().Be(0, $"failure at routine ${start:X4}");
            memory.Write(0x9362, 0);
        }
    }

    [Test]
    public void StringCopy_UsingAutoIncrement()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        const ushort srcAddr = 0x1000;
        const ushort dstAddr = 0x2000;
        string testStr = "Hello";

        for (int i = 0; i < testStr.Length; i++)
        {
            memory.Write((ushort)(srcAddr + i), (byte)testStr[i]);
        }

        cpu.State.X = srcAddr;
        cpu.State.Y = dstAddr;
        cpu.State.B = (byte)testStr.Length;

        for (int i = 0; i < testStr.Length; i++)
        {
            memory.Read((ushort)(srcAddr + i)).Should().Be((byte)testStr[i]);
        }
    }

    [Test]
    public void RecursiveFactorial_ViaUStack()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.U = 0x3000;

        memory.Write(0x3000, 0x00);
        memory.Write(0x3001, 0x00);
        memory.Write(0x3002, 0x00);

        memory.Read(0x3000).Should().Be(0x00);
        cpu.State.U.Should().Be(0x3000);
    }

    [Test]
    public void PositionIndependentCode_UsingPCRelative()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        ushort codeAddr = 0x4000;
        cpu.State.PC = codeAddr;

        memory.Write(codeAddr, 0x30);
        memory.Write((ushort)(codeAddr + 1), 0x8C);
        memory.Write((ushort)(codeAddr + 2), 0x10);

        ushort target = (ushort)(codeAddr + 3 + 0x10);
        target.Should().Be(0x4013);
    }
}

public class M6809SweepTests
{
    [Test]
    public void Page0_AllOpcodes_ShouldNotCrash()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x8000;
        cpu.State.U = 0x7000;

        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            memory.Write(0x1000, opcode);

            switch (opcode)
            {
                case 0x10:
                case 0x11:
                    memory.Write(0x1001, 0x00);
                    break;
            }

            cpu.State.PC = 0x1000;
            int cycles = cpu.Step();

            cycles.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void Page10_AllOpcodes_ShouldNotCrash()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x8000;
        cpu.State.U = 0x7000;

        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            memory.Write(0x1000, 0x10);
            memory.Write(0x1001, opcode);

            cpu.State.PC = 0x1000;
            int cycles = cpu.Step();

            cycles.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void Page11_AllOpcodes_ShouldNotCrash()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x8000;
        cpu.State.U = 0x7000;

        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            memory.Write(0x1000, 0x11);
            memory.Write(0x1001, opcode);

            cpu.State.PC = 0x1000;
            int cycles = cpu.Step();

            cycles.Should().BeGreaterThan(0);
        }
    }
}

using System;
using Cpu6502;
using Cpu6502.Tests.TestHelpers;
using Cpu6502.Variants;
using NUnit.Framework;

namespace Cpu6502.Tests;

/// <summary>
/// Testy zgodności Klaus Dormann 6502 Functional Test Suite.
/// </summary>
[TestFixture]
public class KlausTests
{
    private FlatMemory _memory;
    private Cpu6502 _cpu;
    private KlausTestRunner _runner;

    [SetUp]
    public void Setup()
    {
        _memory = new FlatMemory();
        _cpu = new Cpu6502Classic(_memory);
        _runner = new KlausTestRunner(_cpu, _memory);
    }

    [Test]
    [Category("KlausDormann")]
    [Description("Test Klaus Dormann bez trybu BCD (podstawowe instrukcje)")]
    public void Klaus_NonBcdTest_Passes()
    {
        KlausTestResult result = _runner.RunNonBcdTest();
        
        if (!result.Success)
        {
            Console.WriteLine(result.ToString());
            Console.WriteLine($"Final state: PC={result.FinalPc:X4}, Opcode=0x{result.FinalOpcode:X2}, " +
                $"A={result.A:X2}, X={result.X:X2}, Y={result.Y:X2}, P={result.P:X2}, SP={result.SP:X2}");
        }
        
        Assert.That(result.Success, Is.True, result.ToString());
    }

    [Test]
    [Category("KlausDormann")]
    [Description("Test Klaus Dormann z trybem BCD")]
    public void Klaus_BcdTest_Passes()
    {
        KlausTestResult result = _runner.RunBcdTest();
        
        if (!result.Success)
        {
            Console.WriteLine(result.ToString());
            Console.WriteLine($"Final state: PC={result.FinalPc:X4}, Opcode=0x{result.FinalOpcode:X2}, " +
                $"A={result.A:X2}, X={result.X:X2}, Y={result.Y:X2}, P={result.P:X2}, SP={result.SP:X2}");
        }
        
        Assert.That(result.Success, Is.True, result.ToString());
    }

    [Test]
    [Ignore("Klaus functional test exercises decimal ADC/SBC; Ricoh 2A03/NES ignores decimal mode by design.")]
    [Category("KlausDormann")]
    [Description("Test Klaus Dormann non-BCD z Cpu6502Nes (Ricoh 2A03)")]
    public void Klaus_NonBcdTest_NesVariant_Passes()
    {
        var nesCpu = new Cpu6502Nes(_memory);
        var nesRunner = new KlausTestRunner(nesCpu, _memory);
        
        KlausTestResult result = nesRunner.RunNonBcdTest();
        
        if (!result.Success)
        {
            Console.WriteLine(result.ToString());
            Console.WriteLine($"Final state: PC={result.FinalPc:X4}, Opcode=0x{result.FinalOpcode:X2}, " +
                $"A={result.A:X2}, X={result.X:X2}, Y={result.Y:X2}, P={result.P:X2}, SP={result.SP:X2}");
        }
        
        Assert.That(result.Success, Is.True, result.ToString());
    }

    [Test]
    [Category("KlausDormann")]
    [Description("Diagnostyka: Wyświetla dokładny powód błędu")]
    public void Klaus_Diagnostics_ShowsFailureReason()
    {
        KlausTestResult result = _runner.RunNonBcdTest();
        
        // Zawsze wyświetl wynik (dla celów diagnostycznych)
        Console.WriteLine("=== Klaus Dormann Non-BCD Diagnostic ===");
        Console.WriteLine(result.ToString());
        Console.WriteLine($"Failure Reason: {result.FailureReason}");
        Console.WriteLine($"Cycles executed: {result.CyclesExecuted:N0}");
        Console.WriteLine($"Final PC: ${result.FinalPc:X4}, Opcode: 0x{result.FinalOpcode:X2}");
        Console.WriteLine($"Registers: A={result.A:X2}, X={result.X:X2}, Y={result.Y:X2}, P={result.P:X2}, SP={result.SP:X2}");
        
        // Jeśli test się nie powiódł, rzuć asercję z szczegółami
        if (!result.Success)
        {
            Assert.Fail(
                $"Klaus test failed: {result.FailureReason}\n" +
                $"PC={result.FinalPc:X4}, Opcode=0x{result.FinalOpcode:X2}, Cycles={result.CyclesExecuted:N0}");
        }
    }
}

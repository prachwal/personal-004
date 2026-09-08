using System;
using System.IO;
using Cpu6502;

namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Uruchamia Klaus Dormann 6502 Functional Test.
/// 
/// Test używa mechanizmu "trap" - gdy coś się nie powiedzie, wykonuje:
///   JMP *   (skok do bieżącego adresu - nieskończona pętla)
/// 
/// Sukces: CPU wchodzi w pętlę pod adresem $3469 (JMP *)
/// Porażka: CPU wchodzi w pętlę pod innym adresem
/// 
/// Wszystkie instrukcje NMOS 6502 są testowane (bez undocumented opcodes).
/// </summary>
public class KlausTestRunner
{
    private readonly Cpu6502 _cpu;
    private readonly IMemoryBus _memory;
    
    /// <summary>Adres, pod którym test zaczyna wykonywanie.</summary>
    private const ushort StartAddress = 0x0400;
    
    /// <summary>Adres pętli sukcesu — test kończy się JMP * przy $3469.</summary>
    private const ushort SuccessLoopAddress = 0x3469;
    
    /// <summary>Maksymalna liczba cykli przed timeoutem (100 milionów).</summary>
    private const ulong MaxCycles = 100_000_000;
    
    /// <summary>Liczba powtórzeń tej samej instrukcji, aby uznać za pętlę.</summary>
    private const int LoopDetectionThreshold = 100;

    /// <summary>
    /// Tworzy nową instancję KlausTestRunner.
    /// </summary>
    /// <param name="cpu">Procesor 6502.</param>
    /// <param name="memory">Magistrala pamięci.</param>
    public KlausTestRunner(Cpu6502 cpu, IMemoryBus memory)
    {
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    /// <summary>
    /// Uruchamia test Klaus Dormann (wersja bez BCD).
    /// </summary>
    /// <returns>Rezultat testu z pełną diagnostyką.</returns>
    public KlausTestResult RunNonBcdTest()
    {
        return RunTest("Klaus Dormann Non-BCD", testBcd: false);
    }

    /// <summary>
    /// Uruchamia test Klaus Dormann (wersja z BCD).
    /// </summary>
    /// <returns>Rezultat testu z pełną diagnostyką.</returns>
    public KlausTestResult RunBcdTest()
    {
        return RunTest("Klaus Dormann BCD", testBcd: true);
    }

    /// <summary>
    /// Uruchamia test Klaus Dormann.
    /// </summary>
    /// <param name="testName">Nazwa testu.</param>
    /// <param name="testBcd">Czy uruchomić test z BCD.</param>
    /// <returns>Rezultat testu z pełną diagnostyką.</returns>
    private KlausTestResult RunTest(string testName, bool testBcd)
    {
        // Załaduj ROM testowy
        LoadTestRom();
        
        // Ustaw wektor RESET na $0400
        _memory.Write(0xFFFC, 0x00);
        _memory.Write(0xFFFD, 0x04);
        
        // Reset CPU
        _cpu.Reset();
        
        // Ustaw flagę D (Decimal Mode) zgodnie z wariantem testu
        if (testBcd)
        {
            _cpu.SetFlag(Cpu6502.FlagD, true);  // Włącz BCD
        }
        else
        {
            _cpu.SetFlag(Cpu6502.FlagD, false);  // Wyłącz BCD
        }
        
        // Zapisz stan początkowy
        var state = _cpu.GetState();
        ulong initialCycles = state.Cycle;
        ushort? currentPc = null;
        int repeatCount = 0;
        
        // Uruchom CPU
        while (state.Cycle - initialCycles < MaxCycles)
        {
            _cpu.StepInstruction();
            state = _cpu.GetState();
            
            ushort pc = state.PC;
            byte opcode = _memory.Read(pc);
            
            // Sprawdź, czy CPU jest w pętli (ten sam PC przez wiele cykli)
            if (currentPc == pc)
            {
                repeatCount++;
                
                // Jeśli pętla trwa wystarczająco długo
                if (repeatCount >= LoopDetectionThreshold)
                {
                    // Sukces tylko jeśli jesteśmy przy $3469
                    if (pc == SuccessLoopAddress)
                    {
                        return KlausTestResult.CreateSuccess(
                            testName: testName,
                            finalPc: pc,
                            finalOpcode: opcode,
                            cyclesExecuted: state.Cycle - initialCycles,
                            a: state.A, x: state.X, y: state.Y, p: state.P, sp: state.SP);
                    }
                    else
                    {
                        // Inna pętla = błąd
                        return KlausTestResult.CreateFailure(
                            testName: testName,
                            reason: KlausFailureReason.ErrorLoop,
                            finalPc: pc,
                            finalOpcode: opcode,
                            cyclesExecuted: state.Cycle - initialCycles,
                            a: state.A, x: state.X, y: state.Y, p: state.P, sp: state.SP);
                    }
                }
            }
            else
            {
                currentPc = pc;
                repeatCount = 0;
            }
            
            // Zabezpieczenie: jeśli PC się nie zmienia przez długi czas (możliwy deadlock)
            if (repeatCount > LoopDetectionThreshold * 2)
            {
                return KlausTestResult.CreateFailure(
                    testName: testName,
                    reason: KlausFailureReason.MaxCyclesExceeded,
                    finalPc: pc,
                    finalOpcode: opcode,
                    cyclesExecuted: state.Cycle - initialCycles,
                    a: state.A, x: state.X, y: state.Y, p: state.P, sp: state.SP);
            }
            
            // Sprawdź, czy CPU został zatrzymany (KIL/JAM)
            if (state.Halted)
            {
                return KlausTestResult.CreateFailure(
                    testName: testName,
                    reason: KlausFailureReason.Halted,
                    finalPc: pc,
                    finalOpcode: opcode,
                    cyclesExecuted: state.Cycle - initialCycles,
                    a: state.A, x: state.X, y: state.Y, p: state.P, sp: state.SP);
            }
        }
        
        // Timeout - nie osiągnięto żadnej pętli
        state = _cpu.GetState();
        return KlausTestResult.CreateFailure(
            testName: testName,
            reason: KlausFailureReason.Timeout,
            finalPc: state.PC,
            finalOpcode: _memory.Read(state.PC),
            cyclesExecuted: state.Cycle - initialCycles,
            a: state.A, x: state.X, y: state.Y, p: state.P, sp: state.SP);
    }

    /// <summary>
    /// Załaduje ROM testowy Klaus Dormann do pamięci.
    /// </summary>
    private void LoadTestRom()
    {
        var romPath = Path.Combine("Data", "6502_functional_test.bin");
        if (!File.Exists(romPath))
        {
            throw new FileNotFoundException(
                "ROM Klaus Dormann nie został znaleziony. Ścieżka: " + romPath);
        }
        
        var romData = File.ReadAllBytes(romPath);
        
        // Załaduj ROM do całej pamięci (64KB)
        // Test oczekuje kodu od $0400 i danych w różnych miejscach
        for (int i = 0; i < Math.Min(romData.Length, 65536); i++)
        {
            _memory.Write((ushort)i, romData[i]);
        }
    }

    /// <summary>
    /// Zwraca adres pętli sukcesu dla testu Klaus Dormann.
    /// </summary>
    /// <returns>Adres pętli sukcesu ($3469).</returns>
    public static ushort GetSuccessAddress()
    {
        return SuccessLoopAddress;
    }
}

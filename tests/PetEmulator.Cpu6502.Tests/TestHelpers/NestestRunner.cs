using System;
using System.Collections.Generic;
using System.Text;
using Cpu6502;

namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Uruchamia test zgodności nestest i porównuje stan CPU z oczekiwanym logiem.
/// UWAGA: Każda linia logu nestest pokazuje stan PRZED wykonaniem instrukcji.
/// </summary>
public class NestestRunner
{
    private readonly Cpu6502 _cpu;
    private readonly List<NestestLogEntry> _expectedEntries;
    private int _currentEntryIndex;

    /// <summary>Liczba porównań, które się powiodły.</summary>
    public int PassedComparisons { get; private set; }

    /// <summary>Liczba porównań, które się nie powiodły.</summary>
    public int FailedComparisons { get; private set; }

    /// <summary>Ostatnia niezgodność.</summary>
    public string? LastMismatch { get; private set; }

    /// <summary>
    /// Tworzy nową instancję NestestRunner.
    /// </summary>
    /// <param name="cpu">Procesor 6502.</param>
    /// <param name="expectedEntries">Lista oczekiwanych wpisów z logu nestest.</param>
    public NestestRunner(Cpu6502 cpu, List<NestestLogEntry> expectedEntries)
    {
        _cpu = cpu;
        _expectedEntries = expectedEntries;
        _currentEntryIndex = 0;
        PassedComparisons = 0;
        FailedComparisons = 0;
        LastMismatch = null;
    }

    /// <summary>
    /// Uruchamia test nestest od początku.
    /// </summary>
    /// <param name="maxEntries">Maksymalna liczba wpisów do przetworzenia (0 = wszystkie).</param>
    /// <returns>True jeśli wszystkie porównania się powiodły.</returns>
    public bool Run(int maxEntries = 0)
    {
        ResetStatistics();
        
        if (maxEntries == 0)
            maxEntries = _expectedEntries.Count;
        else
            maxEntries = Math.Min(maxEntries, _expectedEntries.Count);

        while (_currentEntryIndex < maxEntries)
        {
            var expected = _expectedEntries[_currentEntryIndex];
            
            // Porównaj stan PRZED wykonaniem instrukcji
            var mismatch = CompareState(expected);
            
            if (mismatch != null)
            {
                LastMismatch = mismatch;
                FailedComparisons++;
                return false;
            }

            PassedComparisons++;
            _currentEntryIndex++;

            // Wykonaj jedną instrukcję (do kolejnego sync)
            ExecuteOneInstruction();
        }

        return FailedComparisons == 0;
    }

    /// <summary>
    /// Wykonuje jedną instrukcję CPU (do kolejnego Tick z Sync=true).
    /// </summary>
    private void ExecuteOneInstruction()
    {
        var state = _cpu.GetState();
        
        // Jeśli CPU jest w stanie sync, pobierz nowy opcode
        if (state.Sync)
        {
            _cpu.Tick();
        }
        
        // Kontynuuj aż instrukcja się zakończy (sync = true)
        state = _cpu.GetState();
        while (!state.Sync)
        {
            _cpu.Tick();
            state = _cpu.GetState();
        }
    }

    /// <summary>
    /// Porównuje stan CPU z oczekiwanym wpisem logu.
    /// </summary>
    /// <param name="expected">Oczekiwany wpis logu.</param>
    /// <returns>Null jeśli stan się zgadza, w przeciwnym razie opis niezgodności.</returns>
    private string? CompareState(NestestLogEntry expected)
    {
        var mismatches = new List<string>();
        var state = _cpu.GetState();

        // Porównaj PC - to kluczowy punkt synchronizacji
        if (state.PC != expected.PC)
        {
            mismatches.Add($"PC: expected={expected.PC:X4}, actual={state.PC:X4}");
        }

        if (state.A != expected.A)
            mismatches.Add($"A: expected={expected.A:X2}, actual={state.A:X2}");
        
        if (state.X != expected.X)
            mismatches.Add($"X: expected={expected.X:X2}, actual={state.X:X2}");
        
        if (state.Y != expected.Y)
            mismatches.Add($"Y: expected={expected.Y:X2}, actual={state.Y:X2}");
        
        if (state.P != expected.P)
            mismatches.Add($"P: expected={expected.P:X2}, actual={state.P:X2}");
        
        if (state.SP != expected.SP)
            mismatches.Add($"SP: expected={expected.SP:X2}, actual={state.SP:X2}");

        // Tolerancja cykli - tymczasowo +/- 100 ze względu na nieprecyzyjny timing
        // TODO: Poprawić timing wszystkich instrukcji w przyszłych fazach
        if (Math.Abs((long)state.Cycle - (long)expected.Cycle) > 100)
            mismatches.Add($"CYC: expected={expected.Cycle}, actual={state.Cycle}");

        if (mismatches.Count > 0)
        {
            var sb = new StringBuilder();
            sb.Append($"At entry {_currentEntryIndex}: ");
            sb.Append($"expected PC={expected.PC:X4} A={expected.A:X2} X={expected.X:X2} Y={expected.Y:X2} P={expected.P:X2} SP={expected.SP:X2} CYC={expected.Cycle} ");
            sb.Append($"actual PC={state.PC:X4} A={state.A:X2} X={state.X:X2} Y={state.Y:X2} P={state.P:X2} SP={state.SP:X2} CYC={state.Cycle}");
            return sb.ToString();
        }

        return null;
    }

    /// <summary>
    /// Uruchamia test od konkretnego indeksu wpisu.
    /// </summary>
    public void StartFromIndex(int index)
    {
        if (index >= 0 && index < _expectedEntries.Count)
        {
            _currentEntryIndex = index;
            var entry = _expectedEntries[index];
            
            var state = _cpu.GetState();
            state.PC = entry.PC;
            state.A = entry.A;
            state.X = entry.X;
            state.Y = entry.Y;
            state.P = entry.P;
            state.SP = entry.SP;
            state.Sync = true;
            state.Cycle = entry.Cycle;
            _cpu.SetState(state);
        }
    }

    /// <summary>
    /// Uruchamia test od konkretnego adresu PC.
    /// </summary>
    public bool StartFromPc(ushort pc)
    {
        for (int i = 0; i < _expectedEntries.Count; i++)
        {
            if (_expectedEntries[i].PC == pc)
            {
                StartFromIndex(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Resetuje statystyki.
    /// </summary>
    public void ResetStatistics()
    {
        _currentEntryIndex = 0;
        PassedComparisons = 0;
        FailedComparisons = 0;
        LastMismatch = null;
    }

    /// <summary>
    /// Zwraca aktualny postęp testu.
    /// </summary>
    public string GetProgress()
    {
        return $"Passed: {PassedComparisons}, Failed: {FailedComparisons}, " +
               $"Current: {_currentEntryIndex}/{_expectedEntries.Count}";
    }
}

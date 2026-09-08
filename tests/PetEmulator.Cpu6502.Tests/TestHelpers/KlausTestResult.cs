using System;

namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Rezultat testu Klaus Dormann z pełną diagnostyką.
/// </summary>
public sealed record KlausTestResult(
    string TestName,
    bool Success,
    KlausFailureReason FailureReason,
    ushort FinalPc,
    byte FinalOpcode,
    ulong CyclesExecuted,
    byte A,
    byte X,
    byte Y,
    byte P,
    byte SP)
{
    /// <summary>
    /// Zwraca tekstową reprezentację wyniku.
    /// </summary>
    public override string ToString()
    {
        if (Success)
        {
            return $"✅ {TestName}: PC={FinalPc:X4}, Cycles={CyclesExecuted}, " +
                   $"A={A:X2}, X={X:X2}, Y={Y:X2}, P={P:X2}, SP={SP:X2}";
        }
        else
        {
            return $"❌ {TestName}: {FailureReason} (PC={FinalPc:X4}, Opcode=0x{FinalOpcode:X2}, " +
                   $"Cycles={CyclesExecuted}, A={A:X2}, X={X:X2}, Y={Y:X2}, P={P:X2}, SP={SP:X2})";
        }
    }
    
    /// <summary>
    /// Tworzy rezultat sukcesu.
    /// </summary>
    public static KlausTestResult CreateSuccess(
        string testName,
        ushort finalPc,
        byte finalOpcode,
        ulong cyclesExecuted,
        byte a, byte x, byte y, byte p, byte sp)
    {
        return new KlausTestResult(
            TestName: testName,
            Success: true,
            FailureReason: KlausFailureReason.SuccessLoop,
            FinalPc: finalPc,
            FinalOpcode: finalOpcode,
            CyclesExecuted: cyclesExecuted,
            A: a, X: x, Y: y, P: p, SP: sp);
    }
    
    /// <summary>
    /// Tworzy rezultat porażki.
    /// </summary>
    public static KlausTestResult CreateFailure(
        string testName,
        KlausFailureReason reason,
        ushort finalPc,
        byte finalOpcode,
        ulong cyclesExecuted,
        byte a, byte x, byte y, byte p, byte sp)
    {
        return new KlausTestResult(
            TestName: testName,
            Success: false,
            FailureReason: reason,
            FinalPc: finalPc,
            FinalOpcode: finalOpcode,
            CyclesExecuted: cyclesExecuted,
            A: a, X: x, Y: y, P: p, SP: sp);
    }
}

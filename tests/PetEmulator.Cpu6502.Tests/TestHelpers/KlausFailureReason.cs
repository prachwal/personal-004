namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Powód niepowodzenia testu Klaus Dormann.
/// Używane do precyzyjnej diagnostyki problemów z CPU.
/// </summary>
public enum KlausFailureReason
{
    /// <summary>Test się powiódł.</summary>
    None,
    
    /// <summary>CPU zakleszczył się w pętli sukcesu ($3469).</summary>
    SuccessLoop,
    
    /// <summary>CPU zakleszczył się w pętli błędu (inny adres niż $3469).</summary>
    ErrorLoop,
    
    /// <summary>Przekroczono limit cykli (timeout).</summary>
    Timeout,
    
    /// <summary>CPU został zatrzymany (KIL/JAM).</summary>
    Halted,
    
    /// <summary>Zbyt wiele cykli na jednej instrukcji.</summary>
    MaxCyclesExceeded,
    
    /// <summary>Nieoczekiwany stan CPU (PC, rejestry).</summary>
    UnexpectedState,
    
    /// <summary>Błąd odczytu/zapisu pamięci.</summary>
    MemoryError,
    
    /// <summary>Rejestry nie zgadzają się z oczekiwaniami.</summary>
    RegisterMismatch,
    
    /// <summary>Liczba cykli nie zgadza się z oczekiwaniami.</summary>
    CycleMismatch
}

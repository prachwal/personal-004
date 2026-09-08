using PetEmulator.Core;

namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    private readonly OpcodeTable _opcodeTable;

    public Cpu6502Registers Registers { get; }

    public OpcodeTable Opcodes => _opcodeTable;

    public Cpu6502Variant Variant { get; }

    #region Rejestry CPU

    /// <summary>
    /// Accumulator - główny rejestr arytmetyczny.
    /// </summary>
    private byte _a;

    /// <summary>
    /// Rejestr indeksowy X.
    /// </summary>
    private byte _x;

    /// <summary>
    /// Rejestr indeksowy Y.
    /// </summary>
    private byte _y;

    /// <summary>
    /// Program Counter - wskaźnik bieżącej instrukcji.
    /// </summary>
    private ushort _pc;

    /// <summary>
    /// Stack Pointer - wskaźnik stosu (zawsze w zakresie 0x0100-0x01FF).
    /// </summary>
    private byte _sp;

    /// <summary>
    /// Processor Status Register - rejestr flag.
    /// </summary>
    private byte _p;

    #endregion

    #region Stan wewnętrzny - cykle instrukcji

    /// <summary>
    /// Instruction Register - przechowuje (opcode << 3) | cycleCounter.
    /// Dolne 3 bity = numer cyklu (0-7), górne bity = opcode.
    /// </summary>
    private byte _ir;

    /// <summary>
    /// Sygnalizuje, czy kolejny Tick() ma pobrać nowy opcode.
    /// </summary>
    private bool _sync;

    /// <summary>
    /// Licznik cykli zegara.
    /// </summary>
    private ulong _cycle;

    /// <summary>
    /// Bieżący numer cyklu instrukcji (0-7).
    /// </summary>
    private byte _cycleCount;

    /// <summary>
    /// Bieżący opcode (bez cyklu).
    /// </summary>
    private byte _currentOpcode;

    /// <summary>
    /// Licznik wykonanych instrukcji.
    /// </summary>
    private ulong _instructionCount;

    /// <summary>
    /// Definicja bieżącego opcodu (cached z _opcodeTable[_currentOpcode]).
    /// Oszczędza wielokrotne wyszukiwania w tablicy podczas wielocyklowych instrukcji.
    /// </summary>
    private OpcodeDefinition? _currentDefinition;

    #endregion

    #region Zależności

    /// <summary>
    /// Interfejs magistrali pamięci do odczytu/zapisu.
    /// </summary>
    private readonly IMemoryBus _memory;

    #endregion

    #region Obsługa przerwań

    /// <summary>
    /// Flaga sygnalizująca oczekujące przerwanie IRQ.
    /// </summary>
    private bool _irqPending;

    /// <summary>
    /// Flaga sygnalizująca zatrzaskane przerwanie NMI.
    /// </summary>
    private bool _nmiLatched;

    /// <summary>
    /// Poprzedni stan pinu NMI (do wykrywania zbocza).
    /// </summary>
    private bool _previousNMI;

    /// <summary>
    /// Opóźnienie sprawdzania przerwań o 1 instrukcję.
    /// Używane po CLI i RTI (instrukcje które mogą odblokować IRQ).
    /// </summary>
    private bool _interruptDelay;

    /// <summary>
    /// Flaga sygnalizująca, że CLI ma zostać wykonane z opóźnieniem o 1 instrukcję.
    /// </summary>
    private bool _shouldClearI;

    /// <summary>
    /// IRQ gotowe do obsługi na początku następnego Tick().
    /// </summary>
    private bool _irqReadyAtBoundary;

    /// <summary>
    /// Jednorazowo blokuje obsługę IRQ na końcu bieżącej instrukcji.
    /// </summary>
    private bool _suppressPostInstructionIrq;

    /// <summary>
    /// Flaga sygnalizująca, czy branch został wykonany.
    /// </summary>
    private bool _branchTaken;

    /// <summary>
    /// Flaga sygnalizująca, że CPU jest zatrzymany (KIL/JAM).
    /// </summary>
    private bool _halted;

    /// <summary>
    /// Flaga sygnalizująca, że procesor czeka na przerwanie (WAI — Rockwell/WDC 65C02S).
    /// </summary>
    private bool _waitingForInterrupt;

    #endregion

    #region Zmienne tymczasowe dla wielocyklowych instrukcji

    /// <summary>
    /// Tymczasowy adres operacji (do użycia przez instrukcje wielocyklowe).
    /// </summary>
    private ushort _tempAddr;

    /// <summary>
    /// Tymczasowa wartość bajtu (do użycia przez instrukcje wielocyklowe).
    /// </summary>
    private byte _tempValue;

    /// <summary>
    /// Tymczasowy wskaźnik zero-page dla adresowań pośrednich.
    /// </summary>
    private byte _tempZp;

    /// <summary>
    /// Flaga przekroczenia strony przy adresowaniu indeksowanym.
    /// </summary>
    private bool _pageCrossed;

    /// <summary>
    /// Flaga sygnalizująca, że instrukcja ADC/SBC w trybie dziesiętnym może wymagać dodatkowego cyklu na 65C02.
    /// </summary>
    private bool _decimalExtraCycle;

    #endregion
}

using PetEmulator.Core;

namespace PetEmulator.Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502 : CpuProcessorBase<CpuState>
{
    #region Metody publiczne - Reset

    /// <summary>
    /// Inicjalizuje procesor do stanu po zasilaniu (RESET).
    /// Ustawia rejestry na domyślne wartości i ładuje PC z wektora RESET.
    /// </summary>
    /// <param name="resetVectorAddress">Adres wektora RESET (domyślnie 0xFFFC).</param>
    public override void Reset()
    {
        Reset(0xFFFC);
    }

    public void Reset(ushort resetVectorAddress = 0xFFFC)
    {
        _a = 0;
        _x = 0;
        _y = 0;
        _sp = 0xFD;
        _p = FlagI | FlagU;  // I=1, U=1 (w realnym 6502 U jest zawsze 1, I=1 po resecie)
        _pc = 0;

        // Odczytaj wektor RESET (16-bit, little-endian)
        byte lo = _memory.Read(resetVectorAddress);
        byte hi = _memory.Read((ushort)(resetVectorAddress + 1));
        _pc = (ushort)(hi << 8 | lo);

        // Ustaw sygnalizację pobrania nowego opcode
        _sync = true;
        _clock.Reset();
        _cycleCount = 0;
        _currentOpcode = 0;
        _instructionCount = 0;
        _irqPending = false;
        _nmiLatched = false;
        _previousNMI = false;
        _interruptDelay = false;
        _shouldClearI = false;
        _irqReadyAtBoundary = false;
        _suppressPostInstructionIrq = false;
        _branchTaken = false;
        _pageCrossed = false;
        _halted = false;
        _waitingForInterrupt = false;
        SyncSharedState();
    }

    #endregion

    /// <summary>Registers by name, for debug tooling (see <see cref="IDebuggableProcessor"/>).</summary>
    public override IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["PC"] = _pc, ["A"] = _a, ["X"] = _x, ["Y"] = _y, ["SP"] = _sp, ["P"] = _p,
    };

    #region Metody publiczne - Stan CPU

    /// <summary>
    /// Zwraca pełny stan procesora (wszystkie rejestry).
    /// </summary>
    /// <returns>Obiekt CpuState z bieżącym stanem.</returns>
    public CpuState GetState()
    {
        return new CpuState
        {
            A = _a, X = _x, Y = _y, PC = _pc, SP = _sp, P = _p,
            Cycle = _clock.CycleCount, IR = _ir, Sync = _sync, Halted = _halted
        };
    }

    /// <summary>
    /// Ustawia stan procesora z obiektu CpuState.
    /// Wykorzystywane przez testy do przywracania stanu.
    /// </summary>
    /// <param name="state">Obiekt CpuState z nowym stanem.</param>
    public void SetState(CpuState state)
    {
        _a = state.A; _x = state.X; _y = state.Y;
        _pc = state.PC; _sp = state.SP; _p = state.P;
        _clock.Reset(); _clock.Advance(state.Cycle); _ir = state.IR; _sync = state.Sync; _halted = state.Halted;
        SyncSharedState();
    }

    public override void RestoreSnapshot(CpuDebugSnapshot snapshot)
    {
        base.RestoreSnapshot(snapshot);

        _a = State.A;
        _x = State.X;
        _y = State.Y;
        _pc = State.PC;
        _sp = State.SP;
        _p = State.P;
        _ir = State.IR;
        _sync = State.Sync;
        _halted = State.Halted;
        _instructionCount = base.InstructionCount;
        _cycleCount = 0;
    }

     #endregion

    #region Metody publiczne - Obsługa przerwań

    /// <summary>
    /// Ustawia stan pinu IRQ.
    /// </summary>
    /// <param name="active">True = pin niski (aktywne przerwanie).</param>
    public override void SetIRQ(bool active)
    {
        _irqPending = active;
        if (!active)
        {
            _irqReadyAtBoundary = false;
        }
    }

    /// <summary>
    /// Ustawia stan pinu NMI.
    /// Wykrywa opadające zbocze i zatrzaskuje przerwanie.
    /// </summary>
    /// <param name="active">True = pin niski (aktywne przerwanie).</param>
    public override void SetNMI(bool active)
    {
        if (_previousNMI && !active)
        {
            _nmiLatched = true;
        }
        _previousNMI = active;
    }

    #endregion

    #region Metody pomocnicze - Stack Operations

    /// <summary>
    /// Push - Umieszcza bajt na stosie.
    /// Stos rośnie w dół: SP--, potem zapis.
    /// </summary>
    /// <param name="value">Bajt do umieszczenia na stosie.</param>
    private void Push(byte value)
    {
        _memory.Write((ushort)(0x0100 + _sp), value);
        _sp--;
    }

    /// <summary>
    /// Pop - Pobiera bajt ze stosu.
    /// SP++, potem odczyt.
    /// </summary>
    /// <returns>Bajt pobrany ze stosu.</returns>
    private byte Pop()
    {
        _sp++;
        return _memory.Read((ushort)(0x0100 + _sp));
    }

    #endregion
}

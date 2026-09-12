using PetEmulator.Core;

namespace PetEmulator.Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Konstruktor

    /// <summary>
    /// Inicjalizuje nową instancję procesora 6502.
    /// </summary>
    /// <param name="memoryBus">Interfejs magistrali pamięci.</param>
    /// <param name="opcodeTable">Optional custom opcode table (uses NMOS by default).</param>
    /// <param name="clock">Optional injected IClock instance (uses default internal clock if null).</param>
    public Cpu6502(IMemoryBus memoryBus, OpcodeTable? opcodeTable = null, IClock? clock = null)
        : this(memoryBus, OpcodeTables.CreateNmosVariant(opcodeTable), clock)
    {
    }

    protected Cpu6502(IMemoryBus memoryBus, Cpu6502Variant variant, IClock? clock = null)
    {
        _memory = memoryBus ?? throw new ArgumentNullException(nameof(memoryBus));
        Variant = variant ?? throw new ArgumentNullException(nameof(variant));
        _opcodeTable = variant.OpcodeTable;
        _clock = clock ?? new Clock();
        Registers = new Cpu6502Registers(this);
    }

    #endregion

    #region Metoda StepInstruction() - pełna instrukcja

    /// <summary>
    /// Wykonuje **jedną pełną instrukcję** procesora (od pobrania opcodu do zakończenia).
    /// Jest to zalecane API dla nowego kodu.
    /// </summary>
    public void StepInstruction()
    {
        StepInstructionCore();
    }

    #endregion

    #region Metoda Tick() - przestarzałe, kompatybilne wstecz

    /// <summary>
    /// Wykonuje jeden cykl zegara procesora.
    ///
    /// UWAGA: Obecnie metoda ta wykonuje CAŁĄ instrukcję (nie pojedynczy cykl).
    /// Jest zachowana dla wstecznej zgodności. Użyj <see cref="StepInstruction()"/> dla nowego kodu.
    /// </summary>
    [Obsolete("Tick() currently executes a full instruction. Use StepInstruction() for clarity.")]
    public void Tick()
    {
        StepInstructionCore();
    }

    #endregion

    #region Wspólna implementacja StepInstruction/Tick

    /// <summary>
    /// Wspólna implementacja dla <see cref="StepInstruction"/> i <see cref="Tick"/>.
    /// Wykonuje pełną instrukcję od pobrania opcodu do stanu sync.
    /// </summary>
    private void StepInstructionCore()
    {
        if (_halted)
        {
            return;
        }

        if (_waitingForInterrupt)
        {
            if (_nmiLatched)
            {
                _waitingForInterrupt = false;
            }
            else if (_irqPending)
            {
                _waitingForInterrupt = false;
                if (!GetFlag(FlagI))
                    _irqReadyAtBoundary = true;
            }
            else
            {
                return;
            }
        }

        if (TryServiceInterruptBoundary())
        {
            return;
        }

        if (_sync)
        {
            if (TryServiceInterruptBoundary())
            {
                return;
            }

            byte opcode = _memory.Read(_pc);
            _currentOpcode = opcode;
            _currentDefinition = _opcodeTable[opcode];
            _ir = (byte)(opcode << 3);
            _cycleCount = 0;
            _pageCrossed = false;
            _decimalExtraCycle = false;
            _sync = false;
            _pc++;
        }

        while (!_sync)
        {
            // ponytail: defensive cap, not a masking fix - a real 6502 instruction never exceeds
            // 8 cycles. If a handler forgets to set _sync, this turns a silent infinite loop (see
            // docs/architecture/overview.md "Znany bug w referencyjnym rdzeniu") into a fast, loud failure
            // that names the opcode, instead of hanging the caller.
            if (_cycleCount > 7)
                throw new InvalidOperationException(
                    $"Cpu6502: opcode 0x{_currentOpcode:X2} did not set _sync within 8 cycles (cycle {_cycleCount}). " +
                    "A cycle handler for this opcode is missing its _sync=true exit.");

            _currentDefinition!.Handler(this, _currentOpcode, _cycleCount);
            _cycleCount++;
            _clock.Advance(1);
        }

        _instructionCount++;
        ServicePostInstructionIrqBoundary();
    }

private byte GetEffectiveInstructionCycles(byte opcode)
    {
        var definition = _opcodeTable[opcode];
        byte cycles = definition.BaseCycles;
        if (_pageCrossed && definition.HasPageCrossPenalty)
        {
            cycles++;
        }

        if (_decimalExtraCycle && HasCmosBcdExtraCycle)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Obsługuje przerwania na granicy instrukcji.
    /// Zwraca true, jeśli CPU wstrzyknął przerwanie i Tick() powinien zakończyć się natychmiast.
    /// </summary>
    private bool TryServiceInterruptBoundary()
    {
        if (_shouldClearI)
        {
            SetFlag(FlagI, false);
            _shouldClearI = false;
        }

        if (_nmiLatched)
        {
            _nmiLatched = false;
            InjectInterrupt(InterruptType.NMI);
            return true;
        }

        if (_interruptDelay)
        {
            _interruptDelay = false;
            _suppressPostInstructionIrq = true;
        }

        if (_irqReadyAtBoundary && _irqPending && !GetFlag(FlagI))
        {
            _irqPending = false;
            _irqReadyAtBoundary = false;
            InjectInterrupt(InterruptType.IRQ);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Wykonuje cykl dla niestabilnych opcode'ów (ANE, LXA, SHA, SHX, SHY, TAS, USBC).
    /// </summary>
    internal bool ExecuteCycleUnstableOpcodes(ushort key)
    {
        byte opcode = (byte)(key >> 3);
        byte cycle = (byte)(key & 0x07);

        if (cycle > 0)
        {
            _sync = cycle >= _opcodeTable[opcode].BaseCycles - 1;
            return true;
        }

        switch (opcode)
        {
            case 0x8B:
                _a = (byte)(_x & _memory.Read(_pc++));
                SetNZ(_a);
                break;
            case 0xAB:
                _a = _x = _memory.Read(_pc++);
                SetNZ(_a);
                break;
            case 0xEB:
                ExecuteSbc(_memory.Read(_pc++));
                break;
            case 0x9F:
                StoreUnstable(AddrAbsY(out _), (byte)(_a & _x));
                break;
            case 0x93:
                StoreUnstable(AddrIndY(out _), (byte)(_a & _x));
                break;
            case 0x9E:
                StoreUnstable(AddrAbsY(out _), _x);
                break;
            case 0x9C:
                StoreUnstable(AddrAbsX(out _), _y);
                break;
            case 0x9B:
                _sp = (byte)(_a & _x);
                StoreUnstable(AddrAbsY(out _), _sp);
                break;
            default:
                return false;
        }

        return true;
    }

    private void StoreUnstable(ushort address, byte value) =>
        _memory.Write(address, (byte)(value & ((address >> 8) + 1)));

    /// <summary>
    /// Wykonuje cykl dla NOP i KIL opcode'ów.
    /// </summary>
    internal bool ExecuteCycleNopKilOpcodes(ushort key)
    {
        byte opcode = (byte)(key >> 3);
        byte cycle = (byte)(key & 0x07);

        if (cycle > 0)
        {
            _sync = cycle >= _opcodeTable[opcode].BaseCycles - 1;
            return true;
        }

        if (opcode is 0x02 or 0x12 or 0x22 or 0x32 or 0x42 or 0x52 or
            0x62 or 0x72 or 0x92 or 0xB2 or 0xD2 or 0xF2)
        {
            _halted = true;
            _sync = true;
            return true;
        }

        switch (opcode)
        {
            case 0x04 or 0x44 or 0x64: _memory.Read(AddrZp()); break;
            case 0x14 or 0x34 or 0x54 or 0x74 or 0xD4 or 0xF4: _memory.Read(AddrZpX()); break;
            case 0x0C: _memory.Read(AddrAbs()); break;
            case 0x1C or 0x3C or 0x5C or 0x7C or 0xDC or 0xFC: _memory.Read(AddrAbsX(out _)); break;
            case 0x80 or 0x82 or 0x89 or 0xC2 or 0xE2: _memory.Read(_pc++); break;
            case 0x1A or 0x3A or 0x5A or 0x7A or 0xDA or 0xFA: break;
            default: return false;
        }

        return true;
    }

    /// <summary>
    /// Obsługuje IRQ po zakończeniu instrukcji, o ile dana instrukcja nie blokuje tej granicy.
    /// </summary>
    private void ServicePostInstructionIrqBoundary()
    {
        if (_interruptDelay)
        {
            return;
        }

        if (_suppressPostInstructionIrq)
        {
            _suppressPostInstructionIrq = false;
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            return;
        }

        if (_irqPending && !GetFlag(FlagI))
        {
            _irqPending = false;
            _irqReadyAtBoundary = false;
            InjectInterrupt(InterruptType.IRQ);
        }
    }





    #endregion
}

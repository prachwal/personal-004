using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Interrupts;
using Z80InterruptLines = PetEmulator.CpuZ80.Interrupts.IInterruptLines;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu : CpuProcessorBase<Z80State>
{
    private readonly IBus bus;
    private readonly IZ80CoreCapabilities capabilities;
    private readonly ILogger logger;
    private bool coreIrq;
    private bool coreNmi;

    /// <param name="logger">
    /// Optional per-instruction Trace source ("Core" category per the
    /// plan's logging doc). Omit it and no logging happens at all - the
    /// default <see cref="NullLogger"/> makes every call a no-op, so this
    /// costs nothing on the hot Step() path when nobody asked for it.
    /// </param>
    public Z80Cpu(IBus bus, Z80InterruptLines interruptLines, IBusCycleObserver? cycleObserver = null, ILogger<Z80Cpu>? logger = null, IClock? clock = null, IZ80CoreCapabilities? capabilities = null)
        : base(new Z80Registers(), new Z80MemoryBusAdapter(bus), clock, new Z80PortBusAdapter(bus))
    {
        this.bus = bus;
        this.capabilities = capabilities ?? new Z80CoreCapabilities(bus, interruptLines, cycleObserver);
        this.logger = logger ?? NullLogger<Z80Cpu>.Instance;
        InitializeOpcodes();
        Reset();
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "PC={PC:X4} opcode={Opcode:X2}")]
    private static partial void LogInstruction(ILogger logger, ushort pc, byte opcode);

    public Z80Registers Registers => (Z80Registers)State;

    /// <summary>
    /// Condition-checked diagnostic actions, run in order at the start of
    /// every <see cref="Step"/> - see <see cref="CpuHook"/>. Empty by
    /// default; add to this collection, don't replace it.
    /// </summary>
    public CpuHookCollection Hooks { get; } = new();

    public bool Iff1 { get => Registers.Iff1; private set => Registers.Iff1 = value; }
    public bool Iff2 { get => Registers.Iff2; private set => Registers.Iff2 = value; }
    public byte InterruptMode { get => Registers.InterruptMode; private set => Registers.InterruptMode = value; }

    private int traceMachineCycle;
    private int traceTStates;
    private int traceCycleLength;
    private int traceStandaloneRefreshLength;

    public override void Reset()
    {
        coreIrq = false;
        coreNmi = false;
        base.Reset();
    }

    public override void SetIRQ(bool active) => coreIrq = active;

    public override void SetNMI(bool active) => coreNmi = active;

    public new int Step()
    {
        var before = CycleCount;
        StepInstruction();
        return checked((int)(CycleCount - before));
    }

    protected override void BeforeStep()
    {
        traceMachineCycle = 0;
        traceTStates = 0;
        traceCycleLength = 0;
        traceStandaloneRefreshLength = 4;

        // HasAny is a plain bool field - skips even touching the list
        // (Count, indexer) when nothing is registered, the common case.
        if (Hooks.HasAny)
            for (var i = 0; i < Hooks.Count; i++)
                Hooks[i].RunIfMatched(this);

    }

    protected override bool TryHandleWaitBeforeInterrupt(out CpuStepResult result)
    {
        if (capabilities.WaitAsserted)
        {
            result = CpuStepResult.Idle(1, waiting: true); // frozen mid-bus-cycle, same as real hardware
            return true;
        }

        result = default;
        return false;
    }

    protected override bool TryServiceInterrupt(out CpuStepResult result)
    {
        var nmi = capabilities.NmiAsserted || coreNmi;
        var nmiEdge = nmi && !Registers.PreviousNmi;
        Registers.PreviousNmi = nmi;

        if (nmiEdge)
        {
            result = CpuStepResult.Interrupt((ulong)ServiceNmi());
            return true;
        }

        if ((capabilities.IntAsserted || coreIrq) && Iff1 && Registers.InterruptDelay == 0)
        {
            result = CpuStepResult.Interrupt((ulong)ServiceMaskableInterrupt());
            return true;
        }

        result = default;
        return false;
    }

    protected override bool TryHandleHalt(out CpuStepResult result)
    {
        if (!Halted)
        {
            result = default;
            return false;
        }

        IncrementRefresh();
        result = CpuStepResult.Idle(4);
        return true;
    }

    protected override OpcodeKey FetchOpcode()
    {
        var opcode = FetchByte(true);
        LogInstruction(logger, (ushort)(Registers.PC - 1), opcode);
        return OpcodeKey.Base(opcode);
    }

    protected override CpuStepResult ExecuteOpcode(OpcodeDefinition<Z80State> definition)
    {
        var result = definition.Execute(Registers, ExecutionContext);
        if (Registers.InterruptDelay > 0)
            Registers.InterruptDelay--;
        return result;
    }

    protected override OpcodeDefinition<Z80State> DecodeOpcode(OpcodeKey key)
        => Opcodes.TryGet(key, out var definition)
            ? definition!
            : throw new NotSupportedException($"Unsupported Z80 opcode {key.Page:X2}:{key.Opcode:X2}.");


    protected void RegisterOpcode(byte opcode, Func<int> execute)
        => RegisterOpcode(0, opcode, execute);

    protected void RegisterOpcode(byte page, byte opcode, Func<int> execute)
    {
        Opcodes.Set(CreateDefinition(page, opcode, execute));
    }

    private OpcodeDefinition<Z80State> CreateDefinition(byte page, byte opcode, Func<int> execute)
    {
        var metadata = Z80OpcodeMetadata.For(page, opcode);
        return new(
            new OpcodeKey(page, opcode),
            metadata.Mnemonic,
            metadata.Length,
            metadata.BaseCycles,
            metadata.AddressingMode,
            (_, _) => CpuStepResult.Completed((ulong)execute()));
    }

    private int ExecuteRegistered(OpcodeKey key, int fallbackCycles = 8)
    {
        if (!Opcodes.TryGet(key, out var definition))
            return fallbackCycles;

        return ExecuteDefinition(definition!);
    }

    private int ExecuteDefinition(OpcodeDefinition<Z80State> definition)
    {
        SetCurrentOpcode(definition.Key, definition.Mnemonic);
        return checked((int)definition.Execute(Registers, ExecutionContext).Cycles);
    }

    private int Halt()
    {
        Registers.Halted = true;
        return 4;
    }

    private int ExchangeAf()
    {
        (Registers.AF, Registers.AlternateAF) = (Registers.AlternateAF, Registers.AF);
        return 4;
    }

    private int ExchangeAlternate()
    {
        (Registers.BC, Registers.AlternateBC) = (Registers.AlternateBC, Registers.BC);
        (Registers.DE, Registers.AlternateDE) = (Registers.AlternateDE, Registers.DE);
        (Registers.HL, Registers.AlternateHL) = (Registers.AlternateHL, Registers.HL);
        return 4;
    }

    private int ExchangeDeHl()
    {
        (Registers.DE, Registers.HL) = (Registers.HL, Registers.DE);
        return 4;
    }

    private int DecrementBAndJump()
    {
        Registers.B--;
        var offset = (sbyte)FetchByte();
        if (Registers.B != 0)
            Registers.PC = (ushort)(Registers.PC + offset);
        return Registers.B != 0 ? 13 : 8;
    }

    private int IncrementPair(byte opcode)
    {
        SetPair((opcode >> 4) & 3, (ushort)(GetPairValue((opcode >> 4) & 3) + 1));
        return 6;
    }

    private int DecrementPair(byte opcode)
    {
        SetPair((opcode >> 4) & 3, (ushort)(GetPairValue((opcode >> 4) & 3) - 1));
        return 6;
    }

    private int AddPair(byte opcode)
    {
        var left = Registers.HL;
        var right = GetPairValue((opcode >> 4) & 3);
        var result = left + right;
        var flags = (byte)(Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow));
        flags |= (byte)((result >> 8) & (Z80Flags.X | Z80Flags.Y));
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        if (result > 0xFFFF) flags |= Z80Flags.Carry;
        Registers.HL = (ushort)result;
        Registers.F = flags;
        return 11;
    }

    private ushort GetPairValue(int pair)
        => CpuOperandHelpers.ReadPair(pair, Registers.BC, Registers.DE, Registers.HL, Registers.SP);

    private void SetPair(int pair, ushort value)
        => CpuOperandHelpers.WritePair(pair, value,
            value => Registers.BC = value, value => Registers.DE = value,
            value => Registers.HL = value, value => Registers.SP = value);

    private int RotateAccumulator(int operation)
    {
        var value = Registers.A;
        var carry = operation switch
        {
            0 => value >> 7,
            1 => value & 1,
            2 => value >> 7,
            _ => value & 1
        };
        Registers.A = operation switch
        {
            0 => (byte)((value << 1) | carry),
            1 => (byte)((value >> 1) | (carry << 7)),
            2 => (byte)((value << 1) | (IsCarry ? 1 : 0)),
            _ => (byte)((value >> 1) | (IsCarry ? 0x80 : 0))
        };
        Registers.F = (byte)((Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow)) |
            (Registers.A & (Z80Flags.X | Z80Flags.Y)) | (carry != 0 ? Z80Flags.Carry : 0));
        return 4;
    }

    private int DecimalAdjustAccumulator()
    {
        var old = Registers.A;
        var correction = 0;
        var carry = IsCarry;
        if (!IsSubtract)
        {
            if ((old & 0x0F) > 9 || IsHalfCarry) correction |= 0x06;
            if (old > 0x99 || carry)
            {
                correction |= 0x60;
                carry = true;
            }
        }
        else
        {
            if (IsHalfCarry) correction |= 0x06;
            if (carry) correction |= 0x60;
        }

        Registers.A = IsSubtract ? (byte)(old - correction) : (byte)(old + correction);
        var flags = (byte)((Registers.F & Z80Flags.AddSubtract) | Z80Flags.SignZero(Registers.A) |
            Z80Flags.Parity(Registers.A) | (Registers.A & (Z80Flags.X | Z80Flags.Y)) |
            (carry ? Z80Flags.Carry : 0));
        if (((old ^ Registers.A) & 0x10) != 0) flags |= Z80Flags.HalfCarry;
        Registers.F = flags;
        return 4;
    }

    private int ComplementAccumulator()
    {
        Registers.A = (byte)~Registers.A;
        Registers.F = (byte)((Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow | Z80Flags.Carry)) |
            Z80Flags.AddSubtract | Z80Flags.HalfCarry | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        return 4;
    }

    private int SetCarry()
    {
        Registers.F = (byte)((Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow)) |
            Z80Flags.Carry | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        return 4;
    }

    private int ComplementCarry()
    {
        var carry = IsCarry;
        Registers.F = (byte)((Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow)) |
            (carry ? Z80Flags.HalfCarry : Z80Flags.Carry) | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        return 4;
    }

    private int ExchangeStackAndHl()
    {
        var value = ReadWord(Registers.SP);
        WriteWord(Registers.SP, Registers.HL);
        Registers.HL = value;
        return 19;
    }

    private int LoadStackFromHl()
    {
        Registers.SP = Registers.HL;
        return 6;
    }

    private int LoadImmediate(byte opcode)
    {
        SetRegister((opcode >> 3) & 7, FetchByte());
        return (opcode & 0x38) == 0x30 ? 10 : 7;
    }

    private int Transfer(byte opcode)
    {
        var destination = (opcode >> 3) & 7;
        var source = opcode & 7;
        SetRegister(destination, GetRegister(source));
        return destination == 6 || source == 6 ? 7 : 4;
    }

    private int IncrementRegister(byte opcode)
    {
        var register = (opcode >> 3) & 7;
        SetRegister(register, Increment(GetRegister(register)));
        return register == 6 ? 11 : 4;
    }

    private int DecrementRegister(byte opcode)
    {
        var register = (opcode >> 3) & 7;
        SetRegister(register, Decrement(GetRegister(register)));
        return register == 6 ? 11 : 4;
    }

    private int RegisterAlu(byte opcode)
    {
        ApplyAlu((opcode >> 3) & 7, GetRegister(opcode & 7));
        return (opcode & 7) == 6 ? 7 : 4;
    }

    private int ExecuteCb()
    {
        var opcode = FetchByte(true);
        return ExecuteRegistered(new OpcodeKey(0xCB, opcode));
    }

    private int ExecuteCbOpcode(byte opcode)
    {
        var register = opcode & 7;
        var value = GetRegister(register);
        var bit = (opcode >> 3) & 7;
        var group = opcode >> 6;

        if (group == 0)
        {
            var operation = (opcode >> 3) & 7;
            var carry = operation switch
            {
                0 => value >> 7,
                1 => value & 1,
                2 => value >> 7,
                3 => value & 1,
                4 => value >> 7,
                5 => value & 1,
                6 => value >> 7,
                _ => value & 1
            };
            var rotated = operation switch
            {
                0 => (byte)((value << 1) | (value >> 7)),
                1 => (byte)((value >> 1) | (value << 7)),
                2 => (byte)((value << 1) | (IsCarry ? 1 : 0)),
                3 => (byte)((value >> 1) | (IsCarry ? 0x80 : 0)),
                4 => (byte)(value << 1),
                5 => (byte)((value >> 1) | (value & 0x80)),
                6 => (byte)((value << 1) | 1),
                _ => (byte)(value >> 1)
            };
            SetRegister(register, rotated);
            Registers.F = (byte)(Z80Flags.SignZero(rotated) | Z80Flags.Parity(rotated) |
                (rotated & (Z80Flags.X | Z80Flags.Y)) | (carry != 0 ? Z80Flags.Carry : 0));
            return register == 6 ? 15 : 8;
        }

        if (group == 1)
        {
            var carry = (value & (1 << bit)) != 0;
            var sign = bit == 7 && carry ? Z80Flags.Sign : (byte)0;
            var undocumented = register == 6
                ? (byte)((Registers.HL >> 8) & (Z80Flags.X | Z80Flags.Y))
                : (byte)(value & (Z80Flags.X | Z80Flags.Y));
            Registers.F = (byte)((Registers.F & Z80Flags.Carry) | sign |
                (carry ? (byte)0 : Z80Flags.Zero) | (carry ? (byte)0 : Z80Flags.ParityOverflow) |
                undocumented | Z80Flags.HalfCarry);
            return register == 6 ? 12 : 8;
        }

        var result = group == 2 ? (byte)(value & ~(1 << bit)) : (byte)(value | (1 << bit));
        SetRegister(register, result);
        return register == 6 ? 15 : 8;
    }

    private int ExecuteEd()
    {
        var opcode = FetchByte(true);
        return Opcodes.TryGet(new OpcodeKey(0xED, opcode), out var definition)
            ? ExecuteDefinition(definition!)
            : 8;
    }

    private int ExecuteDd() => ExecuteIndexedPrefix(false);

    private int ExecuteFd() => ExecuteIndexedPrefix(true);

    private int ExecuteIndexedPrefix(bool iy)
    {
        var opcode = FetchByte(true);
        return ExecuteRegistered(new OpcodeKey(iy ? (byte)0xFD : (byte)0xDD, opcode));
    }

    private int ExecuteIndexedOpcode(bool iy, byte opcode)
    {
        var index = iy ? Registers.IY : Registers.IX;

        if ((opcode & 0xC0) == 0x40 && opcode != 0x76)
        {
            var destination = (opcode >> 3) & 7;
            var source = opcode & 7;
            if (destination == 6 || source == 6)
            {
                // Real Z80 quirk: register codes 4/5 mean IXH/IXL only in a
                // pure register-to-register indexed op. The moment (IX+d)
                // is one of the operands, the other operand's 4/5 reverts
                // to plain H/L - GetIndexedRegister/SetIndexedRegister
                // would wrongly read/write IX's own bytes here instead.
                var address = IndexedAddress(iy);
                if (destination == 6)
                    WriteMemory(address, GetRegister(source));
                else
                    SetRegister(destination, ReadMemory(address));
                return 19;
            }

            SetIndexedRegister(iy, destination, GetIndexedRegister(iy, source));
            return 8;
        }

        if ((opcode & 0xC7) == 0x06 && ((opcode >> 3) & 7) is 4 or 5)
        {
            SetIndexedRegister(iy, (opcode >> 3) & 7, FetchByte());
            return 11;
        }

        if (((opcode & 0xC7) is 0x04 or 0x05) && ((opcode >> 3) & 7) is 4 or 5)
        {
            var register = (opcode >> 3) & 7;
            var value = GetIndexedRegister(iy, register);
            SetIndexedRegister(iy, register, (opcode & 1) == 0 ? Increment(value) : Decrement(value));
            return 8;
        }

        if ((opcode & 0xC0) == 0x80)
        {
            var source = opcode & 7;
            if (source == 6)
            {
                ApplyAlu((opcode >> 3) & 7, ReadMemory(IndexedAddress(iy)));
                return 19;
            }

            if (source is 4 or 5)
            {
                ApplyAlu((opcode >> 3) & 7, GetIndexedRegister(iy, source));
                return 8;
            }
        }

        switch (opcode)
        {
            case 0x21:
                SetIndex(iy, FetchWord());
                return 14;
            case 0x22:
                StoreIndexAbsolute(iy);
                return 20;
            case 0x2A:
                SetIndex(iy, ReadWord(FetchWord()));
                return 20;
            case 0x23:
                SetIndex(iy, (ushort)(index + 1));
                return 10;
            case 0x2B:
                SetIndex(iy, (ushort)(index - 1));
                return 10;
            case 0x09:
            case 0x19:
            case 0x29:
            case 0x39:
                var pair = (opcode >> 4) & 3;
                SetIndex(iy, AddIndex(index, pair == 2 ? GetIndex(iy) : GetPairValue(pair)));
                return 15;
            case 0xE9:
                Registers.PC = index;
                return 8;
            case 0xE5:
                return Push(index) + 4;
            case 0xE1:
                SetIndex(iy, PopWord());
                return 14;
            case 0xE3:
                var stackValue = ReadWord(Registers.SP);
                WriteWord(Registers.SP, index);
                SetIndex(iy, stackValue);
                return 23;
            case 0xF9:
                Registers.SP = index;
                return 10;
            case 0x36:
                WriteMemory(IndexedAddress(iy), FetchByte());
                return 19;
            case 0x34:
                var incrementAddress = IndexedAddress(iy);
                WriteMemory(incrementAddress, Increment(ReadMemory(incrementAddress)));
                return 23;
            case 0x35:
                var decrementAddress = IndexedAddress(iy);
                WriteMemory(decrementAddress, Decrement(ReadMemory(decrementAddress)));
                return 23;
            case 0xCB:
                return ExecuteIndexedCb(iy);
            default:
                if (opcode == 0xDD)
                    return ExecuteIndexedPrefix(false) + 4;
                if (opcode == 0xFD)
                    return ExecuteIndexedPrefix(true) + 4;
                return ExecuteRegistered(OpcodeKey.Base(opcode)) + 4;
        }
    }

    private int ExecuteIndexedCb(bool iy)
    {
        var address = IndexedAddress(iy);
        var opcode = FetchByte();
        var value = ReadMemory(address);
        var group = opcode >> 6;
        var bit = (opcode >> 3) & 7;
        if (group == 0)
        {
            var operation = (opcode >> 3) & 7;
            var carry = operation switch
            {
                0 or 2 or 4 or 6 => value >> 7,
                _ => value & 1
            };
            var result = operation switch
            {
                0 => (byte)((value << 1) | (value >> 7)),
                1 => (byte)((value >> 1) | (value << 7)),
                2 => (byte)((value << 1) | (IsCarry ? 1 : 0)),
                3 => (byte)((value >> 1) | (IsCarry ? 0x80 : 0)),
                4 => (byte)(value << 1),
                5 => (byte)((value >> 1) | (value & 0x80)),
                6 => (byte)((value << 1) | 1),
                _ => (byte)(value >> 1)
            };
            WriteMemory(address, result);
            if ((opcode & 7) != 6)
                SetRegister(opcode & 7, result);
            Registers.F = (byte)(Z80Flags.SignZero(result) | Z80Flags.Parity(result) |
                (result & (Z80Flags.X | Z80Flags.Y)) | (carry != 0 ? Z80Flags.Carry : 0));
            return 23;
        }

        if (group == 1)
        {
            var set = (value & (1 << bit)) != 0;
            var undocumented = (byte)((address >> 8) & (Z80Flags.X | Z80Flags.Y));
            Registers.F = (byte)((Registers.F & Z80Flags.Carry) | (set ? (byte)0 : Z80Flags.Zero) |
                (set ? (byte)0 : Z80Flags.ParityOverflow) | (bit == 7 && set ? Z80Flags.Sign : (byte)0) |
                Z80Flags.HalfCarry | undocumented);
            return 20;
        }

        var resultValue = group == 2 ? (byte)(value & ~(1 << bit)) : (byte)(value | (1 << bit));
        WriteMemory(address, resultValue);
        if ((opcode & 7) != 6)
            SetRegister(opcode & 7, resultValue);
        return 23;
    }

    private ushort IndexedAddress(bool iy) => (ushort)(GetIndex(iy) + (sbyte)FetchByte());

    private ushort GetIndex(bool iy) => iy ? Registers.IY : Registers.IX;

    private void SetIndex(bool iy, ushort value)
    {
        if (iy) Registers.IY = value;
        else Registers.IX = value;
    }

    private byte GetIndexedRegister(bool iy, int register) => register switch
    {
        4 => (byte)(GetIndex(iy) >> 8),
        5 => (byte)GetIndex(iy),
        _ => GetRegister(register)
    };

    private void SetIndexedRegister(bool iy, int register, byte value)
    {
        switch (register)
        {
            case 4:
                SetIndex(iy, (ushort)((GetIndex(iy) & 0x00FF) | (value << 8)));
                break;
            case 5:
                SetIndex(iy, (ushort)((GetIndex(iy) & 0xFF00) | value));
                break;
            default:
                SetRegister(register, value);
                break;
        }
    }

    private void StoreIndexAbsolute(bool iy)
    {
        var address = FetchWord();
        WriteWord(address, GetIndex(iy));
    }

    private ushort AddIndex(ushort left, ushort right)
    {
        var result = left + right;
        var flags = (byte)(Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.ParityOverflow));
        flags |= (byte)((result >> 8) & (Z80Flags.X | Z80Flags.Y));
        if (((left ^ right ^ result) & 0x1000) != 0) flags |= Z80Flags.HalfCarry;
        if (result > 0xFFFF) flags |= Z80Flags.Carry;
        Registers.F = flags;
        return (ushort)result;
    }

    private int BlockCopy()
    {
        return BlockTransfer(true, true);
    }

    private int BlockTransfer(bool increment, bool repeat)
    {
        var value = ReadMemory(Registers.HL);
        WriteMemory(Registers.DE, value);
        Registers.HL = (ushort)(Registers.HL + (increment ? 1 : -1));
        Registers.DE = (ushort)(Registers.DE + (increment ? 1 : -1));
        Registers.BC--;
        var flags = (byte)((Registers.F & (Z80Flags.Sign | Z80Flags.Zero | Z80Flags.Carry)) |
            (Registers.BC != 0 ? Z80Flags.ParityOverflow : 0));
        flags |= (byte)((Registers.A + value) & (Z80Flags.X | Z80Flags.Y));
        Registers.F = flags;
        if (repeat && Registers.BC != 0)
        {
            Registers.PC -= 2;
            return 21;
        }

        return 16;
    }

    private int BlockCompare(bool increment, bool repeat)
    {
        var value = ReadMemory(Registers.HL);
        var result = Registers.A - value;
        Registers.HL = (ushort)(Registers.HL + (increment ? 1 : -1));
        Registers.BC--;
        var flags = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.AddSubtract | (result & Z80Flags.Sign) |
            (result == 0 ? Z80Flags.Zero : 0) | (Registers.BC != 0 ? Z80Flags.ParityOverflow : 0));
        var halfCarry = ((Registers.A ^ value ^ result) & 0x10) != 0;
        if (halfCarry) flags |= Z80Flags.HalfCarry;
        flags |= (byte)((result - (halfCarry ? 1 : 0)) & (Z80Flags.X | Z80Flags.Y));
        Registers.F = flags;
        if (repeat && Registers.BC != 0 && result != 0)
        {
            Registers.PC -= 2;
            return 21;
        }

        return 16;
    }

    private int BlockInput(bool increment, bool repeat)
    {
        var value = ReadPort(Registers.BC);
        WriteMemory(Registers.HL, value);
        Registers.B--;
        Registers.HL = (ushort)(Registers.HL + (increment ? 1 : -1));
        SetBlockIoFlags(value, Registers.C + (increment ? 1 : -1));
        if (repeat && Registers.B != 0)
        {
            Registers.PC -= 2;
            return 21;
        }

        return 16;
    }

    private int BlockOutput(bool increment, bool repeat)
    {
        var value = ReadMemory(Registers.HL);
        WritePort(Registers.BC, value);
        Registers.B--;
        Registers.HL = (ushort)(Registers.HL + (increment ? 1 : -1));
        SetBlockIoFlags(value, Registers.L);
        if (repeat && Registers.B != 0)
        {
            Registers.PC -= 2;
            return 21;
        }

        return 16;
    }

    private void SetBlockIoFlags(byte value, int addend)
    {
        // addend is C+1/C-1 (BlockInput) or L (BlockOutput); C+/-1 must wrap
        // mod 256 before adding to value, or H/C come out wrong whenever C
        // wraps (0xFF->0 on increment, 0x00->0xFF on decrement).
        var sum = value + (addend & 0xFF);
        var flags = (byte)(Z80Flags.SignZero(Registers.B) | (Registers.B & (Z80Flags.X | Z80Flags.Y)) |
            ((value & Z80Flags.Sign) != 0 ? Z80Flags.AddSubtract : 0));
        if (sum > 0xFF) flags |= Z80Flags.HalfCarry | Z80Flags.Carry;
        if (Z80Flags.Parity((byte)((sum & 7) ^ Registers.B)) != 0) flags |= Z80Flags.ParityOverflow;
        Registers.F = flags;
    }

    private int ServiceNmi()
    {
        traceStandaloneRefreshLength = 5;
        Registers.Halted = false;
        IncrementRefresh();
        Push(Registers.PC);
        Iff2 = Iff1;
        Iff1 = false;
        Registers.PC = 0x0066;
        return 11;
    }

    private int ServiceMaskableInterrupt()
    {
        Registers.Halted = false;
        Iff1 = false;
        Iff2 = false;
        var acknowledgedOpcode = AcknowledgeInterrupt();
        IncrementRefresh();
        if (InterruptMode == 2)
        {
            Push(Registers.PC);
            var vector = (ushort)((Registers.I << 8) | acknowledgedOpcode);
            Registers.PC = ReadWord(vector);
            return 19;
        }

        if (InterruptMode == 0)
        {
            if (!Opcodes.TryGet(OpcodeKey.Base(acknowledgedOpcode), out var definition))
                throw new NotSupportedException($"Unsupported IM 0 interrupt opcode 0x{acknowledgedOpcode:X2}.");
            return ExecuteDefinition(definition!) + 2;
        }

        Push(Registers.PC);
        Registers.PC = 0x0038;
        return 13;
    }

    private static int LoadPair(Action action)
    {
        action();
        return 10;
    }

    private int MemoryWrite(ushort address, byte value, int cycles)
    {
        WriteMemory(address, value);
        return cycles;
    }

    private int MemoryRead(ushort address, Action<byte> assign, int cycles)
    {
        assign(ReadMemory(address));
        return cycles;
    }

    private int StoreAbsolute(byte value)
    {
        var address = FetchWord();
        WriteMemory(address, value);
        return 13;
    }

    private int LoadAbsolute(Action<byte> assign)
    {
        var address = FetchWord();
        assign(ReadMemory(address));
        return 13;
    }

    private int StoreAbsoluteWord(ushort value)
    {
        var address = FetchWord();
        WriteWord(address, value);
        return 16;
    }

    private int LoadAbsoluteWord(Action<ushort> assign)
    {
        var address = FetchWord();
        assign(ReadWord(address));
        return 16;
    }

    private int Jump(ushort address)
    {
        Registers.PC = address;
        return 10;
    }

    private int JumpToHl()
    {
        Registers.PC = Registers.HL;
        return 4;
    }

    private int ConditionalJump(bool condition)
    {
        var address = FetchWord();
        return condition ? Jump(address) : 10;
    }

    private int RelativeJump(bool condition)
    {
        var offset = (sbyte)FetchByte();
        if (condition)
            Registers.PC = (ushort)(Registers.PC + offset);
        return condition ? 12 : 7;
    }

    private int Call(bool condition)
    {
        var address = FetchWord();
        if (condition)
        {
            Push(Registers.PC);
            Registers.PC = address;
            return 17;
        }

        return 10;
    }

    private int ConditionalCall(bool condition) => Call(condition);

    private int Return(bool condition)
    {
        if (!condition)
            return 5;

        Registers.PC = PopWord();
        return 10;
    }

    private int ConditionalReturn(bool condition)
    {
        if (!condition)
            return 5;

        Registers.PC = PopWord();
        return 11;
    }

    private int Push(ushort value)
    {
        Registers.SP -= 2;
        WriteWord(Registers.SP, value);
        return 11;
    }

    private int Pop(Action<ushort> assign)
    {
        assign(PopWord());
        return 10;
    }

    private ushort PopWord()
    {
        var value = ReadWord(Registers.SP);
        Registers.SP += 2;
        return value;
    }

    private int ImmediateAlu(int operation)
    {
        ApplyAlu(operation, FetchByte());
        return 7;
    }

    private int InputImmediate()
    {
        var port = (ushort)((Registers.A << 8) | FetchByte());
        Registers.A = ReadPort(port);
        return 11;
    }

    private int OutputImmediate()
    {
        var port = (ushort)((Registers.A << 8) | FetchByte());
        WritePort(port, Registers.A);
        return 11;
    }

    private int InputRegister(int register)
    {
        var value = ReadPort(Registers.BC);
        if (register != 6)
            SetRegister(register, value);
        Registers.F = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.SignZero(value) |
            Z80Flags.Parity(value) | (value & (Z80Flags.X | Z80Flags.Y)));
        return 12;
    }

    private int OutputRegister(int register)
    {
        WritePort(Registers.BC, register == 6 ? (byte)0 : GetRegister(register));
        return 12;
    }

    private int DisableInterrupts()
    {
        Iff1 = false;
        Iff2 = false;
        return 4;
    }

    private int EnableInterrupts()
    {
        Iff1 = true;
        Iff2 = true;
        Registers.InterruptDelay = 2;
        return 4;
    }

    private int ReturnFromInterrupt(bool reti = false)
    {
        Registers.PC = PopWord();
        Iff1 = Iff2;
        if (reti)
            bus.NotifyInterruptReturn();
        return 14;
    }

    private int LoadInterruptRegister()
    {
        Registers.I = Registers.A;
        return 9;
    }

    private int LoadRefreshRegister()
    {
        Registers.R = Registers.A;
        return 9;
    }

    private int LoadAccumulatorFromInterrupt()
    {
        Registers.A = Registers.I;
        Registers.F = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.SignZero(Registers.A) |
            (Registers.A & (Z80Flags.X | Z80Flags.Y)) | (Iff2 ? Z80Flags.ParityOverflow : 0));
        return 9;
    }

    private int LoadAccumulatorFromRefresh()
    {
        Registers.A = Registers.R;
        Registers.F = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.SignZero(Registers.A) |
            (Registers.A & (Z80Flags.X | Z80Flags.Y)) | (Iff2 ? Z80Flags.ParityOverflow : 0));
        return 9;
    }

    private int NegateAccumulator()
    {
        var value = Registers.A;
        Registers.A = 0;
        Subtract(value, false);
        return 8;
    }

    private int AddPairWithCarry(byte opcode, bool subtract)
    {
        var left = Registers.HL;
        var right = GetPairValue((opcode >> 4) & 3);
        var carry = IsCarry ? 1 : 0;
        var result = subtract ? left - right - carry : left + right + carry;
        var value = (ushort)result;
        var flags = (byte)(subtract ? Z80Flags.AddSubtract : 0);
        var halfCarry = subtract
            ? ((left & 0x0FFF) - (right & 0x0FFF) - carry) < 0
            : ((left & 0x0FFF) + (right & 0x0FFF) + carry) > 0x0FFF;
        if (halfCarry) flags |= Z80Flags.HalfCarry;
        if (result < 0 || result > 0xFFFF) flags |= Z80Flags.Carry;
        if ((value & 0x8000) != 0) flags |= Z80Flags.Sign;
        if (value == 0) flags |= Z80Flags.Zero;
        var overflow = subtract
            ? (left ^ right) & (left ^ value)
            : ~(left ^ right) & (left ^ value);
        if ((overflow & 0x8000) != 0) flags |= Z80Flags.ParityOverflow;
        flags |= (byte)((value >> 8) & (Z80Flags.X | Z80Flags.Y));
        Registers.HL = value;
        Registers.F = flags;
        return 15;
    }

    private int StorePairAbsolute(byte opcode)
    {
        var address = FetchWord();
        WriteWord(address, GetPairValue((opcode >> 4) & 3));
        return 20;
    }

    private int LoadPairAbsolute(byte opcode)
    {
        var address = FetchWord();
        SetPair((opcode >> 4) & 3, ReadWord(address));
        return 20;
    }

    private int RotateRightDecimal()
    {
        var value = ReadMemory(Registers.HL);
        var result = (byte)((Registers.A << 4) | (value >> 4));
        Registers.A = (byte)((Registers.A & 0xF0) | (value & 0x0F));
        WriteMemory(Registers.HL, result);
        Registers.F = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.SignZero(Registers.A) |
            Z80Flags.Parity(Registers.A) | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        return 18;
    }

    private int RotateLeftDecimal()
    {
        var value = ReadMemory(Registers.HL);
        var result = (byte)((value << 4) | (Registers.A & 0x0F));
        Registers.A = (byte)((Registers.A & 0xF0) | (value >> 4));
        WriteMemory(Registers.HL, result);
        Registers.F = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.SignZero(Registers.A) |
            Z80Flags.Parity(Registers.A) | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        return 18;
    }

    private int SetInterruptMode(byte mode)
    {
        InterruptMode = mode;
        return 8;
    }

    private int Restart(ushort address)
    {
        Push(Registers.PC);
        Registers.PC = address;
        return 11;
    }

    private byte FetchByte(bool refresh = false)
    {
        var address = Registers.PC++;
        var value = refresh ? ReadOpcode(address) : ReadMemory(address);
        if (refresh)
            IncrementRefresh();
        return value;
    }

    private void IncrementRefresh()
    {
        Registers.R = (byte)((Registers.R & 0x80) | ((Registers.R + 1) & 0x7F));
        Trace(BusCycleKind.Refresh, (ushort)((Registers.I << 8) | Registers.R), Registers.R);
    }

    private ushort FetchWord() => (ushort)(FetchByte() | (FetchByte() << 8));

    private ushort ReadWord(ushort address) => (ushort)(ReadMemory(address) | (ReadMemory((ushort)(address + 1)) << 8));

    private void WriteWord(ushort address, ushort value)
    {
        WriteMemory(address, (byte)value);
        WriteMemory((ushort)(address + 1), (byte)(value >> 8));
    }

    private byte GetRegister(int register)
        => CpuOperandHelpers.ReadRegister(register, Registers.A, Registers.B, Registers.C, Registers.D,
            Registers.E, Registers.H, Registers.L, Registers.HL, ReadMemory);

    private void SetRegister(int register, byte value)
        => CpuOperandHelpers.WriteRegister(register, value,
            value => Registers.A = value, value => Registers.B = value, value => Registers.C = value,
            value => Registers.D = value, value => Registers.E = value, value => Registers.H = value,
            value => Registers.L = value, Registers.HL, WriteMemory);

    private byte ReadMemory(ushort address)
    {
        var value = Memory.Read(address);
        Trace(BusCycleKind.MemoryRead, address, value);
        return value;
    }

    private void WriteMemory(ushort address, byte value)
    {
        Memory.Write(address, value);
        Trace(BusCycleKind.MemoryWrite, address, value);
    }

    private byte ReadOpcode(ushort address)
    {
        var value = bus.ReadOpcode(address);
        Trace(BusCycleKind.OpcodeFetch, address, value);
        return value;
    }

    private byte ReadPort(byte port)
    {
        var value = bus.ReadPort(port);
        Trace(BusCycleKind.IoRead, port, value);
        return value;
    }

    private byte ReadPort(ushort port)
    {
        var value = bus.ReadPort(port);
        Trace(BusCycleKind.IoRead, port, value);
        return value;
    }

    private void WritePort(byte port, byte value)
    {
        bus.WritePort(port, value);
        Trace(BusCycleKind.IoWrite, port, value);
    }

    private void WritePort(ushort port, byte value)
    {
        bus.WritePort(port, value);
        Trace(BusCycleKind.IoWrite, port, value);
    }

    private byte AcknowledgeInterrupt()
    {
        var value = capabilities.AcknowledgeInterrupt();
        Trace(BusCycleKind.InterruptAcknowledge, 0, value);
        return value;
    }

    private void Trace(BusCycleKind kind, ushort address, byte value)
    {
        var isRefresh = kind == BusCycleKind.Refresh && traceMachineCycle > 0;
        if (!isRefresh)
        {
            traceMachineCycle++;
            traceTStates = 1;
            traceCycleLength = kind == BusCycleKind.Refresh ? traceStandaloneRefreshLength : kind == BusCycleKind.InterruptAcknowledge ? 6 : kind switch
            {
                BusCycleKind.OpcodeFetch => 4,
                BusCycleKind.IoRead or BusCycleKind.IoWrite => 4,
                _ => 3
            };
        }
        else
        {
            traceTStates = traceCycleLength;
        }

        capabilities.Observe(new BusCycle(kind, address, value,
            traceMachineCycle, traceTStates, traceCycleLength));
    }

    private byte Increment(byte value)
    {
        var arithmetic = CpuArithmetic.Increment8(value);
        var result = arithmetic.Result;
        var flags = (byte)(Registers.F & Z80Flags.Carry);
        flags |= Z80Flags.SignZero(result);
        flags |= (byte)(result & (Z80Flags.X | Z80Flags.Y));
        if (arithmetic.HalfCarry) flags |= Z80Flags.HalfCarry;
        if (arithmetic.Overflow) flags |= Z80Flags.ParityOverflow;
        Registers.F = flags;
        return result;
    }

    private byte Decrement(byte value)
    {
        var arithmetic = CpuArithmetic.Decrement8(value);
        var result = arithmetic.Result;
        var flags = (byte)((Registers.F & Z80Flags.Carry) | Z80Flags.AddSubtract);
        flags |= Z80Flags.SignZero(result);
        flags |= (byte)(result & (Z80Flags.X | Z80Flags.Y));
        if (arithmetic.HalfCarry) flags |= Z80Flags.HalfCarry;
        if (arithmetic.Overflow) flags |= Z80Flags.ParityOverflow;
        Registers.F = flags;
        return result;
    }

    private void ApplyAlu(int operation, byte value)
    {
        switch (operation)
        {
            case 0: Add(value, false); break;
            case 1: Add(value, true); break;
            case 2: Subtract(value, false); break;
            case 3: Subtract(value, true); break;
            case 4: Registers.A &= value; Registers.F = (byte)(Z80Flags.SignZero(Registers.A) | Z80Flags.Parity(Registers.A) | Z80Flags.HalfCarry | (Registers.A & (Z80Flags.X | Z80Flags.Y))); break;
            case 5: Registers.A ^= value; Registers.F = (byte)(Z80Flags.SignZero(Registers.A) | Z80Flags.Parity(Registers.A) | (Registers.A & (Z80Flags.X | Z80Flags.Y))); break;
            case 6: Registers.A |= value; Registers.F = (byte)(Z80Flags.SignZero(Registers.A) | Z80Flags.Parity(Registers.A) | (Registers.A & (Z80Flags.X | Z80Flags.Y))); break;
            default: Compare(value); break;
        }
    }

    private void Add(byte value, bool withCarry)
    {
        var left = Registers.A;
        var arithmetic = CpuArithmetic.Add8(left, value, withCarry && IsCarry);
        Registers.A = arithmetic.Result;
        var flags = (byte)(Z80Flags.SignZero(Registers.A) | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        if (arithmetic.HalfCarry) flags |= Z80Flags.HalfCarry;
        if (arithmetic.Overflow) flags |= Z80Flags.ParityOverflow;
        if (arithmetic.Carry) flags |= Z80Flags.Carry;
        Registers.F = flags;
    }

    private void Subtract(byte value, bool withCarry)
    {
        var left = Registers.A;
        var arithmetic = CpuArithmetic.Subtract8(left, value, withCarry && IsCarry);
        Registers.A = arithmetic.Result;
        var flags = (byte)(Z80Flags.SignZero(Registers.A) | Z80Flags.AddSubtract | (Registers.A & (Z80Flags.X | Z80Flags.Y)));
        if (arithmetic.HalfCarry) flags |= Z80Flags.HalfCarry;
        if (arithmetic.Overflow) flags |= Z80Flags.ParityOverflow;
        if (arithmetic.Carry) flags |= Z80Flags.Carry;
        Registers.F = flags;
    }

    private void Compare(byte value)
    {
        var oldA = Registers.A;
        Subtract(value, false);
        Registers.A = oldA;
    }

    private bool IsCarry => Z80Flags.IsSet(Registers.F, Z80Flags.Carry);
    private bool IsHalfCarry => Z80Flags.IsSet(Registers.F, Z80Flags.HalfCarry);
    private bool IsSubtract => Z80Flags.IsSet(Registers.F, Z80Flags.AddSubtract);
    private bool IsZero => Z80Flags.IsSet(Registers.F, Z80Flags.Zero);
    private bool IsParity => Z80Flags.IsSet(Registers.F, Z80Flags.ParityOverflow);
    private bool IsSign => Z80Flags.IsSet(Registers.F, Z80Flags.Sign);
}

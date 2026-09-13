using System.Numerics;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

public partial class Cpu8080
{
    private CpuStepResult ExecuteInstruction(byte opcode, CpuExecutionContext context)
    {
        // Cpu8080State exposes only the five architectural flags. Keep bits
        // that belong to the 8080 status byte representation out of the
        // execution state; PUSH/POP PSW adds/removes bit 1 at the boundary.
        State.Flags &= 0xD5;
        var extraCycles = 0;

        switch (opcode)
        {
            case 0x00:
                break;

            case 0x01: State.BC = Fetch16(); break;
            case 0x11: State.DE = Fetch16(); break;
            case 0x21: State.HL = Fetch16(); break;
            case 0x31: State.SP = Fetch16(); break;
            case 0x02: Memory.Write(State.BC, State.A); break;
            case 0x12: Memory.Write(State.DE, State.A); break;
            case 0x0A: State.A = Memory.Read(State.BC); break;
            case 0x1A: State.A = Memory.Read(State.DE); break;
            case 0x22:
            {
                var address = Fetch16();
                Memory.Write(address, State.L);
                Memory.Write((ushort)(address + 1), State.H);
                break;
            }
            case 0x2A:
            {
                var address = Fetch16();
                State.L = Memory.Read(address);
                State.H = Memory.Read((ushort)(address + 1));
                break;
            }
            case 0x32: Memory.Write(Fetch16(), State.A); break;
            case 0x3A: State.A = Memory.Read(Fetch16()); break;
            case 0xEB: (State.H, State.L, State.D, State.E) = (State.D, State.E, State.H, State.L); break;

            case >= 0x40 and <= 0x7F:
                if (opcode == 0x76)
                {
                    State.Halted = true;
                    break;
                }

                WriteRegister((opcode >> 3) & 7, ReadRegister(opcode & 7));
                break;

            case 0x06: WriteRegister(0, Fetch8()); break;
            case 0x0E: WriteRegister(1, Fetch8()); break;
            case 0x16: WriteRegister(2, Fetch8()); break;
            case 0x1E: WriteRegister(3, Fetch8()); break;
            case 0x26: WriteRegister(4, Fetch8()); break;
            case 0x2E: WriteRegister(5, Fetch8()); break;
            case 0x36: WriteRegister(6, Fetch8()); break;
            case 0x3E: WriteRegister(7, Fetch8()); break;

            case 0x03: State.BC++; break;
            case 0x13: State.DE++; break;
            case 0x23: State.HL++; break;
            case 0x33: State.SP++; break;
            case 0x0B: State.BC--; break;
            case 0x1B: State.DE--; break;
            case 0x2B: State.HL--; break;
            case 0x3B: State.SP--; break;

            case 0x09: AddPair(State.BC); break;
            case 0x19: AddPair(State.DE); break;
            case 0x29: AddPair(State.HL); break;
            case 0x39: AddPair(State.SP); break;

            case >= 0x80 and <= 0x87: Add(ReadRegister(opcode & 7), false); break;
            case >= 0x88 and <= 0x8F: Add(ReadRegister(opcode & 7), true); break;
            case >= 0x90 and <= 0x97: Sub(ReadRegister(opcode & 7), false, true); break;
            case >= 0x98 and <= 0x9F: Sub(ReadRegister(opcode & 7), true, true); break;
            case >= 0xA0 and <= 0xA7: And(ReadRegister(opcode & 7)); break;
            case >= 0xA8 and <= 0xAF: Xor(ReadRegister(opcode & 7)); break;
            case >= 0xB0 and <= 0xB7: Or(ReadRegister(opcode & 7)); break;
            case >= 0xB8 and <= 0xBF: Sub(ReadRegister(opcode & 7), false, false); break;

            case 0xC6: Add(Fetch8(), false); break;
            case 0xCE: Add(Fetch8(), true); break;
            case 0xD6: Sub(Fetch8(), false, true); break;
            case 0xDE: Sub(Fetch8(), true, true); break;
            case 0xE6: And(Fetch8()); break;
            case 0xEE: Xor(Fetch8()); break;
            case 0xF6: Or(Fetch8()); break;
            case 0xFE: Sub(Fetch8(), false, false); break;

            case 0x04: Increment(0); break;
            case 0x0C: Increment(1); break;
            case 0x14: Increment(2); break;
            case 0x1C: Increment(3); break;
            case 0x24: Increment(4); break;
            case 0x2C: Increment(5); break;
            case 0x34: Increment(6); break;
            case 0x3C: Increment(7); break;
            case 0x05: Decrement(0); break;
            case 0x0D: Decrement(1); break;
            case 0x15: Decrement(2); break;
            case 0x1D: Decrement(3); break;
            case 0x25: Decrement(4); break;
            case 0x2D: Decrement(5); break;
            case 0x35: Decrement(6); break;
            case 0x3D: Decrement(7); break;

            case 0x07: RotateLeft(); break;
            case 0x0F: RotateRight(); break;
            case 0x17: RotateLeftThroughCarry(); break;
            case 0x1F: RotateRightThroughCarry(); break;
            case 0x27: DecimalAdjust(); break;
            case 0x2F: State.A = (byte)~State.A; break;
            case 0x37: FlagCarry = true; break;
            case 0x3F: FlagCarry = !FlagCarry; break;

            case 0xC3: State.PC = Fetch16(); break;
            case 0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA:
            {
                var target = Fetch16();
                if (Condition((opcode >> 3) & 7)) State.PC = target;
                break;
            }
            case 0xCD:
                Call(Fetch16());
                break;
            case 0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC:
            {
                var target = Fetch16();
                if (Condition((opcode >> 3) & 7))
                {
                    Call(target);
                    extraCycles = 6;
                }
                break;
            }
            case 0xC9: State.PC = Pop16(); break;
            case 0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 or 0xF0 or 0xF8:
                if (Condition((opcode >> 3) & 7))
                {
                    State.PC = Pop16();
                    extraCycles = 6;
                }
                break;
            case 0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF:
                Call((ushort)(((opcode >> 3) & 7) * 8));
                break;
            case 0xE9: State.PC = State.HL; break;

            case 0xC5: Push16(State.BC); break;
            case 0xD5: Push16(State.DE); break;
            case 0xE5: Push16(State.HL); break;
            case 0xF5: Push16((ushort)((State.A << 8) | PackFlags())); break;
            case 0xC1: State.BC = Pop16(); break;
            case 0xD1: State.DE = Pop16(); break;
            case 0xE1: State.HL = Pop16(); break;
            case 0xF1:
            {
                var value = Pop16();
                State.A = (byte)(value >> 8);
                UnpackFlags((byte)value);
                break;
            }
            case 0xE3:
            {
                var low = Memory.Read(State.SP);
                var high = Memory.Read((ushort)(State.SP + 1));
                Memory.Write(State.SP, State.L);
                Memory.Write((ushort)(State.SP + 1), State.H);
                State.L = low;
                State.H = high;
                break;
            }
            case 0xF9: State.SP = State.HL; break;

            case 0xD3:
            {
                var port = Fetch8();
                context.Ports?.Write(port, State.A);
                break;
            }
            case 0xDB:
            {
                var port = Fetch8();
                State.A = context.Ports?.Read(port) ?? 0xFF;
                break;
            }
            case 0xF3:
                State.InterruptsEnabled = false;
                State.EiPending = false;
                break;
            case 0xFB:
                State.EiPending = true;
                break;
            // 8080-compatible aliases present in the imported implementation.
            case 0xCB: State.PC = Fetch16(); break;
            case 0xD9: State.PC = Pop16(); break;
            case 0xDD or 0xED or 0xFD: Call(Fetch16()); break;
            case 0x08 or 0x10 or 0x18 or 0x20 or 0x28 or 0x30 or 0x38:
                break;
            default:
                throw new NotSupportedException($"8080 opcode 0x{opcode:X2} is not implemented.");
        }

        return CpuStepResult.Completed((ulong)(Cycles[opcode] + extraCycles));
    }

    private byte Fetch8()
    {
        var value = Memory.Read(State.PC);
        State.PC++;
        return value;
    }

    private ushort Fetch16()
    {
        var low = Fetch8();
        var high = Fetch8();
        return (ushort)((high << 8) | low);
    }

    private byte ReadRegister(int index)
        => CpuOperandHelpers.ReadRegister(index, State.A, State.B, State.C, State.D, State.E,
            State.H, State.L, State.HL, Memory.Read);

    private void WriteRegister(int index, byte value)
        => CpuOperandHelpers.WriteRegister(index, value,
            value => State.A = value, value => State.B = value, value => State.C = value,
            value => State.D = value, value => State.E = value, value => State.H = value,
            value => State.L = value, State.HL, Memory.Write);

    private void Add(byte value, bool withCarry)
    {
        var result = CpuArithmetic.Add8(State.A, value, withCarry && FlagCarry);
        FlagAuxiliaryCarry = result.HalfCarry;
        FlagCarry = result.Carry;
        State.A = result.Result;
        SetSignZeroParity(State.A);
    }

    private void Sub(byte value, bool withBorrow, bool writeAccumulator)
    {
        var result = CpuArithmetic.Subtract8(State.A, value, withBorrow && FlagCarry);
        FlagAuxiliaryCarry = result.HalfCarry;
        FlagCarry = result.Carry;
        if (writeAccumulator) State.A = result.Result;
        SetSignZeroParity(result.Result);
    }

    private void And(byte value)
    {
        FlagAuxiliaryCarry = ((State.A | value) & 0x08) != 0;
        State.A &= value;
        FlagCarry = false;
        SetSignZeroParity(State.A);
    }

    private void Xor(byte value)
    {
        State.A ^= value;
        FlagAuxiliaryCarry = false;
        FlagCarry = false;
        SetSignZeroParity(State.A);
    }

    private void Or(byte value)
    {
        State.A |= value;
        FlagAuxiliaryCarry = false;
        FlagCarry = false;
        SetSignZeroParity(State.A);
    }

    private void Increment(int index)
    {
        var value = ReadRegister(index);
        var result = CpuArithmetic.Increment8(value);
        FlagAuxiliaryCarry = result.HalfCarry;
        WriteRegister(index, result.Result);
        SetSignZeroParity(result.Result);
    }

    private void Decrement(int index)
    {
        var value = ReadRegister(index);
        var result = CpuArithmetic.Decrement8(value);
        FlagAuxiliaryCarry = result.HalfCarry;
        WriteRegister(index, result.Result);
        SetSignZeroParity(result.Result);
    }

    private void AddPair(ushort value)
    {
        var sum = (uint)State.HL + value;
        FlagCarry = sum > ushort.MaxValue;
        State.HL = (ushort)sum;
    }

    private void RotateLeft()
    {
        FlagCarry = (State.A & 0x80) != 0;
        State.A = (byte)((State.A << 1) | (State.A >> 7));
    }

    private void RotateRight()
    {
        FlagCarry = (State.A & 1) != 0;
        State.A = (byte)((State.A >> 1) | (State.A << 7));
    }

    private void RotateLeftThroughCarry()
    {
        var carry = FlagCarry ? 1 : 0;
        FlagCarry = (State.A & 0x80) != 0;
        State.A = (byte)((State.A << 1) | carry);
    }

    private void RotateRightThroughCarry()
    {
        var carry = FlagCarry ? 0x80 : 0;
        FlagCarry = (State.A & 1) != 0;
        State.A = (byte)((State.A >> 1) | carry);
    }

    private void DecimalAdjust()
    {
        var correction = 0;
        var carry = FlagCarry;
        if ((State.A & 0x0F) > 9 || FlagAuxiliaryCarry) correction |= 0x06;
        if ((State.A >> 4) > 9 || FlagCarry || ((State.A >> 4) >= 9 && (State.A & 0x0F) > 9))
        {
            correction |= 0x60;
            carry = true;
        }

        var result = State.A + correction;
        FlagAuxiliaryCarry = ((State.A & 0x0F) + (correction & 0x0F)) > 0x0F;
        State.A = (byte)result;
        FlagCarry = carry;
        SetSignZeroParity(State.A);
    }

    private bool Condition(int condition)
        => CpuOperandHelpers.EvaluateCondition(condition, FlagZero, FlagCarry, FlagParity, FlagSign);

    private void Call(ushort target)
    {
        Push16(State.PC);
        State.PC = target;
    }

    private void SetSignZeroParity(byte value)
    {
        FlagSign = (value & 0x80) != 0;
        FlagZero = value == 0;
        FlagParity = (BitOperations.PopCount(value) & 1) == 0;
    }

    private byte PackFlags()
        => (byte)(State.Flags | 0x02);

    private void UnpackFlags(byte value)
        => State.Flags = (byte)(value & 0xD5);

    private bool FlagSign
    {
        get => (State.Flags & 0x80) != 0;
        set => State.Flags = value ? (byte)(State.Flags | 0x80) : (byte)(State.Flags & ~0x80);
    }

    private bool FlagZero
    {
        get => (State.Flags & 0x40) != 0;
        set => State.Flags = value ? (byte)(State.Flags | 0x40) : (byte)(State.Flags & ~0x40);
    }

    private bool FlagAuxiliaryCarry
    {
        get => (State.Flags & 0x10) != 0;
        set => State.Flags = value ? (byte)(State.Flags | 0x10) : (byte)(State.Flags & ~0x10);
    }

    private bool FlagParity
    {
        get => (State.Flags & 0x04) != 0;
        set => State.Flags = value ? (byte)(State.Flags | 0x04) : (byte)(State.Flags & ~0x04);
    }

    private bool FlagCarry
    {
        get => (State.Flags & 0x01) != 0;
        set => State.Flags = value ? (byte)(State.Flags | 0x01) : (byte)(State.Flags & ~0x01);
    }
}

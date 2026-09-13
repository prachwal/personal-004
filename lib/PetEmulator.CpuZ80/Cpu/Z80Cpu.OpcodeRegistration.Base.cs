using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    private void RegisterBaseOpcodes()
    {
        RegisterOpcode(0x00, () => 4);
        RegisterOpcode(0x08, ExchangeAf);
        RegisterOpcode(0x07, () => RotateAccumulator(0));
        RegisterOpcode(0x0F, () => RotateAccumulator(1));
        RegisterOpcode(0x17, () => RotateAccumulator(2));
        RegisterOpcode(0x1F, () => RotateAccumulator(3));
        RegisterOpcode(0x10, DecrementBAndJump);
        RegisterOpcode(0x27, DecimalAdjustAccumulator);
        RegisterOpcode(0x2F, ComplementAccumulator);
        RegisterOpcode(0x37, SetCarry);
        RegisterOpcode(0x3F, ComplementCarry);
        RegisterOpcode(0x76, Halt);
        RegisterOpcode(0x01, () => LoadPair(() => Registers.BC = FetchWord()));
        RegisterOpcode(0x11, () => LoadPair(() => Registers.DE = FetchWord()));
        RegisterOpcode(0x21, () => LoadPair(() => Registers.HL = FetchWord()));
        RegisterOpcode(0x31, () => LoadPair(() => Registers.SP = FetchWord()));
        RegisterOpcode(0x02, () => MemoryWrite(Registers.BC, Registers.A, 7));
        RegisterOpcode(0x12, () => MemoryWrite(Registers.DE, Registers.A, 7));
        RegisterOpcode(0x0A, () => MemoryRead(Registers.BC, value => Registers.A = value, 7));
        RegisterOpcode(0x1A, () => MemoryRead(Registers.DE, value => Registers.A = value, 7));
        RegisterOpcode(0x22, () => StoreAbsoluteWord(Registers.HL));
        RegisterOpcode(0x2A, () => LoadAbsoluteWord(value => Registers.HL = value));
        RegisterOpcode(0x32, () => StoreAbsolute(Registers.A));
        RegisterOpcode(0x3A, () => LoadAbsolute(value => Registers.A = value));
        RegisterOpcode(0xC3, () => Jump(FetchWord()));
        RegisterOpcode(0x18, () => RelativeJump(true));
        RegisterOpcode(0x20, () => RelativeJump(!IsZero));
        RegisterOpcode(0x28, () => RelativeJump(IsZero));
        RegisterOpcode(0x30, () => RelativeJump(!IsCarry));
        RegisterOpcode(0x38, () => RelativeJump(IsCarry));
        RegisterOpcode(0xCD, () => Call(true));
        RegisterOpcode(0xC9, () => Return(true));
        RegisterOpcode(0xC2, () => ConditionalJump(!IsZero));
        RegisterOpcode(0xCA, () => ConditionalJump(IsZero));
        RegisterOpcode(0xD2, () => ConditionalJump(!IsCarry));
        RegisterOpcode(0xDA, () => ConditionalJump(IsCarry));
        RegisterOpcode(0xE2, () => ConditionalJump(!IsParity));
        RegisterOpcode(0xEA, () => ConditionalJump(IsParity));
        RegisterOpcode(0xF2, () => ConditionalJump(!IsSign));
        RegisterOpcode(0xFA, () => ConditionalJump(IsSign));
        RegisterOpcode(0xC4, () => ConditionalCall(!IsZero));
        RegisterOpcode(0xCC, () => ConditionalCall(IsZero));
        RegisterOpcode(0xD4, () => ConditionalCall(!IsCarry));
        RegisterOpcode(0xDC, () => ConditionalCall(IsCarry));
        RegisterOpcode(0xE4, () => ConditionalCall(!IsParity));
        RegisterOpcode(0xEC, () => ConditionalCall(IsParity));
        RegisterOpcode(0xF4, () => ConditionalCall(!IsSign));
        RegisterOpcode(0xFC, () => ConditionalCall(IsSign));
        RegisterOpcode(0xC0, () => ConditionalReturn(!IsZero));
        RegisterOpcode(0xC8, () => ConditionalReturn(IsZero));
        RegisterOpcode(0xD0, () => ConditionalReturn(!IsCarry));
        RegisterOpcode(0xD8, () => ConditionalReturn(IsCarry));
        RegisterOpcode(0xE0, () => ConditionalReturn(!IsParity));
        RegisterOpcode(0xE8, () => ConditionalReturn(IsParity));
        RegisterOpcode(0xF0, () => ConditionalReturn(!IsSign));
        RegisterOpcode(0xF8, () => ConditionalReturn(IsSign));
        RegisterOpcode(0xC5, () => Push(Registers.BC));
        RegisterOpcode(0xD5, () => Push(Registers.DE));
        RegisterOpcode(0xE5, () => Push(Registers.HL));
        RegisterOpcode(0xF5, () => Push(Registers.AF));
        RegisterOpcode(0xC1, () => Pop(value => Registers.BC = value));
        RegisterOpcode(0xD1, () => Pop(value => Registers.DE = value));
        RegisterOpcode(0xE1, () => Pop(value => Registers.HL = value));
        RegisterOpcode(0xF1, () => Pop(value => Registers.AF = value));
        RegisterOpcode(0xC6, () => ImmediateAlu(0));
        RegisterOpcode(0xCE, () => ImmediateAlu(1));
        RegisterOpcode(0xD6, () => ImmediateAlu(2));
        RegisterOpcode(0xDE, () => ImmediateAlu(3));
        RegisterOpcode(0xE6, () => ImmediateAlu(4));
        RegisterOpcode(0xEE, () => ImmediateAlu(5));
        RegisterOpcode(0xF6, () => ImmediateAlu(6));
        RegisterOpcode(0xFE, () => ImmediateAlu(7));
        RegisterOpcode(0xF3, DisableInterrupts);
        RegisterOpcode(0xFB, EnableInterrupts);
        RegisterOpcode(0xE9, JumpToHl);
        RegisterOpcode(0xE3, ExchangeStackAndHl);
        RegisterOpcode(0xD9, ExchangeAlternate);
        RegisterOpcode(0xEB, ExchangeDeHl);
        RegisterOpcode(0xF9, LoadStackFromHl);
        RegisterOpcode(0xC7, () => Restart(0x00));
        RegisterOpcode(0xCF, () => Restart(0x08));
        RegisterOpcode(0xD7, () => Restart(0x10));
        RegisterOpcode(0xDF, () => Restart(0x18));
        RegisterOpcode(0xE7, () => Restart(0x20));
        RegisterOpcode(0xEF, () => Restart(0x28));
        RegisterOpcode(0xF7, () => Restart(0x30));
        RegisterOpcode(0xFF, () => Restart(0x38));
        RegisterOpcode(0xCB, ExecuteCb);
        RegisterOpcode(0xED, ExecuteEd);
        RegisterOpcode(0xDD, ExecuteDd);
        RegisterOpcode(0xFD, ExecuteFd);

        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            if ((opcode & 0xC7) == 0x06)
            {
                var loadOpcode = (byte)opcode;
                RegisterOpcode(loadOpcode, () => LoadImmediate(loadOpcode));
            }
            else if ((opcode & 0xC0) == 0x40 && opcode != 0x76)
            {
                var transferOpcode = (byte)opcode;
                RegisterOpcode(transferOpcode, () => Transfer(transferOpcode));
            }
            else if ((opcode & 0xC7) == 0x04)
            {
                var incrementOpcode = (byte)opcode;
                RegisterOpcode(incrementOpcode, () => IncrementRegister(incrementOpcode));
            }
            else if ((opcode & 0xC7) == 0x05)
            {
                var decrementOpcode = (byte)opcode;
                RegisterOpcode(decrementOpcode, () => DecrementRegister(decrementOpcode));
            }
            else if ((opcode & 0xC0) == 0x80)
            {
                var aluOpcode = (byte)opcode;
                RegisterOpcode(aluOpcode, () => RegisterAlu(aluOpcode));
            }
            else if ((opcode & 0xCF) == 0x03)
            {
                var incrementPairOpcode = (byte)opcode;
                RegisterOpcode(incrementPairOpcode, () => IncrementPair(incrementPairOpcode));
            }
            else if ((opcode & 0xCF) == 0x0B)
            {
                var decrementPairOpcode = (byte)opcode;
                RegisterOpcode(decrementPairOpcode, () => DecrementPair(decrementPairOpcode));
            }
            else if ((opcode & 0xCF) == 0x09)
            {
                var addPairOpcode = (byte)opcode;
                RegisterOpcode(addPairOpcode, () => AddPair(addPairOpcode));
            }
        }
    }
}

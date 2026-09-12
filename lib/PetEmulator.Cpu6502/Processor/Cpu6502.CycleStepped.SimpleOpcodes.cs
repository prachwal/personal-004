namespace PetEmulator.Cpu6502;

public partial class Cpu6502
{
    internal void ExecuteSimpleCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            switch (opcode)
            {
                case 0xEA: Nop(); break;
                case 0xAA: Tax(); break;
                case 0xA8: Tay(); break;
                case 0xBA: Tsx(); break;
                case 0x8A: Txa(); break;
                case 0x9A: Txs(); break;
                case 0x98: Tya(); break;
                case 0xE8: Inx(); break;
                case 0xC8: Iny(); break;
                case 0xCA: Dex(); break;
                case 0x88: Dey(); break;
                case 0x18: Clc(); break;
                case 0x38: Sec(); break;
                case 0xD8: Cld(); break;
                case 0xF8: Sed(); break;
                case 0x58: Cli(); break;
                case 0x78: Sei(); break;
                case 0xB8: Clv(); break;
                default: throw new InvalidOperationException($"Unsupported simple opcode 0x{opcode:X2}.");
            }
        }

        if (cycle == 1)
            _sync = true;
    }

    internal void ExecuteAccumulatorStackCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            switch (opcode)
            {
                case 0x0A: AslAcc(); break;
                case 0x4A: LsrAcc(); break;
                case 0x2A: RolAcc(); break;
                case 0x6A: RorAcc(); break;
                case 0x48: Pha(); break;
                case 0x08: Php(); break;
                case 0x68: Pla(); break;
                case 0x28: Plp(); break;
                case 0x1A: IncAcc(); break;
                case 0x3A: DecAcc(); break;
                case 0xDA: Phx(); break;
                case 0x5A: Phy(); break;
                case 0xFA: Plx(); break;
                case 0x7A: Ply(); break;
                default: throw new InvalidOperationException($"Unsupported accumulator/stack opcode 0x{opcode:X2}.");
            }
        }

        if (cycle == (opcode is 0x48 or 0x08 or 0xDA or 0x5A ? 2 : opcode is 0x68 or 0x28 or 0xFA or 0x7A ? 3 : 1))
            _sync = true;
    }

    /// <summary>
    /// Dispatch dla nowych, jednoinstrukcyjnych opcode'ów 65C02
    /// (STZ, TRB/TSB, BIT immediate/zp,X/abs,X, ALU przez (zp)).
    /// </summary>
    internal void Execute65C02Cycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            switch (opcode)
            {
                case 0x04: TsbZp(); break;
                case 0x0C: TsbAbs(); break;
                case 0x12: OraZpIndirect(); break;
                case 0x14: TrbZp(); break;
                case 0x1C: TrbAbs(); break;
                case 0x32: AndZpIndirect(); break;
                case 0x34: BitZpX(); break;
                case 0x3C: BitAbsX(); break;
                case 0x52: EorZpIndirect(); break;
                case 0x64: StzZp(); break;
                case 0x72: AdcZpIndirect(); break;
                case 0x74: StzZpX(); break;
                case 0x89: BitImm(); break;
                case 0x92: StaZpIndirect(); break;
                case 0x9C: StzAbs(); break;
                case 0x9E: StzAbsX(); break;
                case 0xB2: LdaZpIndirect(); break;
                case 0xD2: CmpZpIndirect(); break;
                case 0xF2: SbcZpIndirect(); break;
                default: throw new InvalidOperationException($"Unsupported 65C02 opcode 0x{opcode:X2}.");
            }
        }

        if (cycle > 0)
            _sync = cycle >= GetEffectiveInstructionCycles(opcode) - 1;
    }

    internal void ExecuteCmosNopCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            switch (_currentDefinition!.AddressingMode)
            {
                case nameof(AddressingMode.Implied):
                    break;
                case nameof(AddressingMode.Immediate):
                    _memory.Read(_pc++);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported CMOS NOP addressing mode for opcode 0x{opcode:X2}.");
            }
        }
        if (cycle >= GetEffectiveInstructionCycles(opcode) - 1)
            _sync = true;
    }
}

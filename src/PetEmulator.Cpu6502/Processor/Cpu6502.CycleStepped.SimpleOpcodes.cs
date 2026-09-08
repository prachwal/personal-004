namespace Cpu6502;

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
                default: throw new InvalidOperationException($"Unsupported accumulator/stack opcode 0x{opcode:X2}.");
            }
        }

        if (cycle == (opcode is 0x48 or 0x08 ? 2 : opcode is 0x68 or 0x28 ? 3 : 1))
            _sync = true;
    }
}

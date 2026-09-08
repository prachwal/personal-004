namespace Cpu6502;

public partial class Cpu6502
{
    internal void ExecuteLoadStoreCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            switch (opcode)
            {
                case 0xA9: LdaImm(); break;
                case 0xA5: LdaZp(); break;
                case 0xB5: LdaZpX(); break;
                case 0xAD: LdaAbs(); break;
                case 0xBD: LdaAbsX(); break;
                case 0xB9: LdaAbsY(); break;
                case 0xA1: LdaIndX(); break;
                case 0xB1: LdaIndY(); break;
                case 0x85: StaZp(); break;
                case 0x95: StaZpX(); break;
                case 0x8D: StaAbs(); break;
                case 0x9D: StaAbsX(); break;
                case 0x99: StaAbsY(); break;
                case 0x81: StaIndX(); break;
                case 0x91: StaIndY(); break;
                case 0xA2: LdxImm(); break;
                case 0xA6: LdxZp(); break;
                case 0xB6: LdxZpY(); break;
                case 0xAE: LdxAbs(); break;
                case 0xBE: LdxAbsY(); break;
                case 0x86: StxZp(); break;
                case 0x96: StxZpY(); break;
                case 0x8E: StxAbs(); break;
                case 0xA0: LdyImm(); break;
                case 0xA4: LdyZp(); break;
                case 0xB4: LdyZpX(); break;
                case 0xAC: LdyAbs(); break;
                case 0xBC: LdyAbsX(); break;
                case 0x84: StyZp(); break;
                case 0x94: StyZpX(); break;
                case 0x8C: StyAbs(); break;
                default: throw new InvalidOperationException($"Unsupported load/store opcode 0x{opcode:X2}.");
            }
        }

        if (cycle > 0)
            _sync = cycle >= GetEffectiveInstructionCycles(opcode) - 1;
    }
}

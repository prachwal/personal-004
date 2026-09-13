using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

public partial class Cpu8080
{
    private static readonly byte[] Lengths =
    [
        1,3,1,1,1,1,2,1, 1,1,1,1,1,1,2,1,
        1,3,1,1,1,1,2,1, 1,1,1,1,1,1,2,1,
        1,3,3,1,1,1,2,1, 1,1,3,1,1,1,2,1,
        1,3,3,1,1,1,2,1, 1,1,3,1,1,1,2,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,
        1,1,3,3,3,1,2,1, 1,1,3,3,3,3,2,1,
        1,1,3,2,3,1,2,1, 1,1,3,2,3,3,2,1,
        1,1,3,1,3,1,2,1, 1,1,3,1,3,3,2,1,
        1,1,3,1,3,1,2,1, 1,1,3,1,3,3,2,1,
    ];

    private static readonly byte[] Cycles =
    [
        4,10, 7, 5, 5, 5, 7, 4,  4,10, 7, 5, 5, 5, 7, 4,
        4,10, 7, 5, 5, 5, 7, 4,  4,10, 7, 5, 5, 5, 7, 4,
        4,10,16, 5, 5, 5, 7, 4,  4,10,16, 5, 5, 5, 7, 4,
        4,10,13, 5,10,10,10, 4,  4,10,13, 5, 5, 5, 7, 4,
        5, 5, 5, 5, 5, 5, 7, 5,  5, 5, 5, 5, 5, 5, 7, 5,
        5, 5, 5, 5, 5, 5, 7, 5,  5, 5, 5, 5, 5, 5, 7, 5,
        5, 5, 5, 5, 5, 5, 7, 5,  5, 5, 5, 5, 5, 5, 7, 5,
        7, 7, 7, 7, 7, 7, 7, 7,  5, 5, 5, 5, 5, 5, 7, 5,
        4, 4, 4, 4, 4, 4, 7, 4,  4, 4, 4, 4, 4, 4, 7, 4,
        4, 4, 4, 4, 4, 4, 7, 4,  4, 4, 4, 4, 4, 4, 7, 4,
        4, 4, 4, 4, 4, 4, 7, 4,  4, 4, 4, 4, 4, 4, 7, 4,
        4, 4, 4, 4, 4, 4, 7, 4,  4, 4, 4, 4, 4, 4, 7, 4,
        5,10,10,10,11,11, 7,11,  5,10,10,10,11,17, 7,11,
        5,10,10,10,11,11, 7,11,  5,10,10,10,11,17, 7,11,
        5,10,10,18,11,11, 7,11,  5, 5,10, 4,11,17, 7,11,
        5,10,10, 4,11,11, 7,11,  5, 5,10, 4,11,17, 7,11,
    ];

    protected override void ConfigureOpcodes(OpcodeTable<Cpu8080State> table)
    {
        for (var value = 0; value < 256; value++)
        {
            var opcode = (byte)value;
            table.Add(new OpcodeDefinition<Cpu8080State>(
                OpcodeKey.Base(opcode),
                Mnemonic(opcode),
                Lengths[opcode],
                Cycles[opcode],
                AddressingMode(opcode),
                (_, context) => ExecuteInstruction(opcode, context)));
        }
    }

    private static string Mnemonic(byte opcode)
        => opcode switch
        {
            0x00 => "NOP",
            0x76 => "HLT",
            0x01 or 0x11 or 0x21 or 0x31 => "LXI",
            0x02 or 0x12 => "STAX",
            0x0A or 0x1A => "LDAX",
            0x22 => "SHLD",
            0x2A => "LHLD",
            0x32 => "STA",
            0x3A => "LDA",
            >= 0x40 and <= 0x7F => "MOV",
            >= 0x80 and <= 0x87 => "ADD",
            >= 0x88 and <= 0x8F => "ADC",
            >= 0x90 and <= 0x97 => "SUB",
            >= 0x98 and <= 0x9F => "SBB",
            >= 0xA0 and <= 0xA7 => "ANA",
            >= 0xA8 and <= 0xAF => "XRA",
            >= 0xB0 and <= 0xB7 => "ORA",
            >= 0xB8 and <= 0xBF => "CMP",
            0xC3 or 0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA => "JMP",
            0xCD or 0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC => "CALL",
            0xC9 or 0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 or 0xF0 or 0xF8 => "RET",
            0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF => "RST",
            0xDB => "IN",
            0xD3 => "OUT",
            0xF3 => "DI",
            0xFB => "EI",
            _ => "8080",
        };

    private static string AddressingMode(byte opcode)
        => opcode switch
        {
            0x01 or 0x11 or 0x21 or 0x31 or 0x22 or 0x2A or 0x32 or 0x3A
                or 0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA
                or 0xC3 or 0xC4 or 0xCC or 0xCD or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC
                => Cpu8080AddressingMode.Direct.ToString(),
            0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E
                or 0xC6 or 0xCE or 0xD6 or 0xDE or 0xE6 or 0xEE or 0xF6 or 0xFE
                or 0xD3 or 0xDB => Cpu8080AddressingMode.Immediate.ToString(),
            0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF
                => Cpu8080AddressingMode.Restart.ToString(),
            _ => Cpu8080AddressingMode.Register.ToString(),
        };
}

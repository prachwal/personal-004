using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    private void RegisterIoOpcodes()
    {
        RegisterOpcode(0xDB, InputImmediate);
        RegisterOpcode(0xD3, OutputImmediate);

        for (var edOpcode = 0; edOpcode <= byte.MaxValue; edOpcode++)
        {
            if ((edOpcode & 0xC7) == 0x40)
            {
                var inputOpcode = (byte)edOpcode;
                RegisterEdOpcode(inputOpcode, () => InputRegister((inputOpcode >> 3) & 7));
            }
            else if ((edOpcode & 0xC7) == 0x41)
            {
                var outputOpcode = (byte)edOpcode;
                RegisterEdOpcode(outputOpcode, () => OutputRegister((outputOpcode >> 3) & 7));
            }
        }
    }
}

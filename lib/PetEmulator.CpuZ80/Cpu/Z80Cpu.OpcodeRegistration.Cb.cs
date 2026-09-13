using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    private void RegisterCbOpcodes()
    {
        for (var cbOpcode = 0; cbOpcode <= byte.MaxValue; cbOpcode++)
        {
            var subOpcode = (byte)cbOpcode;
            RegisterPageOpcode(0xCB, subOpcode, () => ExecuteCbOpcode(subOpcode));
        }
    }
}

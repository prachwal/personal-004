using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    private void RegisterIndexedOpcodes()
    {
        for (var indexedOpcode = 0; indexedOpcode <= byte.MaxValue; indexedOpcode++)
        {
            var subOpcode = (byte)indexedOpcode;
            RegisterOpcode(0xDD, subOpcode, () => ExecuteIndexedOpcode(false, subOpcode));
            RegisterOpcode(0xFD, subOpcode, () => ExecuteIndexedOpcode(true, subOpcode));
        }
    }
}

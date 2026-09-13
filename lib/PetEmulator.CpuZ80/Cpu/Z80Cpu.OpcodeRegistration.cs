using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    protected override void ConfigureOpcodes(OpcodeTable<Z80State> table)
    {
        RegisterBaseOpcodes();

        RegisterEdOpcodes();

        RegisterCbOpcodes();

        for (var indexedOpcode = 0; indexedOpcode <= byte.MaxValue; indexedOpcode++)
        {
            var subOpcode = (byte)indexedOpcode;
            RegisterPageOpcode(0xDD, subOpcode, () => ExecuteIndexedOpcode(false, subOpcode));
            RegisterPageOpcode(0xFD, subOpcode, () => ExecuteIndexedOpcode(true, subOpcode));
        }
    }
}

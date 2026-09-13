using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    protected override void ConfigureOpcodes(OpcodeTable<Z80State> table)
    {
        RegisterBaseOpcodes();

        RegisterEdOpcodes();

        RegisterCbOpcodes();

        RegisterIndexedOpcodes();
    }
}

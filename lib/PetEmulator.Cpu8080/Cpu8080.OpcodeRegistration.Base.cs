using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

public sealed partial class Cpu8080
{
    protected override void ConfigureOpcodes(OpcodeTable<Cpu8080State> table)
    {
        table.Add(new OpcodeDefinition<Cpu8080State>(
            OpcodeKey.Base(0x00),
            "NOP",
            Length: 1,
            BaseCycles: 4,
            AddressingMode: Cpu8080AddressingMode.Implied.ToString(),
            Execute: static (_, _) => CpuStepResult.Completed(4)));
    }
}

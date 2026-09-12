using System.Text;

namespace PetEmulator.Pet.Diagnostics;

public sealed record SuperPetInstructionDiagnostic(
    int Sequence,
    ushort ProgramCounter,
    byte Opcode,
    ushort NextProgramCounter,
    int Cycles,
    ulong TotalInstructions,
    bool Halted,
    byte A,
    byte B,
    ushort X,
    ushort Y,
    ushort U,
    ushort S,
    byte DirectPage,
    byte Flags);

public sealed record SuperPetBusAccessDiagnostic(
    ushort ProgramCounter,
    bool IsWrite,
    ushort Address,
    byte Value);

public sealed record SuperPetStartupDiagnostics(
    string ProfileId,
    SuperPetProcessor SelectedProcessor,
    IReadOnlyList<string> FirmwareRanges,
    ushort ResetVector,
    ushort InitialProgramCounter,
    ushort FinalProgramCounter,
    ulong TotalCycles,
    ulong TotalInstructions,
    bool Halted,
    IReadOnlyList<SuperPetInstructionDiagnostic> Instructions,
    IReadOnlyList<SuperPetBusAccessDiagnostic> DeviceAccesses)
{
    public string Render()
    {
        var output = new StringBuilder()
            .AppendLine($"profile={ProfileId}")
            .AppendLine($"processor={SelectedProcessor}")
            .AppendLine($"firmware={string.Join(", ", FirmwareRanges)}")
            .AppendLine($"reset-vector=${ResetVector:X4}")
            .AppendLine($"pc-initial=${InitialProgramCounter:X4} pc-final=${FinalProgramCounter:X4}")
            .AppendLine($"cycles={TotalCycles} instructions={TotalInstructions} halted={Halted}")
            .AppendLine("trace:");

        foreach (var instruction in Instructions)
        {
            output.AppendLine(
                $"  [{instruction.Sequence}] PC=${instruction.ProgramCounter:X4} " +
                $"OP=${instruction.Opcode:X2} next=${instruction.NextProgramCounter:X4} " +
                $"cycles={instruction.Cycles} total={instruction.TotalInstructions} halted={instruction.Halted} " +
                $"A=${instruction.A:X2} B=${instruction.B:X2} X=${instruction.X:X4} Y=${instruction.Y:X4} " +
                $"U=${instruction.U:X4} S=${instruction.S:X4} DP=${instruction.DirectPage:X2} P=${instruction.Flags:X2}");
        }

        output.AppendLine("device-accesses:");
        foreach (var access in DeviceAccesses)
            output.AppendLine($"  PC=${access.ProgramCounter:X4} {(access.IsWrite ? "W" : "R")} ${access.Address:X4}=${access.Value:X2}");

        return output.ToString();
    }
}

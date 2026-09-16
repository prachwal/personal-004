namespace PetEmulator.Core;

/// <summary>Versioned state captured from one concrete machine implementation.</summary>
public interface IMachineSnapshot
{
    int Version { get; set; }
}

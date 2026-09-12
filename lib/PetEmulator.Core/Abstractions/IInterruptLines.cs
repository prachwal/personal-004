namespace PetEmulator.Core;

/// <summary>Optional maskable and non-maskable interrupt line state.</summary>
public interface IInterruptLines
{
    bool IntAsserted { get; }

    bool NmiAsserted { get; }
}

namespace PetEmulator.Desktop.ViewModels;

/// <summary>One named live value in the side panel's status list - one <see cref="StatusField"/>
/// per row (PC, A, Cycles, ...), instead of one preformatted string per machine that every view
/// had to word-wrap blindly.</summary>
public sealed record StatusField(string Name, string Value);

public interface IShellModule : IDisposable
{
    string WindowTitle { get; }

    /// <summary>Live status rows for the side panel. Machines expose one field per value
    /// (registers, cycles); tools expose a single message row.</summary>
    IReadOnlyList<StatusField> StatusFields { get; }
}

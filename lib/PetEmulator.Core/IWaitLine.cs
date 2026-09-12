namespace PetEmulator.Core;

/// <summary>Optional WAIT capability for processors that can pause a bus cycle.</summary>
public interface IWaitLine
{
    bool WaitAsserted { get; }
}

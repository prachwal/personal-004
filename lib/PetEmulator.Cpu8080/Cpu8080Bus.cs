using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

/// <summary>Optional 8080-specific interrupt acknowledge source.</summary>
public interface ICpu8080InterruptBus
{
    byte AcknowledgeInterrupt();
}

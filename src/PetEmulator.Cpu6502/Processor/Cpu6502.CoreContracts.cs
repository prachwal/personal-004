using PetEmulator.Core;

namespace Cpu6502;

public partial class Cpu6502 : IProcessor
{
    void IProcessor.Reset() => Reset();
}

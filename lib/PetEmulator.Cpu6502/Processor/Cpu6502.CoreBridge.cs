using PetEmulator.Core;

namespace PetEmulator.Cpu6502;

/// <summary>
/// Adapter between the shared processor lifecycle and the cycle-stepped
/// 6502 engine. Opcode lookup and execution use the shared Core registry.
/// </summary>
public partial class Cpu6502
{
    private void SyncSharedState()
    {
        State.A = _a;
        State.X = _x;
        State.Y = _y;
        State.PC = _pc;
        State.SP = _sp;
        State.P = _p;
        State.Cycle = _clock.CycleCount;
        State.IR = _ir;
        State.Sync = _sync;
        State.Halted = _halted;
        base.InstructionCount = _instructionCount;
    }

    protected override OpcodeKey FetchOpcode()
        => OpcodeKey.Base(_memory.Read(_pc));

    protected override PetEmulator.Core.OpcodeDefinition<CpuState> DecodeOpcode(OpcodeKey key)
    {
        return Opcodes.Get(key);
    }

    private PetEmulator.Core.OpcodeDefinition<CpuState> GetSharedOpcode(byte opcode)
        => Opcodes.Get(OpcodeKey.Base(opcode));

    protected override CpuStepResult ExecuteOpcode(
        PetEmulator.Core.OpcodeDefinition<CpuState> definition)
        => definition.Execute(State, ExecutionContext);
}

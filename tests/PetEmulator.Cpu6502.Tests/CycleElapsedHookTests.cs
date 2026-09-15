using System.Linq;
using PetEmulator.Cpu6502;
using PetEmulator.Cpu6502.Variants;
using NUnit.Framework;

namespace PetEmulator.Cpu6502.Tests;

/// <summary>
/// Proves <see cref="Cpu6502.CycleElapsed"/> is a pure per-cycle observation point:
/// it fires exactly once per elapsed CPU cycle, and subscribing to it changes nothing
/// about the resulting CPU/memory state.
/// </summary>
[TestFixture]
public class CycleElapsedHookTests
{
    [TestCase((byte)0xEA, 2)] // NOP (implied), 2 cycles
    [TestCase((byte)0xAD, 4)] // LDA absolute, 4 cycles
    [TestCase((byte)0x0E, 6)] // ASL absolute (RMW), 6 cycles
    public void CycleElapsed_fires_exactly_once_per_elapsed_cycle(byte opcode, int expectedCycles)
    {
        var memory = new FlatMemory();
        var cpu = new Cpu6502Classic(memory);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
        memory.Write(0x0000, opcode);
        memory.Write(0x0001, 0x10); // operand low byte (harmless target address)
        memory.Write(0x0002, 0x00); // operand high byte
        cpu.PC = 0x0000;

        int hookInvocations = 0;
        cpu.CycleElapsed += () => hookInvocations++;

        ulong before = cpu.CycleCount;
        cpu.StepInstruction();
        ulong observedDelta = cpu.CycleCount - before;

        Assert.AreEqual((ulong)expectedCycles, observedDelta, "sanity check on the known opcode's cycle count");
        Assert.AreEqual((int)observedDelta, hookInvocations, "hook must fire exactly once per elapsed cycle");
    }

    [Test]
    public void Subscribing_to_CycleElapsed_does_not_change_resulting_CPU_or_memory_state()
    {
        (Cpu6502Registers registers, byte[] memorySnapshot) RunInstruction(bool attachSubscriber)
        {
            var memory = new FlatMemory();
            var cpu = new Cpu6502Classic(memory);
            memory.Write(0xFFFC, 0x00);
            memory.Write(0xFFFD, 0x00);
            cpu.Reset();

            // ADC #$05 after loading A=$01 and setting carry via SEC, then a RMW (INC $10)
            // to exercise a read-modify-write memory side effect too.
            memory.Write(0x0000, 0xA9); // LDA #$01
            memory.Write(0x0001, 0x01);
            memory.Write(0x0002, 0x38); // SEC
            memory.Write(0x0003, 0x69); // ADC #$05
            memory.Write(0x0004, 0x05);
            memory.Write(0x0005, 0xE6); // INC $10
            memory.Write(0x0006, 0x10);
            cpu.PC = 0x0000;

            if (attachSubscriber)
            {
                cpu.CycleElapsed += () => { };
            }

            for (int i = 0; i < 4; i++)
            {
                cpu.StepInstruction();
            }

            var snapshot = new byte[32];
            for (ushort a = 0; a < snapshot.Length; a++)
            {
                snapshot[a] = memory.Read(a);
            }

            return (cpu.Registers, snapshot);
        }

        var without = RunInstruction(attachSubscriber: false);
        var with = RunInstruction(attachSubscriber: true);

        Assert.AreEqual(without.registers.A, with.registers.A, nameof(Cpu6502Registers.A));
        Assert.AreEqual(without.registers.X, with.registers.X, nameof(Cpu6502Registers.X));
        Assert.AreEqual(without.registers.Y, with.registers.Y, nameof(Cpu6502Registers.Y));
        Assert.AreEqual(without.registers.SP, with.registers.SP, nameof(Cpu6502Registers.SP));
        Assert.AreEqual(without.registers.P, with.registers.P, nameof(Cpu6502Registers.P));
        Assert.AreEqual(without.registers.PC, with.registers.PC, nameof(Cpu6502Registers.PC));
        Assert.IsTrue(without.memorySnapshot.SequenceEqual(with.memorySnapshot), "memory side effects must be identical");
    }
}

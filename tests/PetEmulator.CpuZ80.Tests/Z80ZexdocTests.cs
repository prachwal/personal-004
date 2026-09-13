using System.Diagnostics;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80ZexdocTests
{
    [Fact]
    public async Task ZexdocComCompletesWhenEnabled()
    {
        var bus = new CpmBus();
        var program = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "zexdoc.com"));
        Array.Copy(program, 0, bus.Memory, 0x0100, program.Length);
        bus.Memory[0x0005] = 0xC9;
        bus.Memory[0x0006] = 0x00;
        bus.Memory[0x0007] = 0xF0;

        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.PC = 0x0100;
        cpu.Registers.SP = 0xFF00;

        await Task.Run(() =>
        {
            const ulong progressInterval = 100_000_000;
            const ulong checkpointInterval = 1_000_000_000;
            var nextProgress = progressInterval;
            var nextCheckpoint = checkpointInterval;
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (cpu.Registers.PC == 0x0005)
                {
                    bus.HandleBdos(cpu.Registers);
                    if (cpu.Registers.C == 0)
                        break;
                }

                cpu.Step();

                if (cpu.InstructionCount >= nextProgress)
                {
                    var checkpoint = cpu.InstructionCount >= nextCheckpoint;
                    Console.Error.WriteLine(
                        $"ZEXDOC {(checkpoint ? "checkpoint" : "progress")}: instructions={cpu.InstructionCount:N0}, cycles={cpu.CycleCount:N0}, " +
                        $"elapsed={stopwatch.Elapsed}, PC=0x{cpu.Registers.PC:X4}, SP=0x{cpu.Registers.SP:X4}, " +
                        $"AF=0x{cpu.Registers.AF:X4}, BC=0x{cpu.Registers.BC:X4}, DE=0x{cpu.Registers.DE:X4}, " +
                        $"HL=0x{cpu.Registers.HL:X4}, outputLength={bus.Output.Length}");
                    nextProgress += progressInterval;
                    if (checkpoint)
                        nextCheckpoint += checkpointInterval;
                }
            }
        });

        Assert.DoesNotContain("ERROR", bus.Output, StringComparison.Ordinal);
        Assert.Contains("Tests complete", bus.Output, StringComparison.Ordinal);
    }

    private sealed class CpmBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public string Output { get; private set; } = string.Empty;

        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }

        public void HandleBdos(Z80Registers registers)
        {
            switch (registers.C)
            {
                case 2:
                    Output += (char)registers.E;
                    break;
                case 9:
                    for (var address = registers.DE; Memory[address] != (byte)'$'; address++)
                        Output += (char)Memory[address];
                    break;
            }
        }
    }
}

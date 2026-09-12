using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80ZexdocTests
{
    [Fact]
    public void ZexdocComCompletesWhenEnabled()
    {
        var bus = new CpmBus();
        var program = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "zexdoc.com"));
        Array.Copy(program, 0, bus.Memory, 0x0100, program.Length);
        bus.Memory[0x0005] = 0xC9;

        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.PC = 0x0100;
        cpu.Registers.SP = 0xFF00;

        while (true)
        {
            if (cpu.Registers.PC == 0x0005)
            {
                bus.HandleBdos(cpu.Registers);
                if (cpu.Registers.C == 0)
                    break;
            }

            cpu.Step();
        }

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

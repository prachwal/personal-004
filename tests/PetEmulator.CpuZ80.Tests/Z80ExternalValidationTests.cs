using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80ExternalValidationTests
{
    [Fact]
    public void RaxoftsDocumentedInstructionSuiteReturnsSuccessfully()
    {
        var tap = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "z80doc.tap"));
        var program = ExtractLargestCodeBlock(tap);
        var bus = new TestBus();
        Array.Copy(program, 0, bus.Memory, 0x8000, program.Length);
        bus.Memory[0x0010] = 0xC9; // Spectrum RST 10h output hook.
        bus.Memory[0x1601] = 0xC9; // Spectrum CHAN-OPEN used by printinit.
        bus.Memory[0xF000] = 0x76;
        bus.Memory[0xFFFD] = 0x00;
        bus.Memory[0xFFFE] = 0xF0;
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.SP = 0xFFFD;
        cpu.Registers.PC = 0x8000;

        while (!cpu.Halted)
            cpu.Step();

        Assert.True(cpu.Halted, $"z80doc did not return at PC=0x{cpu.Registers.PC:X4}.");
    }

    private static byte[] ExtractLargestCodeBlock(byte[] tap)
    {
        byte[]? largest = null;
        for (var offset = 0; offset + 2 <= tap.Length;)
        {
            var length = tap[offset] | tap[offset + 1] << 8;
            Assert.InRange(length, 2, tap.Length - offset - 2);
            if (tap[offset + 2] == 0xFF && (largest is null || length > largest.Length + 2))
                largest = tap[(offset + 3)..(offset + 2 + length - 1)];
            offset += 2 + length;
        }

        return Assert.IsType<byte[]>(largest);
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => port == 0xFE ? (byte)0xBF : (byte)0xFF;
        public void WritePort(byte port, byte value) { }
    }
}

using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80CompatibilityTests
{
    [Fact]
    public void EmbeddedRomLikeVectorsProduceExpectedMemoryAndTiming()
    {
        foreach (var vector in Vectors)
        {
            var bus = new TestBus();
            Array.Copy(vector.Program, bus.Memory, vector.Program.Length);
            var cpu = new Z80Cpu(bus, new InterruptLines());
            vector.Setup(cpu, bus);
            var cycles = 0;

            for (var step = 0; step < vector.Steps; step++)
                cycles += cpu.Step();

            Assert.Equal(vector.ExpectedCycles, cycles);
            Assert.Equal(vector.ExpectedValue, bus.Memory[vector.ResultAddress]);
            Assert.True(cpu.Halted);
        }
    }

    [Fact]
    public void LongRomLikeProgramProducesSignatureAndStableTiming()
    {
        var program = new byte[]
        {
            0x21, 0x00, 0x40,
            0x11, 0x00, 0x41,
            0x01, 0x00, 0x01,
            0x7E, 0xC6, 0x01, 0x12, 0x23, 0x13, 0x0B, 0x78, 0xB1,
            0xC2, 0x09, 0x00,
            0x3E, 0x5A, 0x32, 0x00, 0x42, 0x76
        };
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        bus.Memory[0x4000] = 0x00;
        var cpu = new Z80Cpu(bus, new InterruptLines());
        var totalTStates = 0;

        while (!cpu.Halted)
            totalTStates += cpu.Step();

        Assert.Equal(14646, totalTStates);
        Assert.Equal((byte)0x5A, bus.Memory[0x4200]);
        Assert.Equal((ushort)0x0000, cpu.Registers.BC);
        Assert.Equal((ushort)0x4200, cpu.Registers.DE);
        Assert.Equal((ushort)0x4100, cpu.Registers.HL);
    }

    private static readonly Vector[] Vectors =
    [
        new([0x3E, 0x03, 0xC6, 0x02, 0x32, 0x00, 0x40, 0x76], 4, 31, 0x4000, 0x05, static (_, _) => { }),
        new([0x21, 0x00, 0x41, 0x11, 0x00, 0x42, 0x01, 0x01, 0x00, 0xED, 0xB0, 0x76], 5, 50, 0x4200, 0xA5,
            static (_, bus) => bus.Memory[0x4100] = 0xA5),
        new([0xDD, 0x21, 0x00, 0x40, 0x3E, 0x81, 0xDD, 0x77, 0x01, 0x06, 0x03,
            0xDD, 0x34, 0x01, 0x10, 0xFB, 0xDD, 0x7E, 0x01, 0x32, 0x01, 0x40, 0x76],
            13, 186, 0x4001, 0x84, static (_, _) => { })
    ];

    private sealed record Vector(
        byte[] Program,
        int Steps,
        int ExpectedCycles,
        ushort ResultAddress,
        byte ExpectedValue,
        Action<Z80Cpu, TestBus> Setup);

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }
}

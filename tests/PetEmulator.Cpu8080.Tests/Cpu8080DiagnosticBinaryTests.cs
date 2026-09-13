using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080DiagnosticBinaryTests
{
    private const ushort ComLoadAddress = 0x0100;
    private const ulong InstructionLimit = 2_000_000;

    [Test]
    public void Tst8080_passes_in_the_cpm_8080_harness()
    {
        var result = RunDiagnostic("TST8080.COM", "CPU IS OPERATIONAL");

        result.Output.Should().Contain("MICROCOSM ASSOCIATES 8080/8085 CPU DIAGNOSTIC");
        result.Output.Should().Contain("CPU IS OPERATIONAL");
        result.Output.Should().NotContain("ERROR");
        result.Cpu.InstructionCount.Should().Be(651);
        result.Cpu.CycleCount.Should().Be(4924);
    }

    [Test]
    public void EightyEightZeroPre_passes_in_the_cpm_8080_harness()
    {
        var result = RunDiagnostic("8080PRE.COM", "8080 Preliminary tests complete");

        result.Output.Should().Contain("8080 Preliminary tests complete");
        result.Output.Should().NotContain("ERROR");
        result.Cpu.InstructionCount.Should().Be(1061);
        result.Cpu.CycleCount.Should().Be(7817);
    }

    private static DiagnosticResult RunDiagnostic(string fileName, string completionText)
    {
        var memory = new DiagnosticMemory();
        var program = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
        program.CopyTo(memory.Bytes, ComLoadAddress);

        // Match the reference diagnostic harness: OUT 0 ends the run and
        // OUT 1 captures diagnostic output from C/E or the DE string.
        memory.Bytes[0x0000] = 0xD3;
        memory.Bytes[0x0001] = 0x00;
        memory.Bytes[0x0005] = 0xD3;
        memory.Bytes[0x0006] = 0x01;
        memory.Bytes[0x0007] = 0xC9;

        var ports = new DiagnosticPorts(memory);
        var cpu = new Cpu8080(memory, ports: ports);
        ports.Cpu = cpu;
        cpu.CpuState.PC = ComLoadAddress;

        while (!ports.Finished && !cpu.Halted && cpu.InstructionCount < InstructionLimit)
            cpu.StepInstruction();

        ports.Finished.Should().BeTrue($"{fileName} did not signal completion within {InstructionLimit:N0} instructions.");
        ports.Output.Should().Contain(completionText);
        return new DiagnosticResult(cpu, ports.Output);
    }

    private sealed record DiagnosticResult(Cpu8080 Cpu, string Output);

    private sealed class DiagnosticMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];
        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }

    private sealed class DiagnosticPorts(DiagnosticMemory memory) : IPortBus
    {
        public Cpu8080? Cpu { get; set; }
        public bool Finished { get; private set; }
        public string Output { get; private set; } = string.Empty;

        public byte Read(ushort port) => 0;

        public void Write(ushort port, byte value)
        {
            var cpu = Cpu ?? throw new InvalidOperationException("Diagnostic port is not attached to a CPU.");
            switch (port)
            {
                case 0:
                    Finished = true;
                    break;
                case 1 when cpu.CpuState.C == 2:
                    Output += (char)cpu.CpuState.E;
                    break;
                case 1 when cpu.CpuState.C == 9:
                {
                    var address = cpu.CpuState.DE;
                    do
                    {
                        Output += (char)memory.Bytes[address++];
                    }
                    while (memory.Bytes[address] != (byte)'$');
                    break;
                }
            }
        }
    }
}

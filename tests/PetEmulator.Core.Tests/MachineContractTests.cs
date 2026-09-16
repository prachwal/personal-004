using System.Collections;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpc464;
using PetEmulator.Cpc6128;
using PetEmulator.Core;
using PetEmulator.Kaypro;
using PetEmulator.Pet;
using PetEmulator.Trs80;
using PetEmulator.Vic20;

namespace PetEmulator.Core.Tests;

[TestFixture]
public sealed class MachineContractTests
{
    [Test]
    public void Contract_keeps_machine_lifecycle_cpu_agnostic()
    {
        IMachine machine = new TestMachine();

        machine.Name.Should().Be("test");
        machine.IsReady.Should().BeTrue();
        machine.Reset();
        machine.StepInstruction();
        machine.Run(2);
        machine.CycleCount.Should().Be(3);
    }

    [TestCaseSource(nameof(RealMachines))]
    public void Contract_is_implemented_by_every_real_machine(Func<IMachine> factory)
    {
        var machine = factory();

        machine.Name.Should().NotBeNullOrWhiteSpace();
        machine.IsReady.Should().BeTrue();
        machine.Processor.Should().NotBeNull();
        machine.Memory.Should().NotBeNull();

        machine.Reset();
        machine.CycleCount.Should().Be(0);
        machine.StepInstruction();
        machine.CycleCount.Should().BeGreaterThan(0);
    }

    private static IEnumerable RealMachines
    {
        get
        {
            var root = FindRepositoryRoot();
            yield return new TestCaseData((Func<IMachine>)(() =>
                new PetMachine(PetProfileCatalog.Pet2001_8, Path.Combine(root, "roms", "pet"))))
                .SetName("PET 2001-8 implements IMachine");
            yield return new TestCaseData((Func<IMachine>)(() =>
                new Vic20Machine(Path.Combine(root, "roms", "vic20"))))
                .SetName("VIC-20 implements IMachine");
            yield return new TestCaseData((Func<IMachine>)(() => CreateKaypro(root)))
                .SetName("Kaypro II implements IMachine");
            yield return new TestCaseData((Func<IMachine>)(() =>
                new Trs80Machine(File.ReadAllBytes(Path.Combine(root, "roms", "trs80", "model1-level2-v1.4.bin")))))
                .SetName("TRS-80 Model I implements IMachine");
            yield return new TestCaseData((Func<IMachine>)(() =>
                new Cpc464Machine(File.ReadAllBytes(Path.Combine(root, "roms", "cpc464", "cpc464.rom")))))
                .SetName("CPC464 implements IMachine");
            yield return new TestCaseData((Func<IMachine>)(() =>
                new Cpc6128Machine(File.ReadAllBytes(Path.Combine(root, "roms", "cpc6128", "cpc6128.rom")))))
                .SetName("CPC6128 implements IMachine");
        }
    }

    private static KayproMachine CreateKaypro(string root)
    {
        var machine = new KayproMachine();
        machine.LoadMonitorRom(File.ReadAllBytes(Path.Combine(root, "roms", "kaypro", "kaypro-81-149c.bin")));
        return machine;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("PetEmulator.slnx was not found.");
    }

    [Test]
    public void Shared_clock_is_monotonic_and_resettable()
    {
        var clock = new EmulationClock();

        clock.Advance(4);
        clock.Advance(7);

        clock.CycleCount.Should().Be(11);
        clock.Reset();
        clock.CycleCount.Should().Be(0);
    }

    [Test]
    public void Machine_clock_ticks_devices_at_configured_ratio_and_restores_remainder()
    {
        var ticks = 0;
        var clock = new MachineClock(4, () => ticks++);

        clock.Tick(10);

        ticks.Should().Be(2);
        clock.DeviceCycleRemainder.Should().Be(2);
        clock.Restore(1);
        clock.DeviceCycleRemainder.Should().Be(1);
    }

    [Test]
    public void Optional_capabilities_remain_separate_from_processor_lifecycle()
    {
        IPortBus ports = new TestPortBus();
        IInterruptLines lines = new TestInterruptLines();
        IWaitLine wait = new TestInterruptLines();
        IFirqProcessor firq = new TestFirqProcessor();

        ports.Read(0x12).Should().Be(0xA5);
        lines.IntAsserted.Should().BeFalse();
        lines.NmiAsserted.Should().BeFalse();
        wait.WaitAsserted.Should().BeFalse();
        firq.SetFIRQ(true);
    }

    private sealed class TestMachine : IMachine
    {
        public string Name => "test";
        public bool IsReady => true;
        public ulong CycleCount { get; private set; }
        public IProcessor Processor { get; } = new TestProcessor();
        public IMemoryBus Memory { get; } = new TestMemoryBus();

        public void Reset() => CycleCount = 0;

        public void StepInstruction() => CycleCount++;

        public void Run(ulong instructionCount)
        {
            for (var i = 0UL; i < instructionCount; i++)
                StepInstruction();
        }
    }

    private sealed class TestProcessor : IProcessor
    {
        public bool Halted => false;
        public ulong CycleCount => 0;
        public ulong InstructionCount => 0;
        public void Reset() { }
        public void StepInstruction() { }
        public void SetIRQ(bool active) { }
        public void SetNMI(bool active) { }
    }

    private sealed class TestMemoryBus : IMemoryBus
    {
        public byte Read(ushort address) => 0;
        public void Write(ushort address, byte value) { }
    }

    private sealed class TestPortBus : IPortBus
    {
        public byte Read(ushort port) => 0xA5;
        public void Write(ushort port, byte value) { }
    }

    private sealed class TestInterruptLines : IInterruptLines, IWaitLine
    {
        public bool IntAsserted => false;
        public bool NmiAsserted => false;
        public bool WaitAsserted => false;
    }

    private sealed class TestFirqProcessor : IFirqProcessor
    {
        public void SetFIRQ(bool active) { }
    }
}

using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioGroup1Tests
{
    [Test]
    public void DeviceUsesIndependentAAndBDataAndControlPorts()
    {
        var pio = new Z80PioDevice(basePort: 0x40);

        pio.WritePort(0x40, 0x12);
        pio.WritePort(0x41, 0x34);
        pio.WritePort(0x42, 0x00); // vector word on A control
        pio.WritePort(0x43, 0x80); // vector word on B control

        pio.ReadPort(0x40).Should().Be(0x00); // input latch is independent of output latch
        pio.ReadPort(0x41).Should().Be(0x00);
        pio.Ports.Should().Equal(0x40, 0x41, 0x42, 0x43);
        pio.ReadPort(0x44).Should().Be(0xFF);
    }

    [Test]
    public void ResetRestoresInputModesAndClearsParserAndInterruptState()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(0x02, 0xFF); // Mode 3, awaiting I/O select
        pio.WritePort(0x02, 0xAA);
        pio.WritePort(0x03, 0x00); // vector

        pio.Reset();
        var state = pio.CaptureState();

        state.PortA.Mode.Should().Be(Z80PioMode.Input);
        state.PortA.DirectionMask.Should().Be(0xFF);
        state.PortA.ControlPhase.Should().Be(Z80PioControlPhase.Ready);
        state.PortA.InterruptPending.Should().BeFalse();
        state.InterruptVector.Should().Be(0);
        state.InterruptInService.Should().BeFalse();
    }

    [Test]
    public void ControlParserIsIndependentAndSnapshotRoundTripsWithoutAliasing()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(0x02, 0xFF); // A Mode 3
        pio.WritePort(0x02, 0x0F); // A I/O select
        pio.WritePort(0x03, 0x80); // B vector

        var snapshot = pio.CaptureState();
        pio.WritePort(0x02, 0x00);
        pio.RestoreState(snapshot);

        var restored = pio.CaptureState();
        restored.Should().BeEquivalentTo(snapshot);
        restored.PortA.Should().NotBeSameAs(snapshot.PortA);
        restored.PortB.Should().NotBeSameAs(snapshot.PortB);
        restored.PortA.Mode.Should().Be(Z80PioMode.BitControl);
        restored.InterruptVector.Should().Be(0x80);
    }

    [Test]
    public void ControlWordsExposeMode3DirectionAndInterruptMaskPhases()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(0x02, 0xFF); // Mode 3
        pio.WritePort(0x02, 0x5A); // input/output selection
        pio.WritePort(0x02, 0x97); // interrupt control with mask follows
        pio.WritePort(0x02, 0xC3); // mask

        var state = pio.CaptureState().PortA;
        state.Mode.Should().Be(Z80PioMode.BitControl);
        state.DirectionMask.Should().Be(0x5A);
        state.InterruptControl.Should().Be(0x97);
        state.InterruptMask.Should().Be(0xC3);
        state.ControlPhase.Should().Be(Z80PioControlPhase.Ready);
    }

    [TestCase(Z80PioRevision.Z8420)]
    [TestCase(Z80PioRevision.Z84C20)]
    public void RevisionProfilesHaveExplicitFallbackValues(Z80PioRevision revision)
    {
        var pio = new Z80PioDevice(revision: revision);

        pio.Profile.Revision.Should().Be(revision);
        pio.Profile.InvalidReadValue.Should().Be(0xFF);
        pio.Profile.UnsupportedRegisterReadValue.Should().Be(0x00);
    }

    [Test]
    public void NmosAndCmosProfilesExposeElectricalDifferencesWithoutChangingPortContract()
    {
        var nmos = new Z80PioDevice(revision: Z80PioRevision.Z8420).Profile;
        var cmos = new Z80PioDevice(revision: Z80PioRevision.Z84C20).Profile;

        nmos.Technology.Should().Be(Z80PioTechnology.Nmos);
        nmos.MaximumClockMHz.Should().BeApproximately(6.17, 0.001);
        nmos.SupportsStaticClock.Should().BeFalse();
        cmos.Technology.Should().Be(Z80PioTechnology.Cmos);
        cmos.MaximumClockMHz.Should().Be(8.0);
        cmos.SupportsStaticClock.Should().BeTrue();
        nmos.InvalidReadValue.Should().Be(cmos.InvalidReadValue);
        nmos.UnsupportedRegisterReadValue.Should().Be(cmos.UnsupportedRegisterReadValue);
    }

    [Test]
    public void TickRejectsNegativeTimeWithoutInventingClockState()
    {
        var pio = new Z80PioDevice();

        FluentActions.Invoking(() => pio.Tick(-1))
            .Should().Throw<ArgumentOutOfRangeException>();
        pio.CaptureState().PortA.Ready.Should().BeFalse();
    }

    [Test]
    public void ChipAssemblyDoesNotDependOnKayproOrDesktop()
    {
        var references = typeof(Z80PioDevice).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        references.Should().NotContain("PetEmulator.Kaypro");
        references.Should().NotContain("PetEmulator.Desktop");
    }
}

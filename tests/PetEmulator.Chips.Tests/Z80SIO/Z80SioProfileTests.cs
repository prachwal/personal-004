using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioProfileTests
{
    [TestCase(Z80SioRevision.Sio0, false, false)]
    [TestCase(Z80SioRevision.Sio1, false, true)]
    [TestCase(Z80SioRevision.Sio2, true, true)]
    [TestCase(Z80SioRevision.Z8440, true, true)]
    public void RevisionProfileExposesItsDocumentedCapabilities(
        Z80SioRevision revision,
        bool supportsRr3,
        bool supportsDaisyChain)
    {
        var profile = Z80SioProfile.For(revision);

        profile.SupportsRr3.Should().Be(supportsRr3);
        profile.SupportsDaisyChain.Should().Be(supportsDaisyChain);
        profile.InvalidReadValue.Should().Be(0xFF);
        profile.UnsupportedRegisterReadValue.Should().Be(0x00);
    }

    [TestCase(Z80SioRevision.Sio0, false)]
    [TestCase(Z80SioRevision.Sio1, false)]
    [TestCase(Z80SioRevision.Sio2, true)]
    [TestCase(Z80SioRevision.Z8440, true)]
    public void Rr3IsEitherImplementedOrReturnsTheProfileUnsupportedValue(
        Z80SioRevision revision,
        bool supported)
    {
        var sio = new Z80Sio(revision: revision);
        sio.WritePort(0x02, 0x03); // RR3

        sio.ReadPort(0x02).Should().Be(supported
            ? (byte)0
            : sio.Profile.UnsupportedRegisterReadValue);
    }

    [Test]
    public void InvalidPortAndUnsupportedRegisterReadsAreStable()
    {
        var sio = new Z80Sio(revision: Z80SioRevision.Z8440);

        sio.ReadPort(0xFF).Should().Be(sio.Profile.InvalidReadValue);
        sio.WritePort(0x02, 0x04); // RR4 is outside the implemented RR0-RR3 set
        sio.ReadPort(0x02).Should().Be(sio.Profile.UnsupportedRegisterReadValue);
    }
}

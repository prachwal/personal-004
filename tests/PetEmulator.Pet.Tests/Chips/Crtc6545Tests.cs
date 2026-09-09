using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;

namespace PetEmulator.Pet.Tests.Chips;

[TestFixture]
public sealed class Crtc6545Tests
{
    [Test]
    public void SelectsRegistersAndAppliesRegisterMasks()
    {
        var crtc = new Crtc6545();

        WriteRegister(crtc, 0, 0xFF);
        WriteRegister(crtc, 4, 0xFF);
        WriteRegister(crtc, 5, 0xFF);
        WriteRegister(crtc, 6, 0xFF);
        WriteRegister(crtc, 7, 0xFF);
        WriteRegister(crtc, 9, 0xFF);
        WriteRegister(crtc, 11, 0xFF);
        WriteRegister(crtc, 12, 0xFF);
        WriteRegister(crtc, 14, 0xFF);

        ReadRegister(crtc, 0).Should().Be(0xFF);
        ReadRegister(crtc, 4).Should().Be(0x7F);
        ReadRegister(crtc, 5).Should().Be(0x1F);
        ReadRegister(crtc, 6).Should().Be(0x7F);
        ReadRegister(crtc, 7).Should().Be(0x7F);
        ReadRegister(crtc, 9).Should().Be(0x1F);
        ReadRegister(crtc, 11).Should().Be(0x1F);
        ReadRegister(crtc, 12).Should().Be(0x3F);
        ReadRegister(crtc, 14).Should().Be(0x3F);
    }

    [Test]
    public void ResetClearsRegistersCountersAndSignals()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 3);
        WriteRegister(crtc, 1, 2);
        crtc.Tick();
        crtc.Reset();

        crtc.SelectedRegister.Should().Be(0);
        ReadRegister(crtc, 0).Should().Be(0);
        crtc.MACounter.Should().Be(0);
        crtc.RACounter.Should().Be(0);
        (crtc.HSync || crtc.VSync || crtc.DisplayEnable || crtc.VerticalBlanking || crtc.CursorEnable).Should().BeFalse();
    }

    [Test]
    public void EmitsHorizontalSyncAtConfiguredPositionAndWidth()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 4);
        WriteRegister(crtc, 2, 2);
        WriteRegister(crtc, 3, 0x02);

        crtc.Tick();
        crtc.HSync.Should().BeFalse();
        crtc.Tick();
        crtc.HSync.Should().BeFalse();
        crtc.Tick();
        crtc.HSync.Should().BeTrue();
        crtc.Tick();
        crtc.HSync.Should().BeTrue();
        crtc.Tick();
        crtc.HSync.Should().BeFalse();
    }

    [Test]
    public void EmitsDisplayEnableAndVerticalBlankingForFortyByTwentyFiveProfile()
    {
        var crtc = new Crtc6545();
        Configure40x25(crtc);

        crtc.Tick();
        crtc.DisplayEnable.Should().BeTrue();
        crtc.VerticalBlanking.Should().BeFalse();

        Tick(crtc, 40 * 25 - 1);
        crtc.Tick();
        crtc.DisplayEnable.Should().BeFalse();
        crtc.VerticalBlanking.Should().BeTrue();
    }

    [Test]
    public void AdvancesMemoryAndRasterAddressesAndRestartsAtDisplayStart()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 2);
        WriteRegister(crtc, 1, 2);
        WriteRegister(crtc, 4, 1);
        WriteRegister(crtc, 6, 2);
        WriteRegister(crtc, 9, 1);
        WriteRegister(crtc, 12, 0x12);
        WriteRegister(crtc, 13, 0x34);

        crtc.Tick();
        crtc.MACounter.Should().Be(0x1234);
        crtc.Tick();
        crtc.MACounter.Should().Be(0x1235);
        crtc.Tick();
        crtc.RACounter.Should().Be(1);
        Tick(crtc, 9);
        crtc.Tick();
        crtc.RACounter.Should().Be(0);
        crtc.MACounter.Should().Be(0x1234);
    }

    [Test]
    public void UsesDisplayStartAndCursorAddressWithCursorMode()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 1);
        WriteRegister(crtc, 1, 1);
        WriteRegister(crtc, 4, 0);
        WriteRegister(crtc, 6, 1);
        WriteRegister(crtc, 12, 0x02);
        WriteRegister(crtc, 13, 0x34);
        WriteRegister(crtc, 14, 0x02);
        WriteRegister(crtc, 15, 0x34);
        WriteRegister(crtc, 10, 0x00);
        WriteRegister(crtc, 11, 0x00);

        crtc.Tick();
        crtc.DisplayStartAddress.Should().Be(0x0234);
        crtc.CursorAddress.Should().Be(0x0234);
        crtc.CursorEnable.Should().BeTrue();

        WriteRegister(crtc, 10, 0x20);
        crtc.Reset();
        WriteRegister(crtc, 0, 1);
        WriteRegister(crtc, 1, 1);
        WriteRegister(crtc, 4, 0);
        WriteRegister(crtc, 6, 1);
        WriteRegister(crtc, 14, 0);
        WriteRegister(crtc, 15, 0);
        WriteRegister(crtc, 10, 0x20);
        crtc.Tick();
        crtc.CursorEnable.Should().BeFalse();
    }

    [Test]
    public void CapturesLightPenAddressAndReportsItsStatus()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 1);
        WriteRegister(crtc, 1, 1);
        WriteRegister(crtc, 12, 0x03);
        WriteRegister(crtc, 13, 0x21);
        crtc.Tick();
        crtc.LightPenStrobe();

        (crtc.Read(0) & 0x40).Should().Be(0x40);
        ReadRegister(crtc, 16).Should().Be(0x03);
        ReadRegister(crtc, 17).Should().Be(0x21);
        crtc.LightPenRegistered.Should().BeFalse();
    }

    [Test]
    public void TickWithCycleCountAdvancesTheSameAsRepeatedSingleTicks()
    {
        var crtc = new Crtc6545();
        Configure40x25(crtc);
        var expected = new Crtc6545();
        Configure40x25(expected);

        crtc.Tick(5);
        Tick(expected, 5);

        crtc.MACounter.Should().Be(expected.MACounter);
        crtc.RACounter.Should().Be(expected.RACounter);
        crtc.DisplayEnable.Should().Be(expected.DisplayEnable);
    }

    [Test]
    public void InterlaceVideoMode_StepsRasterByTwoAndTogglesFieldEachFrame()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 0); // H total: 1 char clock per line
        WriteRegister(crtc, 1, 1);
        WriteRegister(crtc, 4, 0); // V total: 1 character row per frame
        WriteRegister(crtc, 6, 1);
        WriteRegister(crtc, 9, 3); // max raster address (4 lines/row in non-interlace)
        WriteRegister(crtc, 8, 0x03); // interlace sync and video

        crtc.Tick();
        crtc.RACounter.Should().Be(2, "raster steps by 2 (not 1) in interlace video mode");

        crtc.Tick(); // rolls into a new frame: field toggles, raster restarts on line 1 not 0
        crtc.RACounter.Should().Be(1, "the first field starts on the odd raster line");

        crtc.Tick();
        crtc.RACounter.Should().Be(3);

        crtc.Tick(); // second frame rollover: field toggles back
        crtc.RACounter.Should().Be(0, "the next field starts back on the even raster line");
    }

    [Test]
    public void DisplayEnableSkew_DelaysOutputByConfiguredClockCount()
    {
        var crtc = new Crtc6545();
        WriteRegister(crtc, 0, 3); // H total 4 char clocks
        WriteRegister(crtc, 1, 2); // 2 displayed
        WriteRegister(crtc, 4, 0);
        WriteRegister(crtc, 6, 1);
        WriteRegister(crtc, 9, 0);
        WriteRegister(crtc, 8, 0x80); // display-enable skew = 2 clocks (bits 7:6), no interlace

        // Raw (unskewed) window would be true,true,false,false - skew delays it by 2 clocks.
        crtc.Tick();
        crtc.DisplayEnable.Should().BeFalse("skewed output hasn't caught up to the raw rising edge yet");
        crtc.Tick();
        crtc.DisplayEnable.Should().BeFalse();
        crtc.Tick();
        crtc.DisplayEnable.Should().BeTrue("2 clocks after the raw signal rose");
        crtc.Tick();
        crtc.DisplayEnable.Should().BeTrue("still delayed-high 2 clocks after the raw signal fell");
    }

    [Test]
    public void KeepsTwoInstancesIndependentWhenMappedAtDifferentBaseAddresses()
    {
        var first = new Crtc6545("CRTC1", 0xE880);
        var second = new Crtc6545("CRTC2", 0xE890);

        first.Write(0xE880, 0);
        first.Write(0xE881, 0x27);
        second.Write(0xE890, 0);
        second.Write(0xE891, 0x50);

        first.Read(0xE881).Should().Be(0x27);
        second.Read(0xE891).Should().Be(0x50);
        first.Name.Should().Be("CRTC1");
        second.Name.Should().Be("CRTC2");
    }

    private static void Configure40x25(Crtc6545 crtc)
    {
        WriteRegister(crtc, 0, 39);
        WriteRegister(crtc, 1, 40);
        WriteRegister(crtc, 4, 26);
        WriteRegister(crtc, 6, 25);
        WriteRegister(crtc, 9, 0);
    }

    private static byte ReadRegister(Crtc6545 crtc, byte register)
    {
        crtc.Write(0, register);
        return crtc.Read(1);
    }

    private static void WriteRegister(Crtc6545 crtc, byte register, byte value)
    {
        crtc.Write(0, register);
        crtc.Write(1, value);
    }

    private static void Tick(Crtc6545 crtc, int count)
    {
        for (var index = 0; index < count; index++)
            crtc.Tick();
    }
}

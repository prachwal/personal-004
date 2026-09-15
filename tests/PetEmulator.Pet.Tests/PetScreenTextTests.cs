using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Roms;
using PetEmulator.Pet.Tests.Roms;
using PetEmulator.TestSupport;

namespace PetEmulator.Pet.Tests;

/// <summary>Proves <see cref="CbmScreenCode"/> decodes real PET screen RAM as readable text - a
/// direct memory-to-char lookup, not image recognition, which is all a PET/VIC-20 needs since their
/// video RAM already holds character codes (unlike CPC464, which has no character buffer at all and
/// needs pixel-glyph OCR - see ScreenTextOcr). This also upgrades what PetMachineTests' own
/// ReadyBytes/PressPlayBytes/SearchingBytes byte-array literals were standing in for: those still
/// work (this table doesn't replace them), but a new test can now assert on human-readable text
/// instead of hand-decoding "READY." into [0x12, 0x05, 0x01, 0x04, 0x19, 0x2E].</summary>
public sealed class PetScreenTextTests
{
    [Test]
    public void RealBoot_ScreenTextContainsReadyPrompt()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);

        machine.RunUntil(_ => ScreenText(machine, profile).Contains("READY."), 1_000_000)
            .Should().BeTrue("BASIC should boot to a 'READY.' prompt");
    }

    [Test]
    public void TypedLine_ExecutesInBasic_VisibleAsRealDecodedText()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        machine.RunUntil(mem => ScreenText(machine, profile).Contains("READY."), 1_000_000).Should().BeTrue("must boot first");

        TextTyper.Type(machine, map, "PRINT2+2\n");
        machine.Run(50_000);

        ScreenText(machine, profile).Should().Contain("4", "PRINT2+2 typed through the real keyboard matrix should actually evaluate and print '4'");
    }

    private static string ScreenText(PetMachine machine, PetProfile profile) =>
        CbmScreenCode.ToText(Snapshot(machine, profile), profile.Columns, profile.Rows);

    private static byte[] Snapshot(PetMachine machine, PetProfile profile)
    {
        var bytes = new byte[profile.Columns * profile.Rows];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(profile.VideoRamStart + i));
        return bytes;
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}

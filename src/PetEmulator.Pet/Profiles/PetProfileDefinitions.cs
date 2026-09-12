using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet.Profiles;

[PetProfile("pet-2001-8", "PET 2001-8 / BASIC 1 / 40x25", PetProfileFamily.Pet20xx)]
public sealed class Pet2001_8Profile : PetProfileDefinition
{
    public Pet2001_8Profile() : base("BASIC 1", 40, 25, 0x2000, 0x8000, 0x0400, false, "characters-1.901447-08.bin", PetRomManifest.Pet2001_8, PetCursorTracking.Basic1Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        Profile.KeyboardRevision = PetKeyboardRevision.Pet2001Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.InternalAndExternal;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Pet2001;
    }
}

[PetProfile("pet-2001-32", "PET 2001-32 / BASIC 2 / 40x25", PetProfileFamily.Pet20xx)]
public sealed class Pet2001_32Profile : PetProfileDefinition
{
    public Pet2001_32Profile() : base("BASIC 2", 40, 25, 0x8000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        Profile.KeyboardRevision = PetKeyboardRevision.Pet2001Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Pet2001;
    }
}

[PetProfile("cbm-3008", "CBM 3008 / BASIC 2 / 40x25", PetProfileFamily.Cbm30xx)]
public sealed class Cbm3008Profile : PetProfileDefinition
{
    public Cbm3008Profile() : base("BASIC 2", 40, 25, 0x2000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        Profile.RomDirectoryOverride = "pet-2001-32";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm3000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm3000;
    }
}

[PetProfile("cbm-3016", "CBM 3016 / BASIC 2 / 40x25", PetProfileFamily.Cbm30xx)]
public sealed class Cbm3016Profile : PetProfileDefinition
{
    public Cbm3016Profile() : base("BASIC 2", 40, 25, 0x4000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        Profile.RomDirectoryOverride = "pet-2001-32";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm3000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm3000;
    }
}

[PetProfile("cbm-3032", "CBM 3032 / BASIC 2 / 40x25", PetProfileFamily.Cbm30xx)]
public sealed class Cbm3032Profile : PetProfileDefinition
{
    public Cbm3032Profile() : base("BASIC 2", 40, 25, 0x8000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        Profile.RomDirectoryOverride = "pet-2001-32";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm3000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm3000;
    }
}

[PetProfile("cbm-4032", "CBM 4032 / BASIC 4 / 40x25", PetProfileFamily.Cbm40xx)]
public sealed class Cbm4032Profile : PetProfileDefinition
{
    public Cbm4032Profile() : base("BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm4000;
    }
}

[PetProfile("cbm-4008-crtc-40n60", "CBM 4008 / CRTC 40n60 / BASIC 4 / 40x25", PetProfileFamily.Cbm40xx)]
public sealed class Cbm4008Crtc40N60Profile : PetProfileDefinition
{
    public Cbm4008Crtc40N60Profile() : base("BASIC 4", 40, 25, 0x2000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-4032";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm4000;
    }
}

[PetProfile("cbm-4016-crtc-40n60", "CBM 4016 / CRTC 40n60 / BASIC 4 / 40x25", PetProfileFamily.Cbm40xx)]
public sealed class Cbm4016Crtc40N60Profile : PetProfileDefinition
{
    public Cbm4016Crtc40N60Profile() : base("BASIC 4", 40, 25, 0x4000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-4032";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm4000;
    }
}

[PetProfile("cbm-4032-crtc-40n50", "CBM 4032 / CRTC 40n50 / BASIC 4 / 40x25", PetProfileFamily.Cbm40xx)]
public sealed class Cbm4032Crtc40N50Profile : PetProfileDefinition
{
    public Cbm4032Crtc40N50Profile() : base("BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032_40N50, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-4032";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm4000;
    }
}

[PetProfile("cbm-4032-crtc-40b50", "CBM 4032 / CRTC 40b50 / BASIC 4 / 40x25", PetProfileFamily.Cbm40xx)]
public sealed class Cbm4032Crtc40B50Profile : PetProfileDefinition
{
    public Cbm4032Crtc40B50Profile() : base("BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032_40B50, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-4032";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm4000;
    }
}

[PetProfile("cbm-4032-crtc-40b60", "CBM 4032 / CRTC 40b60 / BASIC 4 / 40x25", PetProfileFamily.Cbm40xx)]
public sealed class Cbm4032Crtc40B60Profile : PetProfileDefinition
{
    public Cbm4032Crtc40B60Profile() : base("BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032_40B60, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-4032";
    }
}

[PetProfile("cbm-8032", "CBM 8032 / BASIC 4 / 80x25", PetProfileFamily.Cbm80xx)]
public sealed class Cbm8032Profile : PetProfileDefinition
{
    public Cbm8032Profile() : base("BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8032, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm8000Business;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm8000;
    }
}

[PetProfile("cbm-8032-crtc-80b50", "CBM 8032 / CRTC 80b50 / BASIC 4 / 80x25", PetProfileFamily.Cbm80xx)]
public sealed class Cbm8032Crtc80B50Profile : PetProfileDefinition
{
    public Cbm8032Crtc80B50Profile() : base("BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8032_80B50, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm8000Business;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm8000;
    }
}

[PetProfile("cbm-8016-converted-80n50", "CBM 8016 / converted 80n50 editor / BASIC 4 / 80x25", PetProfileFamily.Cbm80xx)]
public sealed class Cbm8016Converted80N50Profile : PetProfileDefinition
{
    public Cbm8016Converted80N50Profile() : base("BASIC 4", 80, 25, 0x4000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8016Converted80N50, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
    }
}

[PetProfile("pet-converted-80n-unknown", "PET / converted unknown 80n editor / BASIC 4 / 80x25", PetProfileFamily.Cbm80xx)]
public sealed class Converted80NUnknownProfile : PetProfileDefinition
{
    public Converted80NUnknownProfile() : base("BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Converted80NUnknown, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
    }
}

[PetProfile("cbm-8096-french", "CBM 8096 French / BASIC 4 / 80x25 / placeholder", PetProfileFamily.Cbm80xx, Status = PetProfileStatus.Placeholder)]
public sealed class Cbm8096FrenchProfile : PetProfileDefinition
{
    public Cbm8096FrenchProfile() : base("BASIC 4", 80, 25, 0x10000, 0x8000, 0x0800, true, "characters-french.bin", PetRomManifest.Cbm8096French, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
        Profile.MemoryExpansion = new PetMemoryExpansion(0xFFF0, 0x8000);
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm8000Business;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm8000;
    }
}

[PetProfile("cbm-8296", "CBM 8296 / BASIC 4 / 80x25 / placeholder", PetProfileFamily.Cbm80xx, Status = PetProfileStatus.Placeholder)]
public sealed class Cbm8296Profile : PetProfileDefinition
{
    public Cbm8296Profile() : base("BASIC 4", 80, 25, 0x10000, 0x8000, 0x0800, true, "characters-324242-01.bin", PetRomManifest.Cbm8296, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
        Profile.MemoryExpansion = new PetMemoryExpansion(0xFFF0, 0x10000);
        Profile.KeyboardRevision = PetKeyboardRevision.Cbm8000Business;
        Profile.CassetteConfiguration = PetCassetteConfiguration.ExternalOnly;
        Profile.ConnectorConfiguration = PetConnectorConfiguration.Cbm8000;
    }
}

[PetProfile("superpet-6502", "SuperPET / 6502 mode / 80x25 / placeholder", PetProfileFamily.SuperPet, Status = PetProfileStatus.Placeholder)]
public sealed class SuperPet6502Profile : PetProfileDefinition
{
    public SuperPet6502Profile() : base("BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters.901640-01.bin", PetRomManifest.Cbm8032, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
        Profile.ExpansionRomManifest = PetRomManifest.SuperPetWaterloo50Hz;
        Profile.AciaBaseAddress = SuperPetMemoryMap.AciaBaseAddress;
        Profile.InitialProcessor = SuperPetProcessor.Mos6502;
    }
}

[PetProfile("superpet", "SuperPET / Waterloo 6809 / 80x25 / placeholder", PetProfileFamily.SuperPet, Status = PetProfileStatus.Placeholder)]
public sealed class SuperPet6809Profile : PetProfileDefinition
{
    public SuperPet6809Profile() : base("BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters.901640-01.bin", PetRomManifest.Cbm8032, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        Profile.RomDirectoryOverride = "cbm-8032";
        Profile.ExpansionRomManifest = PetRomManifest.SuperPetWaterloo50Hz;
        Profile.AciaBaseAddress = SuperPetMemoryMap.AciaBaseAddress;
        Profile.InitialProcessor = SuperPetProcessor.Motorola6809;
        Profile.ScreenCharacterEncoding = PetScreenCharacterEncoding.Ascii;
        Profile.CursorStrategy = PetCursorStrategy.ScreenHighBit;
    }
}

namespace PetEmulator.Pet;

/// <summary>Address map of the stock SuperPET expansion board.</summary>
public static class SuperPetMemoryMap
{
    public const ushort ExpansionRamWindow = 0x9000;
    public const ushort ExpansionRamWindowLength = 0x1000;
    public const ushort BankSelectRegister = 0xEFFC;
    public const byte BankCount = 16;
    public const ushort AciaBaseAddress = 0xEFF0;
    public const ushort AciaLength = 4;
    public const ushort ProtectionDongleBaseAddress = 0xEFE0;
    public const ushort ProtectionDongleLength = 4;
}

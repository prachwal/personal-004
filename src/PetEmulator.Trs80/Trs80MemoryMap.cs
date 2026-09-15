namespace PetEmulator.Trs80;

public static class Trs80MemoryMap
{
    public const ushort RomEnd = 0x3000;
    public const ushort FdcStart = 0x37E0;
    public const ushort FdcEnd = 0x37EF;
    public const ushort PrinterStart = 0x37E8;
    public const ushort PrinterEnd = 0x37E8;
    public const ushort KeyboardStart = 0x3800;
    public const ushort KeyboardEnd = 0x38FF;
    public const ushort VideoRamStart = 0x3C00;
    public const ushort VideoRamEnd = 0x3FFF;
    public const byte CassettePort = 0xFF;
}

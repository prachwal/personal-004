namespace PetEmulator.Vic20;

/// <summary>Real, unexpanded VIC-20 hardware addresses (see docs/vic20-migration-plan.md step 5 -
/// expansion-preset banking ($3K/$8K/$16K/$24K/All) deliberately out of scope for v1: an
/// unexpanded machine boots BASIC and runs small programs exactly like real unexpanded
/// hardware does).</summary>
public static class Vic20MemoryMap
{
    public const ushort ZeroPageRamStart = 0x0000;
    public const int ZeroPageRamSize = 0x0400; // 1K: zero page, stack, low system RAM

    public const ushort BuiltinRamStart = 0x1000;
    public const int BuiltinRamSize = 0x1000; // 4K built-in RAM block (includes default screen RAM at $1E00)

    public const ushort CharRomStart = 0x8000;

    public const ushort VicBaseAddress = 0x9000;

    public const ushort Via1BaseAddress = 0x9110;
    public const ushort Via2BaseAddress = 0x9120;

    public const ushort ColorRamStart = 0x9400;
    public const int ColorRamSize = 0x0400;

    public const ushort BasicRomStart = 0xC000;

    public const ushort KernalRomStart = 0xE000;
}

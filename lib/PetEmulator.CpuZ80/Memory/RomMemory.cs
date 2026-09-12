using PetEmulator.CpuZ80.Bus;

namespace PetEmulator.CpuZ80.Memory;

// ponytail: ModelIRomSize/ModelIIIRomSize are TRS-80-named constants living
// in an otherwise CPU-agnostic class - real churn to rename (22 call sites
// across src/tests reference them), no behavior gain from doing so. Leave
// as-is; a second Z80 platform can pass its own literal expectedSize.
public sealed class RomMemory : IMemoryBus
{
    public const int ModelIRomSize = 0x3000;

    /// <summary>Model III's ROM is 14K (0x0000-0x37FF) - a real hardware fact, not a Model I ROM with padding; see the Model III plan's Phase B.</summary>
    public const int ModelIIIRomSize = 0x3800;

    private readonly byte[] data;

    /// <param name="expectedSize">
    /// Defaults to <see cref="ModelIRomSize"/> for backward compatibility
    /// with every Model I caller that existed before a second model did -
    /// pass <see cref="ModelIIIRomSize"/> (or any other real dump size) to
    /// validate a different one instead.
    /// </param>
    public RomMemory(byte[] image, int expectedSize = ModelIRomSize)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length != expectedSize)
            throw new ArgumentOutOfRangeException(nameof(image), $"Expected a {expectedSize}-byte ROM image.");

        data = image.ToArray();
    }

    public byte Read(ushort address) => data[address];

    public void Write(ushort address, byte value)
    {
    }
}

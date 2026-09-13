namespace PetEmulator.Chips;

/// <summary>Compatibility contract for FD1793 callers.</summary>
public interface IFD1793DiskImage : IFD1791DiskImage
{
}

/// <summary>Optional extension for disk images with independently addressable sides.</summary>
public interface IFD1793SidedDiskImage : IFD1793DiskImage, IFD1791SidedDiskImage
{
}

namespace PetEmulator.Pet.CbmDos;

public readonly record struct D64SectorAddress(int Track, int Sector);

public readonly record struct D64SectorInfo(int Track, int Sector, bool IsAllocated);

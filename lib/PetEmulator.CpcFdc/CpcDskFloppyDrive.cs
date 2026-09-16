namespace PetEmulator.CpcFdc;

public sealed class DskFloppyDrive(DskDiskImage image) : IFloppyDrive
{
    public DskDiskImage Image { get; } = image ?? throw new ArgumentNullException(nameof(image));
    public bool MotorOn { get; set; }
    public bool Ready => MotorOn;
    public bool WriteProtected { get; set; }
    public bool TrackZero => Cylinder == 0;
    public bool TwoSided => Image.HeadCount > 1;
    public bool HeadLoaded => MotorOn;
    public bool Index => MotorOn;
    public int Cylinder { get; private set; }
    public int Head { get; private set; }
    public byte LastStatus1 { get; private set; }
    public byte LastStatus2 { get; private set; }

    public bool Seek(int cylinder)
    {
        if (cylinder < 0 || cylinder >= Image.TrackCount) return false;
        Cylinder = cylinder;
        return true;
    }

    public void SelectHead(byte head) => Head = head;

    public byte? NextSectorId(byte cylinder, byte head, byte sector, byte sizeCode)
    {
        var sectors = Image.FindTrack(cylinder, head)?.Sectors;
        if (sectors is null) return null;
        int index = sectors.FindIndex(s => s.Id == sector && s.SizeCode == sizeCode);
        return index >= 0 && index + 1 < sectors.Count ? sectors[index + 1].Id : null;
    }

    public bool IsDeletedSector(byte cylinder, byte head, byte sector, byte sizeCode) =>
        Image.FindTrack(cylinder, head)?.Sectors.FirstOrDefault(s => s.Id == sector && s.SizeCode == sizeCode) is { } found &&
        (found.Status2 & 0x40) != 0;

    public bool ReadSector(byte cylinder, byte head, byte sector, byte sizeCode, Span<byte> data)
    {
        LastStatus1 = LastStatus2 = 0;
        if (!Ready || cylinder != Cylinder || head >= Image.HeadCount) return false;
        DskSector? found = Image.FindTrack(cylinder, head)?.Sectors.FirstOrDefault(s => s.Id == sector && s.SizeCode == sizeCode);
        if (found is null)
        {
            LastStatus1 = 0x04;
            return false;
        }
        LastStatus1 = found.Status1;
        LastStatus2 = found.Status2;
        if (data.Length < found.Data.Length) return false;
        found.Data.AsSpan().CopyTo(data);
        return true;
    }

    public bool WriteSector(byte cylinder, byte head, byte sector, byte sizeCode, ReadOnlySpan<byte> data) =>
        Ready && !WriteProtected && cylinder == Cylinder && head < Image.HeadCount && Image.TryWrite(cylinder, head, sector, sizeCode, data);

    public bool FormatTrack(byte head, byte sizeCode, byte sectorCount, ReadOnlySpan<byte> ids, byte fill)
    {
        if (!Ready || WriteProtected || head >= Image.HeadCount) return false;
        DskTrack? track = Image.FindTrack((byte)Cylinder, head);
        if (track is null || ids.Length < sectorCount * 4) return false;
        track.Sectors.Clear();
        for (int i = 0; i < sectorCount; i++)
        {
            int offset = i * 4;
            int length = 128 << ids[offset + 3];
            track.Sectors.Add(new DskSector(ids[offset + 2], ids[offset + 3], 0, 0, Enumerable.Repeat(fill, length).ToArray()));
        }
        return true;
    }

    public DskFloppyDriveSnapshot CaptureState() => new()
    {
        Image = Image.CaptureState(), MotorOn = MotorOn, WriteProtected = WriteProtected,
        Cylinder = Cylinder, Head = Head,
    };

    public static DskFloppyDrive RestoreState(DskFloppyDriveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var drive = new DskFloppyDrive(DskDiskImage.RestoreState(snapshot.Image))
        { MotorOn = snapshot.MotorOn, WriteProtected = snapshot.WriteProtected };
        drive.Seek(snapshot.Cylinder); drive.SelectHead((byte)snapshot.Head);
        return drive;
    }
}

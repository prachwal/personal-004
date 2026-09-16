namespace PetEmulator.Chips;

/// <summary>
/// Functional FD1793/WD179x floppy-disk controller model.
/// Register offsets are command/status, track, sector and data (0..3);
/// drive selection is exposed separately because its latch is board-specific.
/// </summary>
public class FD1791
{
    public const byte CommandStatusRegister = 0x00;
    public const byte TrackRegister = 0x01;
    public const byte SectorRegister = 0x02;
    public const byte DataRegister = 0x03;

    public const byte BusyFlag = 0x01;
    public const byte DataRequestFlag = 0x02;
    public const byte TrackZeroFlag = 0x04;
    public const byte LostDataFlag = 0x04;
    public const byte RecordNotFoundFlag = 0x10;
    public const byte RecordTypeFlag = 0x20;
    public const byte WriteProtectFlag = 0x40;
    public const byte NotReadyFlag = 0x80;

    private const byte IdAddressMark = 0xFE;
    private const byte DataAddressMark = 0xFB;
    private const byte DeletedDataAddressMark = 0xF8;
    private const byte SyncMarkByte = 0xA1;

    // FM (single-density, real TRS-80 Model I) track layout - IBM 3740 format. Gap lengths are
    // representative standard values (real drives tolerate a range), not a byte-exact replica of
    // any specific physical disk.
    private const int FmGap1Length = 40;
    private const int FmIdSyncLength = 6;
    private const int FmGap2Length = 11;
    private const int FmDataSyncLength = 6;
    private const int FmGap3Length = 27;
    private const byte FmFillByte = 0xFF;

    // MFM (double-density) track layout - IBM System 34 format, same caveat as above.
    private const int MfmGap1Length = 32;
    private const int MfmIdSyncLength = 12;
    private const int MfmSyncMarkLength = 3;
    private const int MfmGap2Length = 22;
    private const int MfmDataSyncLength = 12;
    private const int MfmGap3Length = 54;
    private const byte MfmFillByte = 0x4E;

    public const int DriveCount = 4;
    public const int DefaultDataByteTStates = 56;
    public const int DefaultSeekTStates = 12_000;
    public const int DefaultSectorTStates = 18_000;
    public const long DefaultIndexPeriodTStates = 354_800;
    public const long DefaultIndexPulseWidthTStates = 35_480;
    private readonly IFD1791DiskImage?[] _drives = new IFD1791DiskImage?[DriveCount];
    private readonly int _dataByteTStates;
    private readonly int _seekTStates;
    private readonly int _sectorTStates;
    private readonly long _indexPeriodTStates;
    private readonly long _indexPulseWidthTStates;
    private byte _driveSelect;
    private bool _driveSelectWritten;
    private byte _track;
    private byte _sector;
    private byte _data;
    private byte _status;
    private byte _pendingCommand;
    private int _pendingTStates;
    private int _readSearchTStates;
    private byte[]? _transfer;
    private int _transferIndex;
    private bool _writeTransfer;
    private bool _dataRequestPending;
    private byte _recordType;
    private int _dataDeadlineTStates;
    private int _addressMarkIndex;
    private long _tStateCounter;
    private bool _typeICommand = true;
    private int _lastStepDirection = 1;

    public FD1791(
        IFD1791DiskImage? disk = null,
        int dataByteTStates = DefaultDataByteTStates,
        int seekTStates = DefaultSeekTStates,
        int sectorTStates = DefaultSectorTStates,
        long indexPeriodTStates = DefaultIndexPeriodTStates,
        long indexPulseWidthTStates = DefaultIndexPulseWidthTStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dataByteTStates);
        ArgumentOutOfRangeException.ThrowIfNegative(seekTStates);
        ArgumentOutOfRangeException.ThrowIfNegative(sectorTStates);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexPeriodTStates);
        ArgumentOutOfRangeException.ThrowIfNegative(indexPulseWidthTStates);
        if (indexPulseWidthTStates > indexPeriodTStates)
            throw new ArgumentOutOfRangeException(nameof(indexPulseWidthTStates));

        _dataByteTStates = dataByteTStates;
        _seekTStates = seekTStates;
        _sectorTStates = sectorTStates;
        _indexPeriodTStates = indexPeriodTStates;
        _indexPulseWidthTStates = indexPulseWidthTStates;
        _drives[0] = disk;
        DoubleDensityEnabled = disk?.IsDoubleDensity ?? false;
    }

    public bool IntrqAsserted { get; private set; }
    public bool Busy => (_status & BusyFlag) != 0;
    public bool DrqAsserted => _dataRequestPending;
    public bool DoubleDensityEnabled { get; set; }
    public ulong InterruptSequence { get; private set; }
    public Action<FD1791DiagnosticEvent>? DiagnosticObserver { get; set; }

    /// <summary>Raised for board-visible activity; this is intentionally separate from diagnostics.</summary>
    public event EventHandler<FD1791ActivityEventArgs>? ActivityChanged;

    /// <summary>Enables automatic continuation of READ/WRITE multiple-record commands.</summary>
    public bool MultipleRecordEnabled { get; set; } = true;

    /// <summary>Physical DAL polarity for this controller variant.</summary>
    public virtual Fd179xDataBusMode DataBusMode => Fd179xDataBusMode.Inverted;

    public byte EncodeDataBus(byte logicalValue) =>
        DataBusMode == Fd179xDataBusMode.Inverted ? (byte)~logicalValue : logicalValue;

    public byte DecodeDataBus(byte busValue) =>
        DataBusMode == Fd179xDataBusMode.Inverted ? (byte)~busValue : busValue;

    public FD1791Snapshot CaptureState() => new()
    {
        DriveSelect = _driveSelect, DriveSelectWritten = _driveSelectWritten, Track = _track,
        Sector = _sector, Data = _data, Status = _status, PendingCommand = _pendingCommand,
        PendingTStates = _pendingTStates, ReadSearchTStates = _readSearchTStates,
        Transfer = _transfer?.ToArray(), TransferIndex = _transferIndex, WriteTransfer = _writeTransfer,
        DataRequestPending = _dataRequestPending, RecordType = _recordType,
        DataDeadlineTStates = _dataDeadlineTStates, AddressMarkIndex = _addressMarkIndex,
        TStateCounter = _tStateCounter, TypeICommand = _typeICommand,
        LastStepDirection = _lastStepDirection, IntrqAsserted = IntrqAsserted,
        DoubleDensityEnabled = DoubleDensityEnabled, InterruptSequence = InterruptSequence,
        MultipleRecordEnabled = MultipleRecordEnabled, Side = Side
    };

    public void RestoreState(FD1791Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.TransferIndex < 0 || (state.Transfer is not null && state.TransferIndex > state.Transfer.Length))
            throw new ArgumentException("Invalid FD1791 transfer position.", nameof(state));
        _driveSelect = state.DriveSelect;
        _driveSelectWritten = state.DriveSelectWritten;
        _track = state.Track; _sector = state.Sector; _data = state.Data; _status = state.Status;
        _pendingCommand = state.PendingCommand; _pendingTStates = state.PendingTStates;
        _readSearchTStates = state.ReadSearchTStates; _transfer = state.Transfer?.ToArray();
        _transferIndex = state.TransferIndex; _writeTransfer = state.WriteTransfer;
        _dataRequestPending = state.DataRequestPending; _recordType = state.RecordType;
        _dataDeadlineTStates = state.DataDeadlineTStates; _addressMarkIndex = state.AddressMarkIndex;
        _tStateCounter = state.TStateCounter; _typeICommand = state.TypeICommand;
        _lastStepDirection = state.LastStepDirection; IntrqAsserted = state.IntrqAsserted;
        DoubleDensityEnabled = state.DoubleDensityEnabled; InterruptSequence = state.InterruptSequence;
        MultipleRecordEnabled = state.MultipleRecordEnabled; Side = state.Side;
    }
    public byte Track { get => _track; set => _track = value; }
    public byte Sector { get => _sector; set => _sector = value; }
    public byte Data => _data;
    public byte Status => _status;
    public byte PendingCommand => _pendingCommand;
    public int PendingTStatesRemaining => _pendingTStates;
    public int DataDeadlineTStatesRemaining => _dataDeadlineTStates;
    public byte Side { get; set; }

    /// <summary>Current drive-select latch after masking to the four drive bits.</summary>
    public byte DriveSelect
    {
        get => _driveSelect;
        set
        {
            _driveSelectWritten = true;
            value &= 0x0F;
            _driveSelect = value == 0 ? (byte)0 : (byte)(value & (byte)-value);
        }
    }

    /// <summary>Restores controller state while keeping inserted media.</summary>
    public void Reset()
    {
        _driveSelect = 0;
        _driveSelectWritten = false;
        _track = 0;
        _sector = 0;
        _data = 0;
        _status = 0;
        _pendingCommand = 0;
        _pendingTStates = 0;
        _readSearchTStates = 0;
        _transfer = null;
        _transferIndex = 0;
        _writeTransfer = false;
        _recordType = 0;
        _dataDeadlineTStates = 0;
        _addressMarkIndex = 0;
        _tStateCounter = 0;
        _typeICommand = true;
        _lastStepDirection = 1;
        IntrqAsserted = false;
        InterruptSequence = 0;
        ClearDataRequest();
        Emit(FD1791DiagnosticKind.Reset);
        RaiseActivity(FD1791ActivityKind.Reset);
    }

    public void InsertDisk(int drive, IFD1791DiskImage? disk)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(drive);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(drive, DriveCount);
        _drives[drive] = disk;
        if (drive == 0 && disk is not null)
            DoubleDensityEnabled = disk.IsDoubleDensity;
        Emit(FD1791DiagnosticKind.DiskInserted, value: (byte)drive);
    }

    /// <summary>Advances command, byte-transfer, index-pulse and WAIT timing.</summary>
    public void Tick(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);
        _tStateCounter += tStates;

        if (_pendingTStates > 0)
        {
            _pendingTStates -= tStates;
            if (_pendingTStates <= 0)
            {
                _pendingTStates = 0;
                ExecutePendingCommand();
            }
        }
        else if (_readSearchTStates > 0)
        {
            _readSearchTStates -= tStates;
            if (_readSearchTStates <= 0)
            {
                _readSearchTStates = 0;
                Complete(RecordNotFoundFlag);
            }
        }
        else if (_dataRequestPending && _dataDeadlineTStates > 0)
        {
            _dataDeadlineTStates -= tStates;
            if (_dataDeadlineTStates <= 0)
            {
                Emit(FD1791DiagnosticKind.LostData);
                Complete(LostDataFlag);
            }
        }
    }

    public byte Read(byte register)
    {
        var value = register switch
        {
            CommandStatusRegister => ReadStatus(),
            TrackRegister => _track,
            SectorRegister => _sector,
            DataRegister => ReadData(),
            _ => (byte)0xFF,
        };
        Emit(FD1791DiagnosticKind.RegisterRead, register, value);
        return value;
    }

    public void Write(byte register, byte value)
    {
        Emit(FD1791DiagnosticKind.RegisterWrite, register, value);
        switch (register)
        {
            case CommandStatusRegister:
                WriteCommand(value);
                break;
            case TrackRegister:
                _track = value;
                break;
            case SectorRegister:
                _sector = value;
                break;
            case DataRegister:
                WriteData(value);
                break;
        }
    }

    public byte ReadStatus()
    {
        IntrqAsserted = false;
        var disk = SelectedDisk;
        if (disk is null)
            return (byte)(_status | NotReadyFlag);
        return _typeICommand ? (byte)(_status | IndexPulseFlag()) : _status;
    }

    public byte ReadData()
    {
        if (_writeTransfer || _transfer is null || !_dataRequestPending)
            return _data;

        ClearDataRequest();
        _data = _transfer[_transferIndex++];
        Emit(FD1791DiagnosticKind.DataRead, value: _data);
        RaiseActivity(FD1791ActivityKind.DataTransferred);
        if (_transferIndex != _transfer.Length)
        {
            RequestData();
            return _data;
        }

        if (MultipleRecordEnabled && (_pendingCommand & 0x10) != 0 && SelectedDisk is { } disk)
        {
            var nextSector = (byte)(_sector + 1);
            if (TryReadSector(disk, _track, Side, nextSector, _transfer))
            {
                _sector = nextSector;
                _recordType = disk.IsDeletedDataMark(_track, _sector) ? RecordTypeFlag : (byte)0;
                _transferIndex = 0;
                RequestData();
                return _data;
            }

            _status = BusyFlag;
            _readSearchTStates = _sectorTStates;
            return _data;
        }

        Complete(_recordType);
        return _data;
    }

    public void WriteCommand(byte command)
    {
        if ((command & 0xF0) == 0xD0)
        {
            _pendingTStates = 0;
            _readSearchTStates = 0;
            _typeICommand = true;
            Complete(0);
            return;
        }

        if (Busy)
            return;

        IntrqAsserted = false;
        _pendingCommand = command;
        _status = BusyFlag;
        _typeICommand = (command & 0x80) == 0;
        Emit(FD1791DiagnosticKind.CommandAccepted, value: command);
        RaiseActivity(FD1791ActivityKind.CommandStarted);
        var sectorCommand = (command & 0xC0) == 0x80;
        _pendingTStates = sectorCommand ? _sectorTStates : _seekTStates;
        if (_pendingTStates == 0)
            ExecutePendingCommand();
    }

    public void WriteData(byte value)
    {
        _data = value;
        if (!_writeTransfer || _transfer is null || !_dataRequestPending)
            return;

        ClearDataRequest();
        _transfer[_transferIndex++] = value;
        Emit(FD1791DiagnosticKind.DataWritten, value: value);
        RaiseActivity(FD1791ActivityKind.DataTransferred);
        if (_transferIndex != _transfer.Length)
        {
            RequestData();
            return;
        }

        var disk = SelectedDisk;
        if (disk is null)
        {
            Complete(NotReadyFlag);
            return;
        }

        if (disk.WriteProtected)
        {
            Complete(WriteProtectFlag);
            return;
        }

        if ((_pendingCommand & 0xF0) == 0xF0)
        {
            ParseAndWriteTrack(_transfer, disk, _track, Side);
            Complete(0);
            return;
        }

        if (!TryWriteSector(disk, _track, Side, _sector, _transfer))
        {
            Complete(RecordNotFoundFlag);
            return;
        }

        if (MultipleRecordEnabled && (_pendingCommand & 0x10) != 0)
        {
            _sector++;
            _transferIndex = 0;
            RequestData();
            return;
        }

        Complete(0);
    }

    private IFD1791DiskImage? SelectedDisk
    {
        get
        {
            if (_driveSelect == 0)
                return _driveSelectWritten ? null : _drives[0];

            for (var drive = 0; drive < DriveCount; drive++)
                if ((_driveSelect & (1 << drive)) != 0)
                    return _drives[drive];
            return null;
        }
    }

    private void Step(int direction)
    {
        if (direction > 0)
            _track++;
        else if (_track != 0)
            _track--;
    }

    private void ExecutePendingCommand()
    {
        var disk = SelectedDisk;
        switch (_pendingCommand & 0xF0)
        {
            case 0x00:
                _track = 0;
                _addressMarkIndex = 0;
                Complete(disk is null ? NotReadyFlag : TrackZeroFlag);
                return;
            case 0x10:
                _track = _data;
                _addressMarkIndex = 0;
                Complete(disk is null ? NotReadyFlag : _track == 0 ? TrackZeroFlag : (byte)0);
                return;
            case 0x20:
                Step(_lastStepDirection);
                _addressMarkIndex = 0;
                Complete(disk is null ? NotReadyFlag : (byte)0);
                return;
            case 0x30:
                _lastStepDirection = 1;
                Step(_lastStepDirection);
                _addressMarkIndex = 0;
                Complete(disk is null ? NotReadyFlag : (byte)0);
                return;
            case 0x40 or 0x50:
                _lastStepDirection = -1;
                Step(_lastStepDirection);
                _addressMarkIndex = 0;
                Complete(disk is null ? NotReadyFlag : _track == 0 ? TrackZeroFlag : (byte)0);
                return;
        }

        if (disk is null)
        {
            Complete(NotReadyFlag);
            return;
        }

        if (disk.IsDoubleDensity != DoubleDensityEnabled)
        {
            Complete(RecordNotFoundFlag);
            return;
        }

        if ((_pendingCommand & 0xF0) == 0xC0)
        {
            var lengthCode = disk.SectorSize switch { 128 => 0, 256 => 1, 512 => 2, 1024 => 3, _ => 0 };
            var sectors = disk.SectorsOnTrack(_track);
            _sector = (byte)(disk.FirstSectorId + (sectors > 0 ? _addressMarkIndex % sectors : 0));
            _addressMarkIndex++;
            var idField = new byte[] { IdAddressMark, _track, Side, _sector, (byte)lengthCode };
            var crc = ComputeCrc16(disk.IsDoubleDensity ? PrependSyncMarks(idField) : idField);
            _transfer = [_track, Side, _sector, (byte)lengthCode, (byte)(crc >> 8), (byte)crc];
            _transferIndex = 0;
            _writeTransfer = false;
            RequestData();
            return;
        }

        if ((_pendingCommand & 0xF0) == 0xE0)
        {
            _transfer = BuildTrack(disk, _track, Side);
            _transferIndex = 0;
            _writeTransfer = false;
            RequestData();
            return;
        }

        if ((_pendingCommand & 0xF0) == 0xF0)
        {
            if (disk.WriteProtected)
            {
                Complete(WriteProtectFlag);
                return;
            }

            _transfer = new byte[ComputeTrackLength(disk, _track)];
            _transferIndex = 0;
            _writeTransfer = true;
            RequestData();
            return;
        }

        _writeTransfer = (_pendingCommand & 0xF0) == 0xA0;
        _transfer = new byte[disk.SectorSize];
        _transferIndex = 0;
        if (_writeTransfer)
        {
            if (disk.WriteProtected)
            {
                Complete(WriteProtectFlag);
                return;
            }

            RequestData();
            return;
        }

        if (!TryReadSector(disk, _track, Side, _sector, _transfer))
        {
            Complete(RecordNotFoundFlag);
            return;
        }

        _recordType = disk.IsDeletedDataMark(_track, _sector) ? RecordTypeFlag : (byte)0;
        RequestData();
    }

    /// <summary>Builds one full FM or MFM track (gaps, sync, address marks, real CRC-16) from the
    /// logical sector data <paramref name="disk"/> exposes - used by READ TRACK (0xE0). There's no
    /// raw-track byte source in <see cref="IFD1791DiskImage"/> (JV1 has none at all; DMK's are
    /// private to <c>DmkDiskImage</c>), so this reconstructs a spec-correct, self-consistent track
    /// rather than replaying the original physical layout byte-for-byte - any correct WD179x-driven
    /// software parses it the same way either way.</summary>
    private static byte[] BuildTrack(IFD1791DiskImage disk, int track, int side)
    {
        var mfm = disk.IsDoubleDensity;
        var sectors = disk.SectorsOnTrack(track);
        var firstSector = disk.FirstSectorId;
        var lengthCode = disk.SectorSize switch { 128 => 0, 256 => 1, 512 => 2, 1024 => 3, _ => 0 };
        var fill = mfm ? MfmFillByte : FmFillByte;
        var buffer = new List<byte>(ComputeTrackLength(disk, track));

        buffer.AddRange(Enumerable.Repeat(fill, mfm ? MfmGap1Length : FmGap1Length));

        var sectorData = new byte[disk.SectorSize];
        for (var i = 0; i < sectors; i++)
        {
            var sector = (byte)(firstSector + i);

            buffer.AddRange(Enumerable.Repeat((byte)0x00, mfm ? MfmIdSyncLength : FmIdSyncLength));
            if (mfm)
                buffer.AddRange(Enumerable.Repeat(SyncMarkByte, MfmSyncMarkLength));
            var idField = new byte[] { IdAddressMark, (byte)track, (byte)side, sector, (byte)lengthCode };
            buffer.AddRange(idField);
            var idCrc = ComputeCrc16(mfm ? PrependSyncMarks(idField) : idField);
            buffer.Add((byte)(idCrc >> 8));
            buffer.Add((byte)idCrc);

            buffer.AddRange(Enumerable.Repeat(fill, mfm ? MfmGap2Length : FmGap2Length));
            buffer.AddRange(Enumerable.Repeat((byte)0x00, mfm ? MfmDataSyncLength : FmDataSyncLength));
            if (mfm)
                buffer.AddRange(Enumerable.Repeat(SyncMarkByte, MfmSyncMarkLength));

            // A sector this image doesn't actually have (short/damaged track) reads as zeros rather
            // than failing the whole track - READ TRACK has no per-sector error status to report.
            var deleted = TryReadSector(disk, track, side, sector, sectorData) && disk.IsDeletedDataMark(track, sector);
            var mark = deleted ? DeletedDataAddressMark : DataAddressMark;
            buffer.Add(mark);
            buffer.AddRange(sectorData);
            var dataField = new byte[sectorData.Length + 1];
            dataField[0] = mark;
            sectorData.CopyTo(dataField, 1);
            var dataCrc = ComputeCrc16(mfm ? PrependSyncMarks(dataField) : dataField);
            buffer.Add((byte)(dataCrc >> 8));
            buffer.Add((byte)dataCrc);

            buffer.AddRange(Enumerable.Repeat(fill, mfm ? MfmGap3Length : FmGap3Length));
        }

        return buffer.ToArray();
    }

    /// <summary>Exact byte length <see cref="BuildTrack"/> produces for this disk/track, computed
    /// without reading any sector data - used to size the WRITE TRACK (0xF0) transfer buffer up
    /// front, since the real command's data-request loop needs a fixed byte count before any data
    /// arrives.</summary>
    private static int ComputeTrackLength(IFD1791DiskImage disk, int track)
    {
        var mfm = disk.IsDoubleDensity;
        var sectors = disk.SectorsOnTrack(track);
        var idAndData = 5 + 2 + 1 + disk.SectorSize + 2; // idField(5) + idCrc(2) + mark(1) + data + dataCrc(2)
        var perSector = mfm
            ? MfmIdSyncLength + MfmSyncMarkLength + MfmGap2Length + MfmDataSyncLength + MfmSyncMarkLength + MfmGap3Length + idAndData
            : FmIdSyncLength + FmGap2Length + FmDataSyncLength + FmGap3Length + idAndData;
        return (mfm ? MfmGap1Length : FmGap1Length) + sectors * perSector;
    }

    /// <summary>Scans a raw track received via WRITE TRACK (0xF0) for ID+data field pairs and
    /// writes each recognized sector back through the normal sector-write path - a logical-image
    /// stand-in for what a real drive head does continuously as it passes over newly-written flux
    /// transitions. ID-field and data-field CRC bytes are consumed but not validated: on a real
    /// format pass, whatever the CPU just wrote is definitionally correct, there's nothing to check
    /// it against yet.</summary>
    private static void ParseAndWriteTrack(ReadOnlySpan<byte> raw, IFD1791DiskImage disk, int track, int side)
    {
        var sectorSize = disk.SectorSize;
        byte? pendingSector = null;
        var i = 0;
        while (i < raw.Length)
        {
            if (raw[i] == IdAddressMark && i + 5 <= raw.Length)
            {
                pendingSector = raw[i + 3];
                i += 5 + 2; // idField + its CRC bytes (unchecked, see doc comment)
                continue;
            }

            if (pendingSector is { } sector
                && raw[i] is DataAddressMark or DeletedDataAddressMark
                && i + 1 + sectorSize + 2 <= raw.Length)
            {
                TryWriteSector(disk, track, side, sector, raw.Slice(i + 1, sectorSize));
                pendingSector = null;
                i += 1 + sectorSize + 2; // mark + data + CRC bytes (unchecked, see doc comment)
                continue;
            }

            i++;
        }
    }

    private static byte[] PrependSyncMarks(byte[] field)
    {
        var result = new byte[MfmSyncMarkLength + field.Length];
        Array.Fill(result, SyncMarkByte, 0, MfmSyncMarkLength);
        field.CopyTo(result, MfmSyncMarkLength);
        return result;
    }

    /// <summary>CRC-16-CCITT (poly 0x1021, init 0xFFFF) - the real WD179x/FD179x family's ID- and
    /// data-field check, computed over the address mark plus field bytes (MFM also includes the
    /// three 0xA1 sync-mark bytes ahead of the mark - see <see cref="PrependSyncMarks"/>).</summary>
    private static ushort ComputeCrc16(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= b << 8;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1;
            crc &= 0xFFFF;
        }

        return (ushort)crc;
    }

    private static bool TryReadSector(IFD1791DiskImage disk, int track, int side, int sector, Span<byte> destination) =>
        disk is IFD1791SidedDiskImage sided ? sided.TryReadSector(track, side, sector, destination) : side == 0 && disk.TryReadSector(track, sector, destination);

    private static bool TryWriteSector(IFD1791DiskImage disk, int track, int side, int sector, ReadOnlySpan<byte> source) =>
        disk is IFD1791SidedDiskImage sided ? sided.TryWriteSector(track, side, sector, source) : side == 0 && disk.TryWriteSector(track, sector, source);

    private byte IndexPulseFlag() => _tStateCounter % _indexPeriodTStates < _indexPulseWidthTStates ? DataRequestFlag : (byte)0;

    private void RequestData()
    {
        _status = (byte)(BusyFlag | DataRequestFlag | _recordType);
        _dataRequestPending = true;
        _dataDeadlineTStates = _dataByteTStates;
        InterruptSequence++;
        Emit(FD1791DiagnosticKind.DataRequested);
        RaiseActivity(FD1791ActivityKind.DataRequested);
    }

    private void ClearDataRequest()
    {
        var wasPending = _dataRequestPending;
        _status &= unchecked((byte)~DataRequestFlag);
        _dataRequestPending = false;
        _dataDeadlineTStates = 0;
        if (wasPending)
            Emit(FD1791DiagnosticKind.DataRequestCleared);
    }

    private void Complete(byte error)
    {
        _pendingTStates = 0;
        _readSearchTStates = 0;
        _status = error;
        _recordType = 0;
        _transfer = null;
        _transferIndex = 0;
        _writeTransfer = false;
        ClearDataRequest();
        IntrqAsserted = true;
        InterruptSequence++;
        Emit(FD1791DiagnosticKind.CommandCompleted, value: error);
        RaiseActivity(error == 0 ? FD1791ActivityKind.CommandCompleted : FD1791ActivityKind.Error);
    }

    private void RaiseActivity(FD1791ActivityKind kind)
    {
        ActivityChanged?.Invoke(this, new FD1791ActivityEventArgs(
            kind,
            _tStateCounter,
            _pendingCommand,
            _status,
            _track,
            _sector,
            _transferIndex,
            Busy,
            DrqAsserted,
            IntrqAsserted));
    }

    private void Emit(FD1791DiagnosticKind kind, byte register = 0xFF, byte value = 0)
    {
        DiagnosticObserver?.Invoke(new FD1791DiagnosticEvent(
            kind,
            _tStateCounter,
            register,
            value,
            _pendingCommand,
            _status,
            _track,
            _sector,
            _data,
            _transferIndex,
            _dataDeadlineTStates,
            _dataRequestPending,
            IntrqAsserted));
    }
}

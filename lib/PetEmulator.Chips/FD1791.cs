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

    /// <summary>Enables automatic continuation of READ/WRITE multiple-record commands.</summary>
    public bool MultipleRecordEnabled { get; set; } = true;

    /// <summary>Physical DAL polarity for this controller variant.</summary>
    public virtual Fd179xDataBusMode DataBusMode => Fd179xDataBusMode.Inverted;

    public byte EncodeDataBus(byte logicalValue) =>
        DataBusMode == Fd179xDataBusMode.Inverted ? (byte)~logicalValue : logicalValue;

    public byte DecodeDataBus(byte busValue) =>
        DataBusMode == Fd179xDataBusMode.Inverted ? (byte)~busValue : busValue;
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
            _transfer = [_track, Side, _sector, (byte)lengthCode, 0, 0];
            _transferIndex = 0;
            _writeTransfer = false;
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

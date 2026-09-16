namespace PetEmulator.CpcFdc;

/// <summary>
/// Intel 8272 Floppy Disk Controller — command/result phase FSM with support for read, write,
/// seek, recalibrate, read-ID, and format operations. Three ports: +0 (unused), +1 (MSR, read-only,
/// status flags), +2 (Data, read/write for command/result/execution bytes). Initialize via
/// <see cref="LoadImage"/> to set up a disk image (tracks, heads, sectors, sector size), then
/// send commands (e.g. Seek 0x0F, Read 0x46, Write 0x45) and provide/consume data during
/// execution phase. Queries via <see cref="CurrentPhase"/> and <see cref="Cylinder"/>.
///
/// Ported from proc-vibe-001's <c>ProcVibe.Core.Peripherals.I8272</c> — the only I8272
/// implementation in the peripheral-chip inventory, no alternative to review against. 13 tests.
///
/// Port-mapped like every other Intel 8085/8086-era chip ported into this repo (see
/// <c>Tms9918aChip</c>'s class doc for why): implements <see cref="IPortMappedDevice{TPort,TTransfer}"/>
/// with an 8-bit port instead of <see cref="IMemoryMappedDevice{TAddress,TTransfer}"/>.
/// Port base is 0xF0 (classic real-hardware FDC base 0x3F0 scaled down to byte range).
/// The donor has no <c>Reset()</c> at all; one is added here to satisfy <see cref="IDevice"/>.
/// Note: DiskData, Tracks, Heads, SectorsPerTrack, SectorSize are NOT cleared in Reset() — they
/// represent the loaded disk image and wiring configuration set by <see cref="LoadImage"/>, not
/// internal chip register state (same reasoning as Vic6569Chip not clearing Vram, which is real
/// chip state that a reset should preserve).
/// </summary>
public sealed class I8272Chip
{
    /// <summary>At CPC's 4 MHz CPU clock, one byte at the 250 kbit/s MFM data rate occupies 128 cycles.</summary>
    public const int CpcCyclesPerDataByte = 128;
    public enum Phase { Idle, Command, Execution, Result }

    public Phase CurrentPhase = Phase.Idle;
    public byte[] CommandBuffer = new byte[9];
    public byte[] ResultBuffer = new byte[7];
    public int BufferIndex;
    public int ResultCount;
    public byte Data;
    public ushort Cylinder;
    public int TransferBytesRemaining;
    public byte StepRateHeadUnload { get; private set; }
    public byte HeadLoadNonDma { get; private set; }

    public int Tracks;
    public int Heads;
    public int SectorsPerTrack;
    public int SectorSize;
    public byte[][][]? DiskData;
    public IFloppyDrive? Drive0 { get; set; }
    public IFloppyDrive? Drive1 { get; set; }
    public bool InterruptPending { get; private set; }
    private byte[] _transferBuffer = [];
    private bool _usingDrive;
    private bool _transferError;
    private int _formatIdIndex;
    private byte _interruptStatus = 0x80;
    private int _executionCyclesUntilOverrun;

    public I8272Chip(byte startPort = 0xF0)
    {
        StartPort = startPort;
        EndPort = (byte)(startPort + 2);
    }

    public byte StartPort { get; }
    public byte EndPort { get; }

    public I8272Snapshot CaptureState() => new()
    {
        Phase = CurrentPhase,
        CommandBuffer = CommandBuffer.ToArray(),
        ResultBuffer = ResultBuffer.ToArray(),
        BufferIndex = BufferIndex,
        ResultCount = ResultCount,
        Data = Data,
        Cylinder = Cylinder,
        TransferBytesRemaining = TransferBytesRemaining,
        StepRateHeadUnload = StepRateHeadUnload,
        HeadLoadNonDma = HeadLoadNonDma,
        Tracks = Tracks,
        Heads = Heads,
        SectorsPerTrack = SectorsPerTrack,
        SectorSize = SectorSize,
        DiskData = CloneDiskData(DiskData),
        InterruptPending = InterruptPending,
        TransferBuffer = _transferBuffer.ToArray(),
        UsingDrive = _usingDrive,
        TransferError = _transferError,
        FormatIdIndex = _formatIdIndex,
        InterruptStatus = _interruptStatus,
        ExecutionCyclesUntilOverrun = _executionCyclesUntilOverrun,
        Drive0 = (Drive0 as DskFloppyDrive)?.CaptureState(),
        Drive1 = (Drive1 as DskFloppyDrive)?.CaptureState(),
    };

    public void RestoreState(I8272Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Version != I8272Snapshot.CurrentVersion)
            throw new InvalidDataException($"Unsupported I8272 snapshot version {snapshot.Version}.");
        CurrentPhase = snapshot.Phase;
        CommandBuffer = snapshot.CommandBuffer.ToArray();
        ResultBuffer = snapshot.ResultBuffer.ToArray();
        BufferIndex = snapshot.BufferIndex;
        ResultCount = snapshot.ResultCount;
        Data = snapshot.Data;
        Cylinder = snapshot.Cylinder;
        TransferBytesRemaining = snapshot.TransferBytesRemaining;
        StepRateHeadUnload = snapshot.StepRateHeadUnload;
        HeadLoadNonDma = snapshot.HeadLoadNonDma;
        Tracks = snapshot.Tracks;
        Heads = snapshot.Heads;
        SectorsPerTrack = snapshot.SectorsPerTrack;
        SectorSize = snapshot.SectorSize;
        DiskData = CloneDiskData(snapshot.DiskData);
        InterruptPending = snapshot.InterruptPending;
        _transferBuffer = snapshot.TransferBuffer.ToArray();
        _usingDrive = snapshot.UsingDrive;
        _transferError = snapshot.TransferError;
        _formatIdIndex = snapshot.FormatIdIndex;
        _interruptStatus = snapshot.InterruptStatus;
        _executionCyclesUntilOverrun = snapshot.ExecutionCyclesUntilOverrun;
        Drive0 = snapshot.Drive0 is null ? null : DskFloppyDrive.RestoreState(snapshot.Drive0);
        Drive1 = snapshot.Drive1 is null ? null : DskFloppyDrive.RestoreState(snapshot.Drive1);
    }

    public void Reset()
    {
        CurrentPhase = Phase.Idle;
        BufferIndex = 0;
        ResultCount = 0;
        Data = 0;
        Cylinder = 0;
        TransferBytesRemaining = 0;
        StepRateHeadUnload = 0;
        HeadLoadNonDma = 0;
        InterruptPending = false;
        _transferBuffer = [];
        _usingDrive = false;
        _transferError = false;
        _formatIdIndex = 0;
        _interruptStatus = 0x80;
        _executionCyclesUntilOverrun = 0;
        // Do NOT clear CommandBuffer, ResultBuffer (harmless stale scratch space)
        // Do NOT clear DiskData, Tracks, Heads, SectorsPerTrack, SectorSize (loaded disk image)
    }

    public byte ReadPort(byte port)
    {
        var offset = (byte)(port - StartPort);
        return offset switch
        {
            1 => Msr(),
            2 => DataRead(),
            _ => 0
        };
    }

    public byte ReadMainStatus() => Msr();
    public byte ReadDataRegister() => DataRead();
    public void WriteDataRegister(byte value) => DataWrite(value);

    /// <summary>Advances the byte service deadline during an execution transfer.</summary>
    public void Tick(int cpuCycles)
    {
        if (cpuCycles <= 0 || CurrentPhase != Phase.Execution || _executionCyclesUntilOverrun <= 0) return;
        _executionCyclesUntilOverrun -= cpuCycles;
        if (_executionCyclesUntilOverrun > 0) return;
        BuildErrorResult(0x10); // ST1.OR: CPU/DMA did not service the byte in time.
    }

    public void WritePort(byte port, byte value)
    {
        var offset = (byte)(port - StartPort);
        switch (offset)
        {
            case 2:
                DataWrite(value);
                break;
        }
    }

    public void LoadImage(byte[] raw, int tracks, int heads, int sectorsPerTrack, int sectorSize)
    {
        Tracks = tracks;
        Heads = heads;
        SectorsPerTrack = sectorsPerTrack;
        SectorSize = sectorSize;
        DiskData = new byte[tracks][][];

        var offset = 0;
        var sectorLen = sectorsPerTrack * sectorSize;
        for (var t = 0; t < tracks; t++)
        {
            DiskData[t] = new byte[heads][];
            for (var h = 0; h < heads; h++)
            {
                DiskData[t][h] = new byte[sectorLen];
                for (var s = 0; s < sectorLen; s++)
                    DiskData[t][h][s] = offset < raw.Length ? raw[offset++] : (byte)0;
            }
        }
    }

    private byte Msr()
    {
        if (CurrentPhase == Phase.Idle) return 0x80;
        byte msr = 0x90;
        if (CurrentPhase == Phase.Execution)
        {
            msr |= 0x20;
            if ((CommandBuffer[0] & 0x1F) is 0x06 or 0x0C) msr |= 0x40;
        }
        else if (CurrentPhase == Phase.Result) msr |= 0x40;
        return msr;
    }

    private byte DataRead()
    {
        if (CurrentPhase == Phase.Execution)
        {
            var cmd = CommandBuffer[0] & 0x1F;
            if (cmd is 0x06 or 0x0C)
            {
                var value = ReadSectorByte();
                ArmExecutionDeadline();
                if (TransferBytesRemaining <= 0)
                {
                    if (!AdvanceDataTransfer(false) && CurrentPhase == Phase.Execution)
                    {
                        BuildReadResult();
                        CurrentPhase = Phase.Result;
                    }
                }
                return value;
            }
        }
        if (CurrentPhase == Phase.Result)
        {
            if (BufferIndex >= ResultCount)
            {
                CurrentPhase = Phase.Idle;
                return 0;
            }
            var v = ResultBuffer[BufferIndex++];
            if (BufferIndex >= ResultCount)
                CurrentPhase = Phase.Idle;
            return v;
        }
        return 0;
    }

    private void DataWrite(byte value)
    {
        if (CurrentPhase == Phase.Result) return;
        if (CurrentPhase == Phase.Idle)
        {
            BufferIndex = 0;
            CommandBuffer[0] = value;
            BufferIndex = 1;
            CurrentPhase = Phase.Command;
        }
        else if (CurrentPhase == Phase.Command)
        {
            if (BufferIndex >= CommandBuffer.Length) { InvalidCommand(); return; }
            CommandBuffer[BufferIndex++] = value;
        }

        if (CurrentPhase == Phase.Command)
        {
            var cmd = CommandBuffer[0];
            var needBytes = CommandLength(cmd);
            if (needBytes == 0) { InvalidCommand(); return; }
            if (BufferIndex >= needBytes)
                ExecuteCommand();
        }
        else if (CurrentPhase == Phase.Execution)
        {
            HandleExecutionByte(value);
        }
    }

    private void HandleExecutionByte(byte value)
    {
        ArmExecutionDeadline();
        var cmd = CommandBuffer[0] & 0x1F;
        if (cmd == 0x05)
        {
            WriteSectorByte(value);
            if (TransferBytesRemaining <= 0)
            {
                if (!AdvanceDataTransfer(true) && CurrentPhase == Phase.Execution)
                {
                    BuildWriteResult();
                    CurrentPhase = Phase.Result;
                }
            }
        }
        else if (cmd is 0x46 or 0x66)
        {
        }
        else if (cmd == 0x0D)
        {
            _transferBuffer[_formatIdIndex++] = value;
            if (_formatIdIndex >= _transferBuffer.Length)
            {
                byte head = (byte)((CommandBuffer[1] >> 2) & 1);
                bool ok = SelectedDrive()?.FormatTrack(head, CommandBuffer[2], CommandBuffer[3], _transferBuffer, CommandBuffer[5]) ?? false;
                if (ok) BuildFormatResult(head);
                else BuildErrorResult(0x04);
                CurrentPhase = Phase.Result;
                TransferBytesRemaining = 0;
            }
        }
    }

    private byte ReadSectorByte()
    {
        var track = CommandBuffer[2];
        var head = CommandBuffer[3];
        var sector = CommandBuffer[4];
        var byteInSector = SectorTransferSize() - TransferBytesRemaining;
        int sectorSize = SectorTransferSize();
        if (sector == 0 || sectorSize <= 0) { TransferBytesRemaining = 0; return 0; }
        var offset = (sector - 1) * sectorSize + byteInSector;
        TransferBytesRemaining--;
        if (_usingDrive) return _transferBuffer[byteInSector];
        if (DiskData != null && track < Tracks && head < Heads && offset < DiskData[track][head].Length)
            return DiskData[track][head][offset];
        return 0;
    }

    private void WriteSectorByte(byte value)
    {
        var track = CommandBuffer[2];
        var head = CommandBuffer[3];
        var sector = CommandBuffer[4];
        var byteInSector = SectorTransferSize() - TransferBytesRemaining;
        int sectorSize = SectorTransferSize();
        if (sector == 0 || sectorSize <= 0) { TransferBytesRemaining = 0; return; }
        var offset = (sector - 1) * sectorSize + byteInSector;
        if (_usingDrive) _transferBuffer[byteInSector] = value;
        TransferBytesRemaining--;
        if (DiskData != null && track < Tracks && head < Heads && offset < DiskData[track][head].Length)
            DiskData[track][head][offset] = value;
    }

    private static int CommandLength(byte cmd)
    {
        return (cmd & 0x1F) switch
        {
            0x03 => 3,
            0x04 => 2,
            0x05 or 0x06 or 0x0C => 9,
            0x07 => 2,
            0x08 => 1,
            0x0A => 2,
            0x0D => 6,
            0x0F => 3,
            0x11 or 0x19 or 0x1D => 9,
            _ => 0
        };
    }

    private static byte[][][]? CloneDiskData(byte[][][]? source) => source is null ? null :
        source.Select(track => track.Select(head => head.ToArray()).ToArray()).ToArray();

    private void ExecuteCommand()
    {
        var cmd = CommandBuffer[0] & 0x1F;
        switch (cmd)
        {
            case 0x07:
                Cylinder = 0;
                _interruptStatus = SelectedDrive() switch
                {
                    null => 0x20,
                    { } drive when drive.Seek(0) => 0x20,
                    _ => 0x60
                };
                InterruptPending = true;
                CurrentPhase = Phase.Idle;
                break;
            case 0x0F:
                _interruptStatus = SelectedDrive() switch
                {
                    null => 0x20,
                    { } drive when drive.Seek(CommandBuffer[2]) => 0x20,
                    _ => 0x60
                };
                if ((_interruptStatus & 0x40) == 0) Cylinder = CommandBuffer[2];
                InterruptPending = true;
                CurrentPhase = Phase.Idle;
                break;
            case 0x08:
                ResultReady(2);
                ResultBuffer[0] = InterruptPending ? _interruptStatus : (byte)0x80;
                ResultBuffer[1] = (byte)Cylinder;
                InterruptPending = false;
                break;
            case 0x04:
                SelectedDrive()?.SelectHead((byte)((CommandBuffer[1] >> 2) & 1));
                BuildDriveStatus();
                break;
            case 0x03:
                StepRateHeadUnload = CommandBuffer[1];
                HeadLoadNonDma = CommandBuffer[2];
                CurrentPhase = Phase.Idle;
                break;
            case 0x06 or 0x0C:
                SelectedDrive()?.SelectHead((byte)((CommandBuffer[1] >> 2) & 1));
                _usingDrive = PrepareDriveTransfer(false);
                if (_transferError) { BuildErrorResult(SelectedDrive() is DskFloppyDrive d && !d.Ready ? (byte)0 : (byte)0x04, SelectedDrive() is DskFloppyDrive d2 && !d2.Ready ? (byte)0x48 : (byte)0x40); break; }
                TransferBytesRemaining = SectorTransferSize();
                CurrentPhase = Phase.Execution;
                ArmExecutionDeadline();
                break;
            case 0x05:
                SelectedDrive()?.SelectHead((byte)((CommandBuffer[1] >> 2) & 1));
                _usingDrive = PrepareDriveTransfer(true);
                if (_transferError) { BuildErrorResult(SelectedDrive() is DskFloppyDrive d && !d.Ready ? (byte)0 : (byte)0x04, SelectedDrive() is DskFloppyDrive d2 && !d2.Ready ? (byte)0x48 : (byte)0x40); break; }
                TransferBytesRemaining = SectorTransferSize();
                CurrentPhase = Phase.Execution;
                ArmExecutionDeadline();
                break;
            case 0x0A:
                SelectedDrive()?.SelectHead((byte)((CommandBuffer[1] >> 2) & 1));
                BuildReadIdResult();
                break;
            case 0x0D:
                SelectedDrive()?.SelectHead((byte)((CommandBuffer[1] >> 2) & 1));
                if (CommandBuffer[3] == 0) { InvalidCommand(); break; }
                _transferBuffer = new byte[CommandBuffer[3] * 4];
                _formatIdIndex = 0;
                TransferBytesRemaining = _transferBuffer.Length;
                CurrentPhase = Phase.Execution;
                ArmExecutionDeadline();
                break;
            default:
                InvalidCommand();
                break;
        }
    }

    private void BuildReadResult()
    {
        BufferIndex = 0;
        ResultCount = 7;
        ResultBuffer[0] = 0x00;
        ResultBuffer[1] = SelectedDrive() is DskFloppyDrive drive ? drive.LastStatus1 : (byte)0;
        ResultBuffer[2] = SelectedDrive() is DskFloppyDrive drive2 ? drive2.LastStatus2 : (byte)0;
        ResultBuffer[3] = (byte)Cylinder;
        ResultBuffer[4] = CommandBuffer[3];
        ResultBuffer[5] = CommandBuffer[4];
        ResultBuffer[6] = CommandBuffer[5];
    }

    private void BuildWriteResult()
    {
        if (_usingDrive && !SelectedDrive()!.WriteSector(CommandBuffer[2], CommandBuffer[3], CommandBuffer[4], CommandBuffer[5], _transferBuffer))
        {
            BuildErrorResult(0x02); // write protected
            return;
        }
        BuildReadResult();
    }

    private bool AdvanceDataTransfer(bool write)
    {
        byte endOfTrack = CommandBuffer[6];
        if (CommandBuffer[4] == endOfTrack || (SelectedDrive() is not DskFloppyDrive && CommandBuffer[4] > endOfTrack)) return false;
        if (write && _usingDrive && !SelectedDrive()!.WriteSector(CommandBuffer[2], CommandBuffer[3], CommandBuffer[4], CommandBuffer[5], _transferBuffer))
        {
            BuildErrorResult(0x04);
            return false;
        }
        if (SelectedDrive() is DskFloppyDrive drive)
        {
            byte? next = drive.NextSectorId(CommandBuffer[2], CommandBuffer[3], CommandBuffer[4], CommandBuffer[5]);
            if (next is null)
            {
                BuildErrorResult(0x80); // end of cylinder
                return false;
            }
            CommandBuffer[4] = next.Value;
        }
        else
            CommandBuffer[4]++;
        if (_usingDrive)
        {
            Array.Clear(_transferBuffer);
            if (!write && !SelectedDrive()!.ReadSector(CommandBuffer[2], CommandBuffer[3], CommandBuffer[4], CommandBuffer[5], _transferBuffer))
            {
                BuildErrorResult(0x04);
                return false;
            }
        }
        TransferBytesRemaining = SectorTransferSize();
        return true;
    }

    private void BuildReadIdResult()
    {
        BufferIndex = 0;
        ResultCount = 7;
        ResultBuffer[0] = 0x00;
        ResultBuffer[1] = 0x00;
        ResultBuffer[2] = 0x00;
        ResultBuffer[3] = (byte)Cylinder;
        ResultBuffer[4] = 0;
        ResultBuffer[5] = 1;
        ResultBuffer[6] = (byte)(SectorSize > 0 ? Math.Log2(SectorSize / 128) : 0);
        if (SelectedDrive() is not DskFloppyDrive drive || !drive.Ready)
        {
            ResultBuffer[0] = 0x48;
        }
        else if (drive.Image.FindTrack((byte)drive.Cylinder, (byte)drive.Head)?.Sectors.FirstOrDefault() is not { } sector)
        {
            ResultBuffer[0] = 0x08;
        }
        else
        {
            ResultBuffer[3] = (byte)drive.Cylinder;
            ResultBuffer[4] = (byte)drive.Head;
            ResultBuffer[5] = sector.Id;
            ResultBuffer[6] = sector.SizeCode;
        }
        CurrentPhase = Phase.Result;
    }

    private void FormatTrack()
    {
        ResultReady();
    }

    private void BuildDriveStatus()
    {
        byte drive = (byte)(CommandBuffer[1] & 1);
        IFloppyDrive? selected = drive switch { 0 => Drive0, 1 => Drive1, _ => null };
        ResultReady(1);
        ResultBuffer[0] = (byte)(CommandBuffer[1] & 7); // preserve drive and head select bits
        if (selected is null) return;
        if (selected.Ready) ResultBuffer[0] |= 0x20;
        if (selected.WriteProtected) ResultBuffer[0] |= 0x40;
        if (selected.Head == 1) ResultBuffer[0] |= 0x04;
        if (selected.TrackZero) ResultBuffer[0] |= 0x10;
    }

    private void InvalidCommand()
    {
        ResultReady(1);
        ResultBuffer[0] = 0x80;
    }

    private void BuildFormatResult(byte head)
    {
        ResultReady();
        ResultBuffer[3] = (byte)Cylinder;
        ResultBuffer[4] = head;
        ResultBuffer[5] = _transferBuffer[^4];
        ResultBuffer[6] = _transferBuffer[^1];
    }

    private void BuildErrorResult(byte status1, byte status0 = 0x40)
    {
        ResultReady();
        ResultBuffer[0] = status0;
        ResultBuffer[1] = status1;
        ResultBuffer[3] = CommandBuffer[2];
        ResultBuffer[4] = CommandBuffer[3];
        ResultBuffer[5] = CommandBuffer[4];
        ResultBuffer[6] = CommandBuffer[5];
        _transferError = false;
    }

    private int SectorTransferSize() => SectorSize > 0 ? SectorSize : 128 << CommandBuffer[5];

    private IFloppyDrive? SelectedDrive() => (CommandBuffer[1] & 3) switch
    {
        0 => Drive0,
        1 => Drive1,
        _ => null,
    };

    private bool PrepareDriveTransfer(bool write)
    {
        _transferError = false;
        IFloppyDrive? drive = SelectedDrive();
        if (drive is null)
        {
            _transferError = DiskData is null || !ValidDiskDataSector();
            return false;
        }
        if (!write && (CommandBuffer[0] & 0x1F) == 0x0C &&
            !(drive switch { DskFloppyDrive d => d.IsDeletedSector(CommandBuffer[2], CommandBuffer[3], CommandBuffer[4], CommandBuffer[5]), _ => true }))
        {
            _transferError = true;
            return false;
        }
        _transferBuffer = new byte[128 << CommandBuffer[5]];
        if (write)
        {
            if (DiskData != null && !ValidDiskDataSector()) _transferError = true;
            return !_transferError;
        }
        if (drive.ReadSector(CommandBuffer[2], CommandBuffer[3], CommandBuffer[4], CommandBuffer[5], _transferBuffer)) return true;
        if (DiskData != null && ValidDiskDataSector()) return true;
        _transferError = true;
        return false;
    }

    private bool ValidDiskDataSector() =>
        CommandBuffer[2] < Tracks && CommandBuffer[3] < Heads &&
        CommandBuffer[4] > 0 && CommandBuffer[4] <= SectorsPerTrack;

    private void ResultReady(int resultCount = 7)
    {
        BufferIndex = 0;
        ResultCount = resultCount;
        ResultBuffer[0] = 0x00;
        CurrentPhase = Phase.Result;
        _executionCyclesUntilOverrun = 0;
    }

    private void ArmExecutionDeadline() => _executionCyclesUntilOverrun = CpcCyclesPerDataByte;
}

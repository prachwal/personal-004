namespace PetEmulator.Pet.CbmDos;

/// <summary>
/// Minimal CBM DOS command/data-channel engine: LOAD, SAVE, directory listing ("$"), error
/// channel. Knows nothing about IEEE-488 or the CPU bus - see <see cref="PetIeeeDiskDrive"/> for
/// that.
/// </summary>
public sealed class CbmDosEngine
{
    private D64Image? _image;
    private readonly List<byte> _commandBuffer = [];
    private readonly List<byte> _fileOutput = [];
    private int _filePosition;
    private readonly List<byte> _errorOutput = [];
    private int _errorPosition;
    private byte _currentSecAddr = 0xFF;
    private bool _waitingForFilename;
    private bool _saveMode;
    private List<byte> _saveFilenameBytes = [];
    private bool _saveDataPhase;
    private bool _channelOpen;

    public bool DataAvailable =>
        _currentSecAddr == 15
            ? _errorPosition < _errorOutput.Count
            : _filePosition < _fileOutput.Count;

    public void AttachImage(D64Image image)
    {
        _image = image;
        SetError("73,CBM DOS V2.6 4040,00,00");
        _fileOutput.Clear();
        _filePosition = 0;
        _saveMode = false;
        _saveFilenameBytes.Clear();
        _saveDataPhase = false;
    }

    public void OpenChannel(byte secAddr)
    {
        _currentSecAddr = secAddr;
        _commandBuffer.Clear();
        _channelOpen = true;

        switch (secAddr)
        {
            case 15:
                _waitingForFilename = false;
                _saveMode = false;
                break;

            case 0:
                _waitingForFilename = true;
                _saveMode = false;
                break;

            case 1:
                _waitingForFilename = !_saveDataPhase;
                _saveMode = true;
                break;

            default:
                // Channels 2-14: general file channels (BASIC 4 OPEN/GET#/INPUT#)
                if (secAddr <= 14)
                {
                    _waitingForFilename = true;
                    _saveMode = false;
                }
                else
                {
                    SetError("65,NO CHANNEL,00,00");
                }
                break;
        }
    }

    public void CloseChannel()
    {
        // The bus closes the addressed device twice per ATN cycle in the common case: once
        // automatically when ATN reasserts after a DataOut transfer (PetIeeeBus.OnATNWrite), and
        // again when it decodes the UNLISTEN byte that follows (or BASIC 4's CLOSE-SA, which
        // skips UNLISTEN but hits the same auto-close) - both are legitimate real-hardware
        // signals a device must tolerate, not a bug in the bus. Without this guard, the second
        // call finds the just-received filename/data still sitting in _commandBuffer with
        // _waitingForFilename already cleared by the first call, and misreads it as a DOS command
        // (typically producing a bogus "30, SYNTAX ERROR" instead of the real LOAD/SAVE result).
        if (!_channelOpen)
            return;
        _channelOpen = false;

        if (_saveMode && _saveDataPhase)
        {
            if (_commandBuffer.Count > 0)
                WriteSaveFile();
            _saveMode = false;
            _saveDataPhase = false;
            _waitingForFilename = false;
            return;
        }

        if (_saveMode && _waitingForFilename && _commandBuffer.Count > 0)
        {
            ProcessSaveFilename();
            _waitingForFilename = false;
            _saveDataPhase = true;
            return;
        }

        if (_saveMode && _waitingForFilename)
        {
            SetError("30,SYNTAX ERROR,00,00");
            _saveMode = false;
            _waitingForFilename = false;
            return;
        }

        if (_waitingForFilename && _commandBuffer.Count > 0)
        {
            ProcessFilename();
            _waitingForFilename = false;
            return;
        }

        if (_commandBuffer.Count > 0 && !_waitingForFilename && !_saveMode)
        {
            ProcessCommand();
            return;
        }

        _waitingForFilename = false;
    }

    public void ReceiveByte(byte data)
    {
        // ponytail: PetIeeeBus.OnDioWrite can't yet distinguish a genuine data byte from the
        // KERNAL's own between-byte "release DIO to idle" write - it decodes to a literal 0x00
        // here (see docs/pet-disk-testing-strategy.md's bug #4; confirmed against VICE's real
        // parallel.c, which gates byte capture on a DAV edge our binding doesn't reach for this
        // KERNAL's write routine). Real PETSCII filenames never contain a literal 0x00 byte, so
        // it's safe to drop while collecting one; NOT safe during SAVE's actual data phase
        // (tokenized BASIC legitimately uses 0x00 as a line terminator), so this stays scoped to
        // _waitingForFilename rather than becoming a blanket filter. Upgrade path: fix the real
        // gate in PetIeeeBus/PetIeeeBusBinding once the KERNAL's actual per-byte strobe signal
        // for this routine is identified.
        if (_waitingForFilename && data == 0x00)
            return;
        _commandBuffer.Add(data);
    }

    public bool TryGetByte(out byte data)
    {
        if (_currentSecAddr == 15)
        {
            if (_errorPosition < _errorOutput.Count)
            {
                data = _errorOutput[_errorPosition++];
                return true;
            }
        }
        else
        {
            if (_filePosition < _fileOutput.Count)
            {
                data = _fileOutput[_filePosition++];
                return true;
            }
        }
        data = 0;
        return false;
    }

    private void ProcessCommand()
    {
        string cmd = PetsciiToString(_commandBuffer);
        if (string.IsNullOrEmpty(cmd))
        {
            SetError("00, OK,00,00");
            return;
        }

        char c = char.ToUpperInvariant(cmd[0]);
        switch (c)
        {
            case 'I':
            case 'V':
            case 'S':
            case 'R':
            case 'U':
                SetError("00, OK,00,00");
                break;

            default:
                SetError("30,SYNTAX ERROR,00,00");
                break;
        }
    }

    private void ProcessFilename()
    {
        if (_commandBuffer.Count == 1 && _commandBuffer[0] == 0x24)
        {
            GenerateDirectoryListing();
            return;
        }

        if (_image == null)
        {
            SetError("74,DRIVE NOT READY,00,00");
            return;
        }

        var dir = _image.ReadDirectory();
        var entry = FindFileByBytes(dir, _commandBuffer);

        if (entry.FilenameBytes is null)
        {
            SetError("62,FILE NOT FOUND,00,00");
            return;
        }

        byte[] fileData = _image.ReadFile(entry);
        _fileOutput.Clear();
        _fileOutput.AddRange(fileData);
        _filePosition = 0;
        SetError("00, OK,00,00");
    }

    private static DirEntry FindFileByBytes(List<DirEntry> dir, List<byte> nameBytes)
    {
        foreach (var entry in dir)
        {
            if (entry.Type != FileType.Prg || entry.FilenameBytes is null)
                continue;

            if (entry.SizeInSectors == 0)
                continue;

            if (BytesMatch(nameBytes, entry.FilenameBytes))
                return entry;
        }
        return default;
    }

    private static bool BytesMatch(List<byte> a, byte[] b)
    {
        if (a.Count == 0) return false;

        int len = a.Count;
        if (len > b.Length) return false;

        for (int i = 0; i < len; i++)
        {
            byte ba = a[i];
            byte bb = b[i];

            if (ba == bb) continue;

            if (ba >= (byte)'a' && ba <= (byte)'z' && bb == ba - 32) continue;
            if (bb >= (byte)'a' && bb <= (byte)'z' && ba == bb - 32) continue;

            return false;
        }
        return true;
    }

    private void GenerateDirectoryListing()
    {
        if (_image == null)
        {
            SetError("74,DRIVE NOT READY,00,00");
            return;
        }

        var dir = _image.ReadDirectory();
        _fileOutput.Clear();

        ushort addr = 0x0401;
        int totalBlocks = 0;

        _fileOutput.Add((byte)(addr & 0xFF));
        _fileOutput.Add((byte)(addr >> 8));

        // Header line: real CBM DOS directory listings always lead with the disk name/id/DOS
        // type, quoted, before any file entries - e.g. `0 "MY DISK         " 2a 2A`. This was
        // missing entirely (jumped straight to entries/BLOCKS FREE), so LIST never showed the
        // mounted disk's name even though every entry after it was correctly formed.
        string header = $"\"{_image.DiskName,-16}\" {_image.DiskId,-2} 2A";
        ushort headerNextLine = (ushort)(addr + 4 + header.Length + 1);

        _fileOutput.Add((byte)(headerNextLine & 0xFF));
        _fileOutput.Add((byte)(headerNextLine >> 8));
        _fileOutput.Add(0x00);
        _fileOutput.Add(0x00);
        foreach (char ch in header)
            _fileOutput.Add((byte)ch);
        _fileOutput.Add(0x00);
        addr = headerNextLine;

        foreach (var entry in dir)
        {
            if (entry.Type == FileType.Del)
                continue;

            totalBlocks += entry.SizeInSectors;
            int lineLen = 1 + 16 + 1 + 5;
            ushort nextLine = (ushort)(addr + 4 + lineLen);

            _fileOutput.Add((byte)(nextLine & 0xFF));
            _fileOutput.Add((byte)(nextLine >> 8));
            _fileOutput.Add((byte)(addr & 0xFF));
            _fileOutput.Add((byte)(addr >> 8));

            _fileOutput.Add(0x20);
            for (int i = 0; i < 16; i++)
            {
                byte b = entry.FilenameBytes[i];
                _fileOutput.Add(b == 0xA0 ? (byte)0x20 : b);
            }
            _fileOutput.Add(0x20);

            string sizeStr = entry.SizeInSectors.ToString();
            foreach (char ch in sizeStr)
                _fileOutput.Add((byte)ch);

            _fileOutput.Add(0x00);
            addr = nextLine;
        }

        string blocksFree = $"BLOCKS FREE. {664 - totalBlocks}";
        int blocksLen = 1 + 16 + 1 + blocksFree.Length;
        ushort lastLine = (ushort)(addr + 4 + blocksLen + 2);

        _fileOutput.Add((byte)(lastLine & 0xFF));
        _fileOutput.Add((byte)(lastLine >> 8));
        _fileOutput.Add((byte)(addr & 0xFF));
        _fileOutput.Add((byte)(addr >> 8));

        _fileOutput.Add(0x20);
        for (int i = 0; i < 16; i++)
        {
            _fileOutput.Add(i < blocksFree.Length ? (byte)blocksFree[i] : (byte)0x20);
        }
        _fileOutput.Add(0x20);
        foreach (char ch in blocksFree)
            _fileOutput.Add((byte)ch);
        _fileOutput.Add(0x00);
        _fileOutput.Add(0x00);
        _fileOutput.Add(0x00);

        _filePosition = 0;
        SetError("00, OK,00,00");
    }

    private void ProcessSaveFilename()
    {
        _saveFilenameBytes = new List<byte>(_commandBuffer);
        SetError("00, OK,00,00");
    }

    private void WriteSaveFile()
    {
        if (_image == null)
        {
            SetError("74,DRIVE NOT READY,00,00");
            return;
        }

        if (_saveFilenameBytes.Count == 0)
        {
            SetError("30,SYNTAX ERROR,00,00");
            return;
        }

        byte[] data = _commandBuffer.ToArray();
        if (data.Length < 2)
        {
            SetError("30,SYNTAX ERROR,00,00");
            return;
        }

        byte[] filename16 = new byte[16];
        for (int i = 0; i < 16; i++)
            filename16[i] = i < _saveFilenameBytes.Count ? _saveFilenameBytes[i] : (byte)0xA0;

        int totalSectors = (data.Length + 252) / 254;
        if (totalSectors < 1) totalSectors = 1;

        int firstTrack = 0, firstSector = 0;
        int prevTrack = 0, prevSector = 0;
        int dataPos = 0;

        for (int s = 0; s < totalSectors; s++)
        {
            if (!_image.TryAllocateSector(out int t, out int sec))
                break;

            if (s == 0)
            {
                firstTrack = t;
                firstSector = sec;
            }

            byte[] sectorData = new byte[256];

            int copyLen = Math.Min(254, data.Length - dataPos);
            for (int i = 0; i < copyLen; i++)
                sectorData[2 + i] = data[dataPos + i];
            dataPos += copyLen;

            bool isLast = s == totalSectors - 1 || dataPos >= data.Length;
            if (isLast)
            {
                int remaining = dataPos >= data.Length ? (data.Length % 254) : 254;
                if (remaining == 0 && dataPos >= data.Length)
                    remaining = 254;
                if (dataPos > data.Length)
                    remaining = 254 - (dataPos - data.Length);

                sectorData[0] = 0;
                sectorData[1] = (byte)(remaining == 254 ? 0 : remaining);
            }
            else
            {
                sectorData[0] = 0;
                sectorData[1] = 0;
            }

            if (prevTrack > 0)
            {
                byte[] prevData = new byte[256];
                int prevOff = D64Image.TrackSectorToOffset(prevTrack, prevSector);
                Array.Copy(_image.SaveToBytes(), prevOff, prevData, 0, 256);
                prevData[0] = (byte)t;
                prevData[1] = (byte)sec;
                _image.WriteSector(prevTrack, prevSector, prevData);
            }

            _image.WriteSector(t, sec, sectorData);
            prevTrack = t;
            prevSector = sec;

            if (isLast)
                break;
        }

        int totalAllocated = (data.Length + 253) / 254;
        if (totalAllocated < 1) totalAllocated = 1;

        var entry = new DirEntry(
            FileType.Prg, true, false,
            (byte)firstTrack, (byte)firstSector,
            filename16, totalAllocated);

        _image.AddDirectoryEntry(entry);
        _fileOutput.Clear();
        _filePosition = 0;
        SetError("00, OK,00,00");
    }

    private void SetError(string message)
    {
        _errorOutput.Clear();
        foreach (char ch in message)
            _errorOutput.Add((byte)ch);
        _errorOutput.Add(0x0D);
        _errorOutput.Add(0x00);
        _errorPosition = 0;
    }

    private static string PetsciiToString(List<byte> bytes)
    {
        var chars = new char[bytes.Count];
        for (int i = 0; i < bytes.Count; i++)
        {
            byte b = bytes[i];
            chars[i] = b is 0x0D or 0x0A ? ' ' : (char)b;
        }
        return new string(chars).TrimEnd('\0', ' ');
    }

    public void Reset()
    {
        _commandBuffer.Clear();
        _fileOutput.Clear();
        _filePosition = 0;
        _errorOutput.Clear();
        _errorPosition = 0;
        _currentSecAddr = 0xFF;
        _waitingForFilename = false;
        SetError("73,CBM DOS V2.6 4040,00,00");
    }
}

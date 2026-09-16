using System.Text;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.CpcFdc;

namespace PetEmulator.CpcFdc.Tests;

public sealed class I8272ChipTests
{
    [Test]
    public void Read_data_returns_sector_bytes_and_success_result()
    {
        var fdc = CreateController(0x5A);

        Send(fdc, 0x46, 0, 0, 0, 1, 2, 1, 0x1B, 0xFF);
        var data = ReadBytes(fdc, 512);
        var result = ReadResult(fdc);

        data.Should().OnlyContain(value => value == 0x5A);
        result[0].Should().Be(0);
        result[5].Should().Be(1);
        result[6].Should().Be(2);
    }

    [Test]
    public void Write_data_updates_sector_and_returns_success_result()
    {
        var fdc = CreateController(0x00);

        Send(fdc, 0x45, 0, 0, 0, 1, 2, 1, 0x1B, 0xFF);
        for (var index = 0; index < 512; index++) fdc.WriteDataRegister(0xA6);
        var result = ReadResult(fdc);

        result[0].Should().Be(0);
        var image = ((DskFloppyDrive)fdc.Drive0!).Image;
        image.TryRead(0, 0, 1, 2, new byte[512]).Should().BeTrue();
        var data = new byte[512];
        image.TryRead(0, 0, 1, 2, data);
        data.Should().OnlyContain(value => value == 0xA6);
    }

    [Test]
    public void Seek_and_sense_interrupt_report_new_cylinder_and_clear_pending_interrupt()
    {
        var fdc = CreateController(0x00, tracks: 3);

        Send(fdc, 0x0F, 0, 2);
        fdc.InterruptPending.Should().BeTrue();
        Send(fdc, 0x08);
        var result = ReadResult(fdc, 2);

        result[0].Should().Be(0x20);
        result[1].Should().Be(2);
        fdc.InterruptPending.Should().BeFalse();
        ((DskFloppyDrive)fdc.Drive0!).Cylinder.Should().Be(2);
    }

    [Test]
    public void Read_id_returns_first_sector_id_and_size_code()
    {
        var fdc = CreateController(0x00);

        Send(fdc, 0x4A, 0);
        var result = ReadResult(fdc);

        result[0].Should().Be(0);
        result[3].Should().Be(0);
        result[4].Should().Be(0);
        result[5].Should().Be(1);
        result[6].Should().Be(2);
    }

    [Test]
    public void Read_without_ready_drive_returns_not_ready_status()
    {
        var fdc = CreateController(0x00);
        ((DskFloppyDrive)fdc.Drive0!).MotorOn = false;

        Send(fdc, 0x46, 0, 0, 0, 1, 2, 1, 0x1B, 0xFF);
        var result = ReadResult(fdc);

        result[0].Should().Be(0x48);
    }

    [Test]
    public void Write_protected_drive_returns_write_protect_status_after_transfer()
    {
        var fdc = CreateController(0x00);
        ((DskFloppyDrive)fdc.Drive0!).WriteProtected = true;

        Send(fdc, 0x45, 0, 0, 0, 1, 2, 1, 0x1B, 0xFF);
        for (var index = 0; index < 512; index++) fdc.WriteDataRegister(0xA6);
        var result = ReadResult(fdc);

        result[1].Should().Be(0x02);
    }

    [Test]
    public void Read_past_last_sector_returns_end_of_cylinder_status()
    {
        var fdc = CreateController(0x00);

        Send(fdc, 0x46, 0, 0, 0, 1, 2, 2, 0x1B, 0xFF);
        _ = ReadBytes(fdc, 512);
        var result = ReadResult(fdc);

        result[0].Should().Be(0x40);
        result[1].Should().Be(0x80);
    }

    [Test]
    public void Invalid_command_returns_invalid_command_status()
    {
        var fdc = new I8272Chip();

        Send(fdc, 0x7F);

        ReadResult(fdc, 1)[0].Should().Be(0x80);
    }

    private static I8272Chip CreateController(byte fill, int tracks = 1)
    {
        var fdc = new I8272Chip();
        var image = DskDiskImage.Load(CreateDsk(fill, tracks));
        fdc.Drive0 = new DskFloppyDrive(image) { MotorOn = true };
        return fdc;
    }

    private static void Send(I8272Chip fdc, params byte[] command)
    {
        foreach (var value in command) fdc.WritePort(0xF2, value);
    }

    private static byte[] ReadBytes(I8272Chip fdc, int count)
    {
        var data = new byte[count];
        for (var index = 0; index < data.Length; index++) data[index] = fdc.ReadDataRegister();
        return data;
    }

    private static byte[] ReadResult(I8272Chip fdc, int count = 7) => ReadBytes(fdc, count);

    private static byte[] CreateDsk(byte fill, int tracks)
    {
        const int trackLength = 0x300;
        var dsk = new byte[0x100 + tracks * trackLength];
        Encoding.ASCII.GetBytes("MV - CPCEMU Disk-File\r\nDisk-Info\r\n").CopyTo(dsk, 0);
        dsk[0x30] = (byte)tracks;
        dsk[0x31] = 1;
        dsk[0x32] = 0;
        dsk[0x33] = 3;
        for (var track = 0; track < tracks; track++)
        {
            var offset = 0x100 + track * trackLength;
            Encoding.ASCII.GetBytes("Track-Info\r\n").CopyTo(dsk, offset);
            dsk[offset + 0x10] = (byte)track;
            dsk[offset + 0x11] = 0;
            dsk[offset + 0x15] = 1;
            dsk[offset + 0x1A] = 1;
            dsk[offset + 0x1B] = 2;
            dsk.AsSpan(offset + 0x100, 512).Fill(fill);
        }
        return dsk;
    }
}

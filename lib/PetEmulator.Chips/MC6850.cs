using PetEmulator.Core;

namespace PetEmulator.Chips;

/// <summary>
/// Motorola MC6850 Asynchronous Communications Interface Adapter.
///
/// The model covers the two memory-mapped registers, receive/transmit status,
/// overrun indication and the receive/transmit IRQ enables. Transport timing is
/// external: call <see cref="Receive"/> when a byte arrives and
/// <see cref="TransmitComplete"/> when the attached serial device has sent the
/// byte. This is the behavior used by the existing Osborne/SuperPET serial
/// implementations in the earlier emulator repositories.
/// </summary>
public sealed class MC6850 : IMemoryMappedDevice
{
    public const byte ReceiveDataRegisterFull = 0x01;
    public const byte TransmitDataRegisterEmpty = 0x02;
    public const byte Overrun = 0x20;
    public const byte InterruptRequest = 0x80;
    public const byte ReceiveInterruptEnable = 0x80;
    public const byte TransmitInterruptEnable = 0x20;

    private readonly ushort _baseAddress;

    public MC6850(string name = "MC6850 ACIA", ushort baseAddress = 0)
    {
        if ((uint)baseAddress + 2 > 0x1_0000)
            throw new ArgumentOutOfRangeException(nameof(baseAddress), "The ACIA must fit in the 16-bit address space.");

        Name = name;
        _baseAddress = baseAddress;
        Reset();
    }

    public string Name { get; }

    public uint Length => 2;

    public byte Control { get; private set; }

    public byte Status { get; private set; }

    public byte TxData { get; private set; }

    public byte RxData { get; private set; }

    public bool Irq => (Status & InterruptRequest) != 0;

    public void Reset()
    {
        Control = 0;
        Status = TransmitDataRegisterEmpty;
        TxData = 0;
        RxData = 0;
    }

    public void Tick(ulong cycles)
    {
        // Transmission timing belongs to the attached serial transport.
    }

    public byte Read(ushort address)
    {
        var offset = GetOffset(address);
        if (offset == 1)
        {
            Status &= unchecked((byte)~ReceiveDataRegisterFull);
            UpdateIrq();
            return RxData;
        }

        UpdateIrq();
        return Status;
    }

    public void Write(ushort address, byte value)
    {
        var offset = GetOffset(address);
        switch (offset)
        {
            case 0:
                Control = value;
                if ((value & 0x03) == 0x03)
                    Status = TransmitDataRegisterEmpty;
                UpdateIrq();
                break;
            case 1:
                TxData = value;
                Status &= unchecked((byte)~TransmitDataRegisterEmpty);
                UpdateIrq();
                break;
        }
    }

    public void TransmitComplete()
    {
        Status |= TransmitDataRegisterEmpty;
        UpdateIrq();
    }

    public void Receive(byte data)
    {
        if ((Status & ReceiveDataRegisterFull) != 0)
            Status |= Overrun;

        RxData = data;
        Status |= ReceiveDataRegisterFull;
        UpdateIrq();
    }

    private ushort GetOffset(ushort address)
    {
        if (address < _baseAddress || address >= _baseAddress + Length)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Address is outside the ACIA range.");

        return (ushort)(address - _baseAddress);
    }

    private void UpdateIrq()
    {
        var rxIrq = (Control & ReceiveInterruptEnable) != 0 && (Status & ReceiveDataRegisterFull) != 0;
        var txIrq = (Control & 0x60) == TransmitInterruptEnable && (Status & TransmitDataRegisterEmpty) != 0;
        if (rxIrq || txIrq)
            Status |= InterruptRequest;
        else
            Status &= unchecked((byte)~InterruptRequest);
    }
}

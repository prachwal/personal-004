using PetEmulator.Chips;
using PetEmulator.Core;

namespace PetEmulator.Trs80;

public sealed class Trs80FdcWiring : IMemoryBus
{
    public Trs80FdcWiring(IFD1791DiskImage? disk = null)
    {
        Controller = new FD1793(disk);
        Controller.DriveSelect = 1;
    }
    public FD1793 Controller { get; }
    public int? SelectedDrive { get; private set; } = 0;
    public byte Read(ushort address) => address <= 0x37E3 ? (byte)(SelectedDrive ?? 0) : address is >= 0x37EC and <= 0x37EF ? Controller.Read((byte)(address - 0x37EC)) : (byte)0xFF;
    public void Write(ushort address, byte value)
    {
        if (address <= 0x37E3)
        {
            SelectedDrive = value switch { 1 => 0, 2 => 1, 4 => 2, 8 => 3, _ => null };
            Controller.DriveSelect = value;
        }
        else if (address is >= 0x37EC and <= 0x37EF) Controller.Write((byte)(address - 0x37EC), value);
    }
    public void Tick(int tStates) => Controller.Tick(tStates);
    public Trs80FdcSnapshot CaptureState() => new() { Controller = Controller.CaptureState(), SelectedDrive = SelectedDrive };
    public void RestoreState(Trs80FdcSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Controller.RestoreState(state.Controller);
        SelectedDrive = state.SelectedDrive;
    }
    public void Reset() { Controller.Reset(); SelectedDrive = null; }
}

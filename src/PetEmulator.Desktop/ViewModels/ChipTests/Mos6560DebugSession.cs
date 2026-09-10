using PetEmulator.Chips;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mos6560DebugSession : ChipDebugSessionBase
{
    public Mos6560DebugSession()
        : base("MOS 6560 VIC", CreateDefinition()) { }

    private static ChipDebugDefinition CreateDefinition()
    {
        var chip = new MOS6560("Chip Tester VIC");
        return new ChipDebugDefinition(
            () => chip.Tick(1), chip.Reset,
            name => chip.Read(ParseRegister(name)),
            (name, value) => chip.Write(ParseRegister(name), value),
            Enumerable.Range(0, MOS6560.RegisterCount)
                .Select(index => (Name: $"R{index:X1}", Read: (Func<byte>)(() => chip.Read((ushort)index))))
                .ToArray(), []);
    }

    private static ushort ParseRegister(string name) => Convert.ToUInt16(name.TrimStart('R'), 16);
}

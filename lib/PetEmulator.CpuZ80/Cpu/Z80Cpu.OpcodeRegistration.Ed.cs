using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    private void RegisterEdOpcodes()
    {
        RegisterOpcode(0xED, 0x45, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x4D, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x55, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x5D, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x65, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x6D, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x75, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x7D, ReturnFromInterrupt);
        RegisterOpcode(0xED, 0x47, LoadInterruptRegister);
        RegisterOpcode(0xED, 0x4F, LoadRefreshRegister);
        RegisterOpcode(0xED, 0x57, LoadAccumulatorFromInterrupt);
        RegisterOpcode(0xED, 0x5F, LoadAccumulatorFromRefresh);
        RegisterOpcode(0xED, 0x56, () => SetInterruptMode(1));
        RegisterOpcode(0xED, 0x5E, () => SetInterruptMode(2));
        RegisterOpcode(0xED, 0x67, RotateRightDecimal);
        RegisterOpcode(0xED, 0x6F, RotateLeftDecimal);
        RegisterOpcode(0xED, 0xA0, () => BlockTransfer(true, false));
        RegisterOpcode(0xED, 0xA1, () => BlockCompare(true, false));
        RegisterOpcode(0xED, 0xA8, () => BlockTransfer(false, false));
        RegisterOpcode(0xED, 0xA9, () => BlockCompare(false, false));
        RegisterOpcode(0xED, 0xB0, () => BlockTransfer(true, true));
        RegisterOpcode(0xED, 0xB1, () => BlockCompare(true, true));
        RegisterOpcode(0xED, 0xB8, () => BlockTransfer(false, true));
        RegisterOpcode(0xED, 0xB9, () => BlockCompare(false, true));
        RegisterOpcode(0xED, 0xA2, () => BlockInput(true, false));
        RegisterOpcode(0xED, 0xAA, () => BlockInput(false, false));
        RegisterOpcode(0xED, 0xB2, () => BlockInput(true, true));
        RegisterOpcode(0xED, 0xBA, () => BlockInput(false, true));
        RegisterOpcode(0xED, 0xA3, () => BlockOutput(true, false));
        RegisterOpcode(0xED, 0xAB, () => BlockOutput(false, false));
        RegisterOpcode(0xED, 0xB3, () => BlockOutput(true, true));
        RegisterOpcode(0xED, 0xBB, () => BlockOutput(false, true));

        for (var edOpcode = 0; edOpcode <= byte.MaxValue; edOpcode++)
        {
            if ((edOpcode & 0xCF) == 0x42)
            {
                var arithmeticOpcode = (byte)edOpcode;
                RegisterOpcode(0xED, arithmeticOpcode, () => AddPairWithCarry(arithmeticOpcode, true));
            }
            else if ((edOpcode & 0xCF) == 0x4A)
            {
                var arithmeticOpcode = (byte)edOpcode;
                RegisterOpcode(0xED, arithmeticOpcode, () => AddPairWithCarry(arithmeticOpcode, false));
            }
            else if ((edOpcode & 0xC7) == 0x44)
            {
                RegisterOpcode(0xED, (byte)edOpcode, NegateAccumulator);
            }
            else if ((edOpcode & 0xC7) == 0x46)
            {
                var modeOpcode = (byte)edOpcode;
                RegisterOpcode(0xED, modeOpcode, () => SetInterruptMode((byte)(((modeOpcode >> 3) & 3) == 0 ? 0 : 1)));
            }
            else if ((edOpcode & 0xCF) == 0x43)
            {
                var storeOpcode = (byte)edOpcode;
                RegisterOpcode(0xED, storeOpcode, () => StorePairAbsolute(storeOpcode));
            }
            else if ((edOpcode & 0xCF) == 0x4B)
            {
                var loadOpcode = (byte)edOpcode;
                RegisterOpcode(0xED, loadOpcode, () => LoadPairAbsolute(loadOpcode));
            }
        }

        RegisterOpcode(0xED, 0x46, () => SetInterruptMode(0));
        RegisterOpcode(0xED, 0x4E, () => SetInterruptMode(0));
        RegisterOpcode(0xED, 0x66, () => SetInterruptMode(0));
        RegisterOpcode(0xED, 0x6E, () => SetInterruptMode(0));
        RegisterOpcode(0xED, 0x56, () => SetInterruptMode(1));
        RegisterOpcode(0xED, 0x76, () => SetInterruptMode(1));
        RegisterOpcode(0xED, 0x5E, () => SetInterruptMode(2));
        RegisterOpcode(0xED, 0x7E, () => SetInterruptMode(2));
    }
}

using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

public partial class Z80Cpu
{
    protected override void ConfigureOpcodes(OpcodeTable<Z80State> table)
    {
        RegisterBaseOpcodes();

        RegisterEdOpcode(0x45, ReturnFromInterrupt);
        RegisterEdOpcode(0x4D, ReturnFromInterrupt);
        RegisterEdOpcode(0x55, ReturnFromInterrupt);
        RegisterEdOpcode(0x5D, ReturnFromInterrupt);
        RegisterEdOpcode(0x65, ReturnFromInterrupt);
        RegisterEdOpcode(0x6D, ReturnFromInterrupt);
        RegisterEdOpcode(0x75, ReturnFromInterrupt);
        RegisterEdOpcode(0x7D, ReturnFromInterrupt);
        RegisterEdOpcode(0x47, LoadInterruptRegister);
        RegisterEdOpcode(0x4F, LoadRefreshRegister);
        RegisterEdOpcode(0x57, LoadAccumulatorFromInterrupt);
        RegisterEdOpcode(0x5F, LoadAccumulatorFromRefresh);
        RegisterEdOpcode(0x56, () => SetInterruptMode(1));
        RegisterEdOpcode(0x5E, () => SetInterruptMode(2));
        RegisterEdOpcode(0x67, RotateRightDecimal);
        RegisterEdOpcode(0x6F, RotateLeftDecimal);
        RegisterEdOpcode(0xA0, () => BlockTransfer(true, false));
        RegisterEdOpcode(0xA1, () => BlockCompare(true, false));
        RegisterEdOpcode(0xA8, () => BlockTransfer(false, false));
        RegisterEdOpcode(0xA9, () => BlockCompare(false, false));
        RegisterEdOpcode(0xB0, () => BlockTransfer(true, true));
        RegisterEdOpcode(0xB1, () => BlockCompare(true, true));
        RegisterEdOpcode(0xB8, () => BlockTransfer(false, true));
        RegisterEdOpcode(0xB9, () => BlockCompare(false, true));
        RegisterEdOpcode(0xA2, () => BlockInput(true, false));
        RegisterEdOpcode(0xAA, () => BlockInput(false, false));
        RegisterEdOpcode(0xB2, () => BlockInput(true, true));
        RegisterEdOpcode(0xBA, () => BlockInput(false, true));
        RegisterEdOpcode(0xA3, () => BlockOutput(true, false));
        RegisterEdOpcode(0xAB, () => BlockOutput(false, false));
        RegisterEdOpcode(0xB3, () => BlockOutput(true, true));
        RegisterEdOpcode(0xBB, () => BlockOutput(false, true));

        for (var edOpcode = 0; edOpcode <= byte.MaxValue; edOpcode++)
        {
            if ((edOpcode & 0xC7) == 0x40)
            {
                var inputOpcode = (byte)edOpcode;
                RegisterEdOpcode(inputOpcode, () => InputRegister((inputOpcode >> 3) & 7));
            }
            else if ((edOpcode & 0xC7) == 0x41)
            {
                var outputOpcode = (byte)edOpcode;
                RegisterEdOpcode(outputOpcode, () => OutputRegister((outputOpcode >> 3) & 7));
            }
            else if ((edOpcode & 0xCF) == 0x42)
            {
                var arithmeticOpcode = (byte)edOpcode;
                RegisterEdOpcode(arithmeticOpcode, () => AddPairWithCarry(arithmeticOpcode, true));
            }
            else if ((edOpcode & 0xCF) == 0x4A)
            {
                var arithmeticOpcode = (byte)edOpcode;
                RegisterEdOpcode(arithmeticOpcode, () => AddPairWithCarry(arithmeticOpcode, false));
            }
            else if ((edOpcode & 0xC7) == 0x44)
            {
                RegisterEdOpcode((byte)edOpcode, NegateAccumulator);
            }
            else if ((edOpcode & 0xC7) == 0x46)
            {
                var modeOpcode = (byte)edOpcode;
                RegisterEdOpcode(modeOpcode, () => SetInterruptMode((byte)(((modeOpcode >> 3) & 3) == 0 ? 0 : 1)));
            }
            else if ((edOpcode & 0xCF) == 0x43)
            {
                var storeOpcode = (byte)edOpcode;
                RegisterEdOpcode(storeOpcode, () => StorePairAbsolute(storeOpcode));
            }
            else if ((edOpcode & 0xCF) == 0x4B)
            {
                var loadOpcode = (byte)edOpcode;
                RegisterEdOpcode(loadOpcode, () => LoadPairAbsolute(loadOpcode));
            }
        }

        RegisterEdOpcode(0x46, () => SetInterruptMode(0));
        RegisterEdOpcode(0x4E, () => SetInterruptMode(0));
        RegisterEdOpcode(0x66, () => SetInterruptMode(0));
        RegisterEdOpcode(0x6E, () => SetInterruptMode(0));
        RegisterEdOpcode(0x56, () => SetInterruptMode(1));
        RegisterEdOpcode(0x76, () => SetInterruptMode(1));
        RegisterEdOpcode(0x5E, () => SetInterruptMode(2));
        RegisterEdOpcode(0x7E, () => SetInterruptMode(2));

        for (var cbOpcode = 0; cbOpcode <= byte.MaxValue; cbOpcode++)
        {
            var subOpcode = (byte)cbOpcode;
            RegisterPageOpcode(0xCB, subOpcode, () => ExecuteCbOpcode(subOpcode));
        }

        for (var indexedOpcode = 0; indexedOpcode <= byte.MaxValue; indexedOpcode++)
        {
            var subOpcode = (byte)indexedOpcode;
            RegisterPageOpcode(0xDD, subOpcode, () => ExecuteIndexedOpcode(false, subOpcode));
            RegisterPageOpcode(0xFD, subOpcode, () => ExecuteIndexedOpcode(true, subOpcode));
        }
    }
}

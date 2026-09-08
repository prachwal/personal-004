namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Obsługa przerwań



    /// <summary>
    /// Wstrzykuje sekwencję obsługi przerwania.
    /// </summary>
    /// <param name="type">Typ przerwania (IRQ, NMI, BRK).</param>
    private void InjectInterrupt(InterruptType type)
    {
        _waitingForInterrupt = false;

        // Zapamiętaj bieżący PC (dla pushowania)
        ushort returnPC = _pc;

        // Push PCH i PCL
        Push((byte)(returnPC >> 8));
        Push((byte)(returnPC & 0xFF));

        // Push rejestru P z odpowiednimi flagami
        byte pushedP = _p;
        if (type == InterruptType.BRK)
        {
            pushedP |= FlagB;  // B=1 tylko dla BRK
        }
        else
        {
            pushedP &= unchecked((byte)~FlagB);  // B=0 dla IRQ/NMI
        }
        pushedP |= FlagU;  // bit5=1
        Push(pushedP);

        // Ustaw flagę I (wyłącz dalsze IRQ)
        SetFlag(FlagI, true);

        // Odczytaj wektor przerwania
        ushort vector;
        if (type == InterruptType.NMI)
        {
            vector = 0xFFFA;
        }
        else
        {
            vector = 0xFFFE;
        }

        byte lo = _memory.Read(vector);
        byte hi = _memory.Read((ushort)(vector + 1));
        _pc = (ushort)(hi << 8 | lo);

        // Ustaw sygnalizację pobrania nowego opcode
        _sync = true;
    }

    #endregion
}

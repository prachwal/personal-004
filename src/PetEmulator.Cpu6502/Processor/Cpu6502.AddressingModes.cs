// <copyright file="Cpu6502.AddressingModes.cs" company="6502 Emulator">
// Copyright © 2026 6502 Emulator. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Metody adresowania

    /// <summary>
    /// Immediate addressing mode - returns PC and increments it
    /// </summary>
    /// <returns>Current PC value (address of operand)</returns>
    private ushort AddrImmediate()
    {
        return _pc++;
    }

    /// <summary>
    /// Zero Page addressing mode
    /// </summary>
    /// <returns>Zero page address ($00xx)</returns>
    private ushort AddrZp()
    {
        byte zp = _memory.Read(_pc++);
        return zp;
    }

    /// <summary>
    /// Zero Page,X addressing mode - wraps within zero page
    /// </summary>
    /// <returns>Zero page address with X offset</returns>
    private ushort AddrZpX()
    {
        byte zp = (byte)(_memory.Read(_pc++) + _x);
        return zp;
    }

    /// <summary>
    /// Zero Page,Y addressing mode - wraps within zero page
    /// </summary>
    /// <returns>Zero page address with Y offset</returns>
    private ushort AddrZpY()
    {
        byte zp = (byte)(_memory.Read(_pc++) + _y);
        return zp;
    }

    /// <summary>
    /// Absolute addressing mode
    /// </summary>
    /// <returns>16-bit absolute address</returns>
    private ushort AddrAbs()
    {
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc++);
        return (ushort)(hi << 8 | lo);
    }

    /// <summary>
    /// Absolute,X addressing mode - may cross page boundary
    /// </summary>
    /// <param name="pageCrossed">Output: true if page boundary was crossed</param>
    /// <returns>Absolute address with X offset</returns>
    private ushort AddrAbsX(out bool pageCrossed)
    {
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc++);
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort addr = (ushort)(baseAddr + _x);
        pageCrossed = (addr >> 8) != hi;
        return addr;
    }

    /// <summary>
    /// Absolute,Y addressing mode - may cross page boundary
    /// </summary>
    /// <param name="pageCrossed">Output: true if page boundary was crossed</param>
    /// <returns>Absolute address with Y offset</returns>
    private ushort AddrAbsY(out bool pageCrossed)
    {
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc++);
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort addr = (ushort)(baseAddr + _y);
        pageCrossed = (addr >> 8) != hi;
        return addr;
    }

    /// <summary>
    /// Indirect,X addressing mode - pre-indexed, wraps in zero page
    /// </summary>
    /// <returns>Indirect address from (zp+X)</returns>
    private ushort AddrIndX()
    {
        byte zp = (byte)(_memory.Read(_pc++) + _x);  // wraps in zero page
        byte lo = _memory.Read(zp);
        byte hi = _memory.Read((byte)(zp + 1));    // wraps in zero page
        return (ushort)(hi << 8 | lo);
    }

    /// <summary>
    /// Indirect,Y addressing mode - post-indexed, may cross page boundary
    /// </summary>
    /// <param name="pageCrossed">Output: true if page boundary was crossed</param>
    /// <returns>Indirect address from (zp)+Y</returns>
    private ushort AddrIndY(out bool pageCrossed)
    {
        byte zp = _memory.Read(_pc++);
        byte lo = _memory.Read(zp);
        byte hi = _memory.Read((byte)(zp + 1));
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort addr = (ushort)(baseAddr + _y);
        pageCrossed = (addr >> 8) != hi;
        return addr;
    }

    /// <summary>
    /// Indirect Absolute addressing mode for JMP (6C).
    /// NMOS 6502 bug: when indirect address ends with $xxFF, high byte is read from $xx00 instead of $(xx+1)00.
    /// Ricoh 2A03 (NES) does NOT have this bug.
    /// </summary>
    /// <returns>Indirect absolute address</returns>
    private ushort AddrIndirectAbs()
    {
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc++);
        ushort ptrAddr = (ushort)((hi << 8) | lo);

        byte addrLo = _memory.Read(ptrAddr);

        // NMOS bug: if ptrAddr ends with $xxFF, high byte is read from $xx00 instead of $(xx+1)00
        // Ricoh 2A03 (NES) reads from the correct address (ptrAddr + 1)
        ushort ptrAddrHi = HasJmpIndirectBug && (ptrAddr & 0xFF) == 0xFF
            ? (ushort)(ptrAddr & 0xFF00)  // same page (bug)
            : (ushort)(ptrAddr + 1);     // next address (correct)

        byte addrHi = _memory.Read(ptrAddrHi);

        return (ushort)((addrHi << 8) | addrLo);
    }

    /// <summary>
    /// Zero Page Indirect addressing mode (zp) - 65C02 only.
    /// Reads a pointer from zero page and dereferences it.
    /// Wraps within zero page (zp and zp+1).
    /// No page-cross penalty (wrapping is zero-page local).
    /// </summary>
    /// <returns>Indirect address from (zp)</returns>
    private ushort AddrZpIndirect()
    {
        byte zp = _memory.Read(_pc++);
        byte lo = _memory.Read(zp);
        byte hi = _memory.Read((byte)(zp + 1));  // wraps in zero page
        return (ushort)(hi << 8 | lo);
    }

    /// <summary>
    /// Absolute Indirect,X addressing mode for JMP (abs,X) - 65C02 only.
    /// Reads a 16-bit absolute address from PC, adds X to it (16-bit addition),
    /// then reads the 16-bit target address from that computed location.
    /// Does NOT have the NMOS page-wrap bug (always reads correctly across pages).
    /// </summary>
    /// <returns>Indirect address from (abs+X)</returns>
    private ushort AddrIndirectAbsX()
    {
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc++);
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort ptrAddr = (ushort)(baseAddr + _x);  // 16-bit addition, wraps at 0xFFFF

        byte addrLo = _memory.Read(ptrAddr);
        byte addrHi = _memory.Read((ushort)(ptrAddr + 1));  // normal 16-bit increment, no bug

        return (ushort)((addrHi << 8) | addrLo);
    }

    #endregion
}

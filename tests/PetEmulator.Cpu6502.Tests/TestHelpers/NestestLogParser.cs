using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Parser pliku logu nestest.
/// </summary>
public static class NestestLogParser
{
    /// <summary>
    /// Parsuje plik logu nestest i zwraca listę wpisów.
    /// </summary>
    /// <param name="logPath">Ścieżka do pliku logu.</param>
    /// <returns>Lista wpisów logu.</returns>
    public static List<NestestLogEntry> Parse(string logPath)
    {
        var entries = new List<NestestLogEntry>();
        var lines = File.ReadAllLines(logPath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                continue;

            var entry = ParseLine(line);
            if (entry.HasValue)
                entries.Add(entry.Value);
        }

        return entries;
    }

    private static NestestLogEntry? ParseLine(string line)
    {
        try
        {
            // Format: C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 21 CYC:7
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 6)
                return null;

            // PC (4 hex chars)
            if (!ushort.TryParse(parts[0], NumberStyles.HexNumber, null, out ushort pc))
                return null;

            // Opcode bytes - collect consecutive 2-char hex values
            var opcodeBytes = new List<byte>();
            int i = 1;
            while (i < parts.Length && parts[i].Length == 2 && IsHex(parts[i]))
            {
                if (byte.TryParse(parts[i], NumberStyles.HexNumber, null, out byte b))
                    opcodeBytes.Add(b);
                i++;
            }

            // Instruction - everything until we hit a register label
            var instructionParts = new List<string>();
            while (i < parts.Length && !IsRegisterLabel(parts[i]) && !IsPpuLabel(parts[i]))
            {
                instructionParts.Add(parts[i]);
                i++;
            }
            string instruction = string.Join(" ", instructionParts).Trim();

            // Parse register values
            byte a = ParseRegisterValue(parts, ref i, "A:");
            byte x = ParseRegisterValue(parts, ref i, "X:");
            byte y = ParseRegisterValue(parts, ref i, "Y:");
            byte p = ParseRegisterValue(parts, ref i, "P:");
            byte sp = ParseRegisterValue(parts, ref i, "SP:");

            // Skip PPU info
            while (i < parts.Length && !parts[i].StartsWith("CYC:", StringComparison.Ordinal))
                i++;

            // Cycle
            ulong cycle = 0;
            if (i < parts.Length && parts[i].StartsWith("CYC:", StringComparison.Ordinal))
            {
                var cycleStr = parts[i].Substring(4);
                ulong.TryParse(cycleStr, out cycle);
            }

            return new NestestLogEntry(pc, opcodeBytes.ToArray(), instruction, a, x, y, p, sp, cycle);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsHex(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        return s.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    private static bool IsRegisterLabel(string part)
    {
        // Sprawdź dokładne nazwy rejestrów, nie "PPU:" 
        return part.StartsWith("A:", StringComparison.Ordinal) ||
               part.StartsWith("X:", StringComparison.Ordinal) ||
               part.StartsWith("Y:", StringComparison.Ordinal) ||
               part.StartsWith("P:", StringComparison.Ordinal) ||
               part.StartsWith("SP:", StringComparison.Ordinal) ||
               part.StartsWith("CYC:", StringComparison.Ordinal);
    }

    private static bool IsPpuLabel(string part)
    {
        return part.StartsWith("PPU:", StringComparison.Ordinal);
    }

    private static byte ParseRegisterValue(string[] parts, ref int index, string prefix)
    {
        while (index < parts.Length && !parts[index].StartsWith(prefix, StringComparison.Ordinal))
            index++;

        if (index >= parts.Length)
            return 0;

        var valueStr = parts[index].Substring(prefix.Length);
        if (byte.TryParse(valueStr, NumberStyles.HexNumber, null, out byte result))
            return result;
        return 0;
    }
}

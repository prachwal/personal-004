namespace Cpu6502;

public static class OpcodeTables
{
    public static OpcodeTable CreateNmos() => Nmos;

    public static Cpu6502Variant CreateNmosVariant(OpcodeTable? opcodeTable = null) =>
        new("MOS 6502", (opcodeTable ?? Nmos).Seal(), CpuQuirk.DecimalArithmetic | CpuQuirk.JmpIndirectPageWrap);

    /// <summary>
    /// Determines whether a 6502 opcode incurs a page-cross penalty.
    /// Used during table construction to populate OpcodeDefinition.HasPageCrossPenalty.
    /// </summary>
    private static bool HasPageCrossPenaltyFor(byte opcode)
    {
        return opcode is
            0xBD or 0xB9 or 0xB1 or 0xBE or 0xBC or
            0x7D or 0x79 or 0x71 or
            0xFD or 0xF9 or 0xF1 or
            0xDD or 0xD9 or 0xD1 or
            0x3D or 0x39 or 0x31 or
            0x1D or 0x19 or 0x11 or
            0x5D or 0x59 or 0x51;
    }

    /// <summary>
    /// Determines the base cycle count for a 6502 opcode.
    /// Used during table construction to populate OpcodeDefinition.BaseCycles.
    /// </summary>
    private static byte BaseCyclesFor(byte opcode)
    {
        return opcode switch
        {
            0xA9 => 2, 0xA5 => 3, 0xB5 => 4, 0xAD => 4, 0xBD => 4, 0xB9 => 4, 0xA1 => 6, 0xB1 => 5,
            0x85 => 3, 0x95 => 4, 0x8D => 4, 0x9D => 5, 0x99 => 5, 0x81 => 6, 0x91 => 6,
            0xA2 => 2, 0xA6 => 3, 0xB6 => 4, 0xAE => 4, 0xBE => 4,
            0x86 => 3, 0x96 => 4, 0x8E => 4,
            0xA0 => 2, 0xA4 => 3, 0xB4 => 4, 0xAC => 4, 0xBC => 4,
            0x84 => 3, 0x94 => 4, 0x8C => 4,
            0xAA => 2, 0xA8 => 2, 0xBA => 2, 0x8A => 2, 0x9A => 2, 0x98 => 2,
            0x18 => 2, 0x38 => 2, 0xD8 => 2, 0xF8 => 2, 0x58 => 2, 0x78 => 2, 0xB8 => 2,
            0xEA => 2,
            0x69 => 2, 0x65 => 3, 0x75 => 4, 0x6D => 4, 0x7D => 4, 0x79 => 4, 0x61 => 6, 0x71 => 5,
            0xE9 => 2, 0xE5 => 3, 0xF5 => 4, 0xED => 4, 0xFD => 4, 0xF9 => 4, 0xE1 => 6, 0xF1 => 5,
            0xC9 => 2, 0xC5 => 3, 0xD5 => 4, 0xCD => 4, 0xDD => 4, 0xD9 => 4, 0xC1 => 6, 0xD1 => 5,
            0xE0 => 2, 0xE4 => 3, 0xEC => 4,
            0xC0 => 2, 0xC4 => 3, 0xCC => 4,
            0x29 => 2, 0x25 => 3, 0x35 => 4, 0x2D => 4, 0x3D => 4, 0x39 => 4, 0x21 => 6, 0x31 => 5,
            0x09 => 2, 0x05 => 3, 0x15 => 4, 0x0D => 4, 0x1D => 4, 0x19 => 4, 0x01 => 6, 0x11 => 5,
            0x49 => 2, 0x45 => 3, 0x55 => 4, 0x4D => 4, 0x5D => 4, 0x59 => 4, 0x41 => 6, 0x51 => 5,
            0xE6 => 5, 0xF6 => 6, 0xEE => 6, 0xFE => 7,
            0xC6 => 5, 0xD6 => 6, 0xCE => 6, 0xDE => 7,
            0xE8 => 2, 0xC8 => 2, 0xCA => 2, 0x88 => 2,
            0x0A => 2, 0x06 => 5, 0x16 => 6, 0x0E => 6, 0x1E => 7,
            0x4A => 2, 0x46 => 5, 0x56 => 6, 0x4E => 6, 0x5E => 7,
            0x2A => 2, 0x26 => 5, 0x36 => 6, 0x2E => 6, 0x3E => 7,
            0x6A => 2, 0x66 => 5, 0x76 => 6, 0x6E => 6, 0x7E => 7,
            0x90 => 2, 0xB0 => 2, 0xF0 => 2, 0x30 => 2, 0xD0 => 2, 0x10 => 2, 0x50 => 2, 0x70 => 2,
            0x48 => 3, 0x08 => 3, 0x68 => 4, 0x28 => 4,
            0x4C => 3, 0x6C => 5, 0x20 => 6, 0x60 => 6,
            0x00 => 7, 0x40 => 6,
            0x24 => 3, 0x2C => 4,
            0x0B => 2, 0x2B => 2, 0x4B => 2, 0x6B => 2, 0xCB => 2, 0xBB => 4,
            // Faza 19 - Niestabilne opkody
            0x8B => 2, 0xAB => 2, 0xEB => 2,  // ANE, LXA, USBC - Immediate
            0x83 => 6,  // SAX (ind,X)
            0x9F => 5, 0x93 => 6,  // SHA - abs,Y, ind,Y
            0x9E => 5,  // SHX - abs,Y
            0x9C => 5,  // SHY - abs,X
            0x9B => 5,  // TAS - abs,Y
            // Faza 19 - NOP-y
            0x04 => 3, 0x44 => 3, 0x64 => 3,  // NOP zp
            0x14 => 4, 0x34 => 4, 0x54 => 4, 0x74 => 4, 0xD4 => 4, 0xF4 => 4,  // NOP zp,X
            0x0C => 4,  // NOP abs
            0x1C => 4, 0x3C => 4, 0x5C => 4, 0x7C => 4, 0xDC => 4, 0xFC => 4,  // NOP abs,X
            0x80 => 2, 0x82 => 2, 0x89 => 2, 0xC2 => 2, 0xE2 => 2,  // NOP imm
            0x1A => 2, 0x3A => 2, 0x5A => 2, 0x7A => 2, 0xDA => 2, 0xFA => 2,  // NOP impl
            // Faza 19 - KIL
            0x02 => 1, 0x12 => 1, 0x22 => 1, 0x32 => 1, 0x42 => 1, 0x52 => 1,
            0x62 => 1, 0x72 => 1, 0x92 => 1, 0xB2 => 1, 0xD2 => 1, 0xF2 => 1,
            _ => 2
        };
    }

    public static Cpu6502Variant CreateNesVariant(OpcodeTable? opcodeTable = null) =>
        new("Ricoh 2A03", opcodeTable ?? CreateNmos(), CpuQuirk.None);

    public static Cpu6502Variant CreateCommodore6510Variant(OpcodeTable? opcodeTable = null) =>
        new("Commodore 6510", opcodeTable ?? CreateNmos(), CpuQuirk.DecimalArithmetic | CpuQuirk.JmpIndirectPageWrap);

    public static Cpu6502Variant CreateAtari6507Variant(OpcodeTable? opcodeTable = null) =>
        new("Atari 6507", opcodeTable ?? CreateNmos(), CpuQuirk.DecimalArithmetic | CpuQuirk.JmpIndirectPageWrap);

    public static OpcodeTable CreateCmos65C02Table() =>
        Nmos.Derive(table =>
        {
            table.Set(new OpcodeDefinition(0x04, "TSB", AddressingMode.ZeroPage, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x0C, "TSB", AddressingMode.Absolute, 3, 6, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x12, "ORA", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x14, "TRB", AddressingMode.ZeroPage, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x1A, "INC", AddressingMode.Accumulator, 1, 2, ExecuteAccumulatorStackCycle));
            table.Set(new OpcodeDefinition(0x1C, "TRB", AddressingMode.Absolute, 3, 6, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x32, "AND", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x34, "BIT", AddressingMode.ZeroPageX, 2, 4, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x3A, "DEC", AddressingMode.Accumulator, 1, 2, ExecuteAccumulatorStackCycle));
            table.Set(new OpcodeDefinition(0x3C, "BIT", AddressingMode.AbsoluteX, 3, 4, Execute65C02Cycle, true));
            table.Set(new OpcodeDefinition(0x52, "EOR", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x5A, "PHY", AddressingMode.Implied, 1, 3, ExecuteAccumulatorStackCycle));
            table.Set(new OpcodeDefinition(0x64, "STZ", AddressingMode.ZeroPage, 2, 3, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x72, "ADC", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x74, "STZ", AddressingMode.ZeroPageX, 2, 4, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x7A, "PLY", AddressingMode.Implied, 1, 4, ExecuteAccumulatorStackCycle));
            table.Set(new OpcodeDefinition(0x7C, "JMP", AddressingMode.Indirect, 3, 6, ExecuteControlFlowCycle));
            table.Set(new OpcodeDefinition(0x80, "BRA", AddressingMode.Relative, 2, 2, ExecuteBranchCycle));
            table.Set(new OpcodeDefinition(0x89, "BIT", AddressingMode.Immediate, 2, 2, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x92, "STA", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x9C, "STZ", AddressingMode.Absolute, 3, 4, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0x9E, "STZ", AddressingMode.AbsoluteX, 3, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0xB2, "LDA", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0xD2, "CMP", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0xDA, "PHX", AddressingMode.Implied, 1, 3, ExecuteAccumulatorStackCycle));
            table.Set(new OpcodeDefinition(0xF2, "SBC", AddressingMode.ZeroPageIndirect, 2, 5, Execute65C02Cycle));
            table.Set(new OpcodeDefinition(0xFA, "PLX", AddressingMode.Implied, 1, 4, ExecuteAccumulatorStackCycle));

            // Tranche 2: remap remaining illegal-NMOS opcodes to 65C02 NOPs
            // 59 bytes at Implied, Length 1, Cycles 1
            table.Set(new OpcodeDefinition(0x03, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x07, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x0F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x13, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x17, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x1B, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x1F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x23, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x27, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x2F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x33, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x37, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x3B, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x3F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x43, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x47, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x4F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x53, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x57, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x5B, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x5F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x63, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x67, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x6F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x73, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x77, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x7B, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x7F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x83, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x87, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x8B, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x8F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x93, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x97, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x9B, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x9F, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xA3, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xA7, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xAB, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xAF, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xB3, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xB7, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xBB, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xBF, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xC3, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xC7, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xCF, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xD3, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xD7, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xDB, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xDF, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xE3, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xE7, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xEB, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xEF, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xF3, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xF7, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xFB, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0xFF, "NOP", AddressingMode.Implied, 1, 1, ExecuteCmosNopCycle));

            // 4 bytes at Immediate, Length 2, Cycles 2
            table.Set(new OpcodeDefinition(0x02, "NOP", AddressingMode.Immediate, 2, 2, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x22, "NOP", AddressingMode.Immediate, 2, 2, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x42, "NOP", AddressingMode.Immediate, 2, 2, ExecuteCmosNopCycle));
            table.Set(new OpcodeDefinition(0x62, "NOP", AddressingMode.Immediate, 2, 2, ExecuteCmosNopCycle));

            // 0x5C - reuse Nmos handler with bumped cycle count and mnemonic
            table.Set(Nmos[0x5C] with { Mnemonic = "NOP", BaseCycles = 8 });
        });

    private static void Execute65C02Cycle(Cpu6502 cpu, byte opcode, byte cycle) =>
        cpu.Execute65C02Cycle(opcode, cycle);

    private static void ExecuteCmosNopCycle(Cpu6502 cpu, byte opcode, byte cycle) =>
        cpu.ExecuteCmosNopCycle(opcode, cycle);

    private static void ExecuteControlFlowCycle(Cpu6502 cpu, byte opcode, byte cycle)
    {
        var key = (ushort)((opcode << 3) | cycle);
        if (!cpu.ExecuteCycleControlFlow(key))
            throw new InvalidOperationException($"Unsupported control flow opcode 0x{opcode:X2}.");
    }

    private static void ExecuteBranchCycle(Cpu6502 cpu, byte opcode, byte cycle)
    {
        var key = (ushort)((opcode << 3) | cycle);
        if (!cpu.ExecuteCycleBranches(key))
            throw new InvalidOperationException($"Unsupported branch opcode 0x{opcode:X2}.");
    }

    public static Cpu6502Variant CreateCmos65C02Variant(OpcodeTable? opcodeTable = null) =>
        new("WDC 65C02", (opcodeTable ?? CreateCmos65C02Table()).Seal(), CpuQuirk.DecimalArithmetic | CpuQuirk.CmosBcdExtraCycle);

    private static OpcodeTable CreateNmosTable()
    {
        var table = new OpcodeTable();
        var mnemonics = Mnemonics.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var modes = Modes.Where(char.IsLetterOrDigit).ToArray();
        if (mnemonics.Length != 256)
            throw new InvalidOperationException($"Expected 256 opcode names, got {mnemonics.Length}.");
        if (modes.Length != 256)
            throw new InvalidOperationException($"Expected 256 opcode modes, got {modes.Length}.");
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var value = (byte)opcode;
            var addressingMode = ParseMode(modes[opcode]);
            var definition = new OpcodeDefinition(
                value,
                mnemonics[opcode],
                addressingMode,
                InstructionLength(addressingMode),
                BaseCyclesFor(value),
                ExecuteUnmappedOpcodeCycle,
                HasPageCrossPenaltyFor(value));

            if (SimpleOpcodes.Contains(value))
                definition = definition with { Handler = ExecuteSimpleCycle };
            else if (AccumulatorStackOpcodes.Contains(value))
                definition = definition with { Handler = ExecuteAccumulatorStackCycle };
            else if (LoadStoreOpcodes.Contains(value))
                definition = definition with { Handler = ExecuteLoadStoreCycle };
            else if (ArithmeticCompareLogicOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleArithmeticCompareLogic((ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported arithmetic opcode 0x{opcode:X2}.");
                    }
                };
            else if (BranchOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleBranches((ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported branch opcode 0x{opcode:X2}.");
                    }
                };
            else if (ControlFlowOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleControlFlow((ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported control-flow opcode 0x{opcode:X2}.");
                    }
                };
            else if (RmwOpcodes.Contains(value))
                definition = definition with { Handler = ExecuteRmwOpcodeCycle };
            else if (IllegalRmwOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleIllegalRMW((ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported illegal RMW opcode 0x{opcode:X2}.");
                    }
                };
            else if (NopKilOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleNopKilOpcodes((ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported NOP/KIL opcode 0x{opcode:X2}.");
                    }
                };
            else if (UnstableOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleUnstableOpcodes((ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported unstable opcode 0x{opcode:X2}.");
                    }
                };
            else if (IllegalLoadStoreOpcodes.Contains(value))
                definition = definition with
                {
                    Handler = static (cpu, opcode, cycle) =>
                    {
                        if (!cpu.ExecuteCycleLoadStoreTransferFlags(opcode, cycle, (ushort)((opcode << 3) | cycle)))
                            throw new InvalidOperationException($"Unsupported illegal load/store opcode 0x{opcode:X2}.");
                    }
                };

            table.Set(definition);
        }

        return table;
    }

    private static void ExecuteSimpleCycle(Cpu6502 cpu, byte opcode, byte cycle) =>
        cpu.ExecuteSimpleCycle(opcode, cycle);

    private static void ExecuteAccumulatorStackCycle(Cpu6502 cpu, byte opcode, byte cycle) =>
        cpu.ExecuteAccumulatorStackCycle(opcode, cycle);

    private static void ExecuteLoadStoreCycle(Cpu6502 cpu, byte opcode, byte cycle) =>
        cpu.ExecuteLoadStoreCycle(opcode, cycle);

    private static void ExecuteRmwOpcodeCycle(Cpu6502 cpu, byte opcode, byte cycle)
    {
        var key = (ushort)((opcode << 3) | cycle);
        if (!cpu.ExecuteCycleMathsStackBranches(key))
            throw new InvalidOperationException($"Unsupported RMW opcode 0x{opcode:X2}.");
    }

    private static void ExecuteUnmappedOpcodeCycle(Cpu6502 cpu, byte opcode, byte cycle) =>
        throw new InvalidOperationException($"Unmapped opcode 0x{opcode:X2} at cycle {cycle}.");

    private static AddressingMode ParseMode(char mode) => mode switch
    {
        'I' => AddressingMode.Implied,
        'A' => AddressingMode.Accumulator,
        'M' => AddressingMode.Immediate,
        'Z' => AddressingMode.ZeroPage,
        'x' => AddressingMode.ZeroPageX,
        'y' => AddressingMode.ZeroPageY,
        'W' => AddressingMode.Absolute,
        'X' => AddressingMode.AbsoluteX,
        'Y' => AddressingMode.AbsoluteY,
        'N' => AddressingMode.Indirect,
        'i' => AddressingMode.IndirectX,
        'j' => AddressingMode.IndirectY,
        'r' => AddressingMode.Relative,
        _ => throw new InvalidOperationException($"Unknown addressing mode: {mode}")
    };

    private static byte InstructionLength(AddressingMode mode) => mode switch
    {
        AddressingMode.Implied or AddressingMode.Accumulator => 1,
        AddressingMode.Immediate or AddressingMode.ZeroPage or AddressingMode.ZeroPageX or
        AddressingMode.ZeroPageY or AddressingMode.ZeroPageIndirect or AddressingMode.IndirectX or
        AddressingMode.IndirectY or AddressingMode.Relative => 2,
        _ => 3
    };

    private static readonly byte[] SimpleOpcodes =
    [
        0xEA, 0xAA, 0xA8, 0xBA, 0x8A, 0x9A, 0x98,
        0xE8, 0xC8, 0xCA, 0x88,
        0x18, 0x38, 0xD8, 0xF8, 0x58, 0x78, 0xB8
    ];

    private static readonly byte[] AccumulatorStackOpcodes =
    [0x0A, 0x4A, 0x2A, 0x6A, 0x48, 0x08, 0x68, 0x28];

    private static readonly byte[] LoadStoreOpcodes =
    [
        0xA9, 0xA5, 0xB5, 0xAD, 0xBD, 0xB9, 0xA1, 0xB1,
        0x85, 0x95, 0x8D, 0x9D, 0x99, 0x81, 0x91,
        0xA2, 0xA6, 0xB6, 0xAE, 0xBE, 0x86, 0x96, 0x8E,
        0xA0, 0xA4, 0xB4, 0xAC, 0xBC, 0x84, 0x94, 0x8C
    ];

    private static readonly byte[] ArithmeticCompareLogicOpcodes =
    [
        0x69, 0x65, 0x75, 0x6D, 0x7D, 0x79, 0x61, 0x71,
        0xE9, 0xE5, 0xF5, 0xED, 0xFD, 0xF9, 0xE1, 0xF1,
        0xC9, 0xC5, 0xD5, 0xCD, 0xDD, 0xD9, 0xC1, 0xD1,
        0xE0, 0xE4, 0xEC, 0xC0, 0xC4, 0xCC,
        0x29, 0x25, 0x35, 0x2D, 0x3D, 0x39, 0x21, 0x31,
        0x09, 0x05, 0x15, 0x0D, 0x1D, 0x19, 0x01, 0x11,
        0x49, 0x45, 0x55, 0x4D, 0x5D, 0x59, 0x41, 0x51,
        0x0B, 0x2B, 0x4B, 0x6B, 0xCB
    ];

    private static readonly byte[] BranchOpcodes =
    [0x90, 0xB0, 0xF0, 0x30, 0xD0, 0x10, 0x50, 0x70];

    private static readonly byte[] ControlFlowOpcodes =
    [0x00, 0x40, 0x4C, 0x6C, 0x20, 0x60, 0x24, 0x2C];

    private static readonly byte[] RmwOpcodes =
    [
        0x06, 0x16, 0x0E, 0x1E, 0x46, 0x56, 0x4E, 0x5E,
        0x26, 0x36, 0x2E, 0x3E, 0x66, 0x76, 0x6E, 0x7E,
        0xE6, 0xF6, 0xEE, 0xFE, 0xC6, 0xD6, 0xCE, 0xDE
    ];

    private static readonly byte[] IllegalRmwOpcodes =
    [
        0xC7, 0xD7, 0xCF, 0xDF, 0xDB, 0xC3, 0xD3,
        0xE7, 0xF7, 0xEF, 0xFF, 0xFB, 0xE3, 0xF3,
        0x27, 0x37, 0x2F, 0x3F, 0x3B, 0x23, 0x33,
        0x67, 0x77, 0x6F, 0x7F, 0x7B, 0x63, 0x73,
        0x07, 0x17, 0x0F, 0x1F, 0x1B, 0x03, 0x13,
        0x47, 0x57, 0x4F, 0x5F, 0x5B, 0x43, 0x53
    ];

    private static readonly byte[] NopKilOpcodes =
    [
        0x04, 0x44, 0x64, 0x14, 0x34, 0x54, 0x74, 0xD4, 0xF4,
        0x0C, 0x1C, 0x3C, 0x5C, 0x7C, 0xDC, 0xFC,
        0x80, 0x82, 0x89, 0xC2, 0xE2, 0x1A, 0x3A, 0x5A, 0x7A, 0xDA, 0xFA,
        0x02, 0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72, 0x92, 0xB2, 0xD2, 0xF2
    ];

    private static readonly byte[] UnstableOpcodes =
    [0x8B, 0xAB, 0xEB, 0x9F, 0x93, 0x9E, 0x9C, 0x9B];

    private static readonly byte[] IllegalLoadStoreOpcodes =
    [0xBB, 0xA7, 0xB7, 0xAF, 0xBF, 0xA3, 0xB3, 0x87, 0x97, 0x8F, 0x83];

    public static OpcodeTable Nmos { get; } = CreateNmosTable();

    private const string Mnemonics = """
        BRK ORA KIL SLO NOP ORA ASL SLO PHP ORA ASL ANC NOP ORA ASL SLO
        BPL ORA KIL SLO NOP ORA ASL SLO CLC ORA NOP SLO NOP ORA ASL SLO
        JSR AND KIL RLA BIT AND ROL RLA PLP AND ROL ANC BIT AND ROL RLA
        BMI AND KIL RLA NOP AND ROL RLA SEC AND NOP RLA NOP AND ROL RLA
        RTI EOR KIL SRE NOP EOR LSR SRE PHA EOR LSR ALR JMP EOR LSR SRE
        BVC EOR KIL SRE NOP EOR LSR SRE CLI EOR NOP SRE NOP EOR LSR SRE
        RTS ADC KIL RRA NOP ADC ROR RRA PLA ADC ROR ARR JMP ADC ROR RRA
        BVS ADC KIL RRA NOP ADC ROR RRA SEI ADC NOP RRA NOP ADC ROR RRA
        NOP STA KIL SAX STY STA STX SAX DEY NOP TXA XAA STY STA STX SAX
        BCC STA KIL SHA STY STA STX SAX TYA STA TXS TAS SHY STA SHX SHA
        LDY LDA LDX LAX LDY LDA LDX LAX TAY LDA TAX LXA LDY LDA LDX LAX
        BCS LDA KIL LAX LDY LDA LDX LAX CLV LDA TSX LAS LDY LDA LDX LAX
        CPY CMP NOP DCP CPY CMP DEC DCP INY CMP DEX AXS CPY CMP DEC DCP
        BNE CMP KIL DCP NOP CMP DEC DCP CLD CMP NOP DCP NOP CMP DEC DCP
        CPX SBC NOP ISC CPX SBC INC ISC INX SBC NOP USBC CPX SBC INC ISC
        BEQ SBC KIL ISC NOP SBC INC ISC SED SBC NOP ISC NOP SBC INC ISC
        """;

    private const string Modes = """
        IiIiZZZZIMAMWWWW
        rjIjxxxxIYIYXXXX
        WiIiZZZZIMAMWWWW
        rjIjxxxxIYIYXXXX
        IiIiZZZZIMAMWWWW
        rjIjxxxxIYIYXXXX
        IiIiZZZZIMAMNWWW
        rjIjxxxxIYIYXXXX
        IiIiZZZZIMAMWWWW
        rjIjxxxxIYIYXXXX
        MiMiZZZZIMIMWWWW
        rjIjxxxxIYIYXXXX
        IiIiZZZZIMAMWWWW
        rjIjxxxxIYIYXXXX
        MiMiZZZZIMIMWWWW
        rjIjxxxxIYIYXXXX
        """;
}

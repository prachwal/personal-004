using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Disassembly;
using PetEmulator.Kaypro;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproBootTests
{
    [Test]
    [Category("BinaryBoot")]
    public void MonitorRomAndMatchingCpMImageReachAsteriskOrPromptOutput()
    {
        var root = FindRepositoryRoot();
        var cycles = new BootCycleTrace();
        var machine = new KayproMachine(cycles);
        machine.LoadMonitorRom(File.ReadAllBytes(Path.Combine(root, "roms", "kaypro", "kaypro-81-149c.bin")));
        machine.InsertDisk(0, new KayproDiskImage(
            File.ReadAllBytes(Path.Combine(root, "roms", "kaypro", "cpm22-rom149.dsk")),
            firstSectorId: 0));

        var output = new List<byte>();
        var commands = new List<string>();
        var instructions = new List<string>();
        var transferInstructions = new List<string>();
        var nmiInstructions = new List<string>();
        var finalDataReads = new List<string>();
        var lastByteTrace = new List<string>();
        var tracingLastByte = false;
        ushort hookPc = 0;
        byte hookB = 0;
        var fdcEvents = new List<FD1791DiagnosticEvent>();
        var dataRequests = 0;
        var dataReads = 0;
        var lastCommand = machine.Bus.Fdc.PendingCommand;
        machine.Bus.Fdc.DiagnosticObserver = diagnostic =>
        {
            if (diagnostic.Kind == FD1791DiagnosticKind.DataRequested)
            {
                dataRequests++;
                if (diagnostic.TransferIndex == 511)
                {
                    tracingLastByte = true;
                    lastByteTrace.Clear();
                }
            }
            if (diagnostic.Kind == FD1791DiagnosticKind.DataRead)
            {
                dataReads++;
                if (diagnostic.TransferIndex >= 508)
                {
                    if (finalDataReads.Count >= 64)
                        finalDataReads.RemoveAt(0);
                    finalDataReads.Add($"I={diagnostic.TransferIndex} V={diagnostic.Value:X2} PC={hookPc:X4} B={hookB:X2}");
                }
            }
            if (fdcEvents.Count >= 256)
                fdcEvents.RemoveAt(0);
            fdcEvents.Add(diagnostic);
        };
        machine.Cpu.Hooks.Add(new CpuHook(cpu =>
        {
            hookPc = cpu.Registers.PC;
            hookB = cpu.Registers.B;
            if (tracingLastByte && lastByteTrace.Count < 48)
            {
                var traceDecoded = Z80Disassembler.Disassemble(machine.Memory.Read, cpu.Registers.PC);
                lastByteTrace.Add($"PC={cpu.Registers.PC:X4} B={cpu.Registers.B:X2} H={(machine.Cpu.Halted ? 1 : 0)} " +
                    $"P={(machine.Bus.FdcNmiPending ? 1 : 0)} D={(machine.Bus.Fdc.DrqAsserted ? 1 : 0)} {traceDecoded.Text}");
            }
            if (cpu.Registers.PC == 0x0066 || cpu.Registers.PC >= 0xFF00 ||
                cpu.Registers.PC is >= 0x0431 and <= 0x046C)
            {
                var decoded = Z80Disassembler.Disassemble(machine.Memory.Read, cpu.Registers.PC);
                if (instructions.Count >= 96)
                    instructions.RemoveAt(0);
                instructions.Add($"PC={cpu.Registers.PC:X4} B={cpu.Registers.B:X2} NMI={machine.Bus.InterruptLines.NmiAsserted} {decoded.Text}");
            }
            if (machine.Bus.Fdc.DrqAsserted && cpu.Registers.PC >= 0xFF00)
            {
                if (transferInstructions.Count < 120)
                {
                    var transferDecoded = Z80Disassembler.Disassemble(machine.Memory.Read, cpu.Registers.PC);
                    transferInstructions.Add($"PC={cpu.Registers.PC:X4} B={cpu.Registers.B:X2} HALT={machine.Cpu.Halted} " +
                        $"DRQ={(machine.Bus.Fdc.DrqAsserted ? 1 : 0)} " +
                        $"PEND={(machine.Bus.FdcNmiPending ? 1 : 0)} ST={machine.Bus.Fdc.Status:X2} {transferDecoded.Text}");
                }
            }
            if (cpu.Registers.PC == 0x0066)
            {
                if (nmiInstructions.Count >= 32)
                    nmiInstructions.RemoveAt(0);
                nmiInstructions.Add($"B={cpu.Registers.B:X2} HALT={machine.Cpu.Halted} DRQ={(machine.Bus.Fdc.DrqAsserted ? 1 : 0)} PEND={(machine.Bus.FdcNmiPending ? 1 : 0)}");
            }
            if (machine.Bus.Fdc.PendingCommand != lastCommand)
            {
                lastCommand = machine.Bus.Fdc.PendingCommand;
                commands.Add($"PC={cpu.Registers.PC:X4}/CMD={lastCommand:X2}/TR={machine.Bus.Fdc.Track}/SE={machine.Bus.Fdc.Sector}/ST={machine.Bus.Fdc.Status:X2}");
            }
        }));
        machine.Bus.Sio.Transmitted += value => output.Add(value);
        machine.Reset();

        for (var batch = 0; batch < 20 && !ContainsPrompt(output) && !machine.Video.GetText().Contains("A>"); batch++)
            machine.Run(100_000);

        Assert.That(ContainsPrompt(output) || machine.Video.GetText().Contains("A>"), Is.True,
            $"ROM boot did not produce a CP/M prompt. Output={ToDisplay(output)}, Video={machine.Video.GetText()[..Math.Min(160, machine.Video.GetText().Length)]}, PC={machine.Cpu.Registers.PC:X4}, " +
            $"AF={machine.Cpu.Registers.AF:X4}, BC={machine.Cpu.Registers.BC:X4}, DE={machine.Cpu.Registers.DE:X4}, " +
            $"HL={machine.Cpu.Registers.HL:X4}, SP={machine.Cpu.Registers.SP:X4}, " +
            $"FdcStatus={machine.Bus.Fdc.Status:X2}, Track={machine.Bus.Fdc.Track}, Sector={machine.Bus.Fdc.Sector}, " +
            $"Data={machine.Bus.Fdc.Data:X2}, SystemPort={machine.Bus.SystemPortValue:X2}, " +
            $"Seq={machine.Bus.Fdc.InterruptSequence}, DRQ={machine.Bus.Fdc.DrqAsserted}, Deadline={machine.Bus.Fdc.DataDeadlineTStatesRemaining}, " +
            $"FA00={Convert.ToHexString(Enumerable.Range(0, 32).Select(offset => machine.Memory.Read((ushort)(0xFA00 + offset))).ToArray())}, " +
            $"FC00={Convert.ToHexString(Enumerable.Range(0, 32).Select(offset => machine.Memory.Read((ushort)(0xFC00 + offset))).ToArray())}, " +
            $"VideoRaw={Convert.ToHexString(Enumerable.Range(0, 16).Select(offset => machine.Memory.Read((ushort)(0x3000 + offset))).ToArray())}, " +
            $"Commands={string.Join('|', commands.Take(16))}, " +
            $"FdcEvents={string.Join('|', fdcEvents.Select(FormatFdcEvent))}, " +
            $"DataRequests={dataRequests},DataReads={dataReads},NmiPulses={machine.Bus.FdcNmiPulseCount},NmiPending={machine.Bus.FdcNmiPending}, " +
            $"Bus={cycles.Summary()}, Instructions={string.Join('|', instructions.TakeLast(64))}, " +
            $"TransferInstructions={string.Join('|', transferInstructions.TakeLast(96))}, " +
            $"NmiInstructions={string.Join('|', nmiInstructions.TakeLast(32))}, " +
            $"FinalDataReads={string.Join('|', finalDataReads.TakeLast(64))}, " +
            $"LastByteTrace={string.Join('|', lastByteTrace)}");
    }

    private static bool ContainsPrompt(IReadOnlyList<byte> output)
    {
        for (var index = 1; index < output.Count; index++)
            if (output[index - 1] == (byte)'A' && output[index] == (byte)'>')
                return true;
        return false;
    }

    private static string ToDisplay(IEnumerable<byte> bytes)
        => new(bytes.Select(value => value is >= 0x20 and <= 0x7E ? (char)value : '.').ToArray());

    private static string FormatFdcEvent(FD1791DiagnosticEvent diagnostic)
        => $"{diagnostic.Kind}@T{diagnostic.TStates}/R{diagnostic.Register:X2}/V{diagnostic.Value:X2}" +
           $"/C{diagnostic.Command:X2}/S{diagnostic.Status:X2}/TR{diagnostic.Track}/SE{diagnostic.Sector}" +
           $"/I{diagnostic.TransferIndex}/D{diagnostic.DataDeadlineTStates}/Q{(diagnostic.DrqAsserted ? 1 : 0)}";

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed class BootCycleTrace : IBusCycleObserver
    {
        private readonly Dictionary<BusCycleKind, int> _counts = new();
        private readonly List<string> _interesting = new();

        public void Observe(BusCycle cycle)
        {
            _counts[cycle.Kind] = _counts.GetValueOrDefault(cycle.Kind) + 1;
            if (_interesting.Count >= 160)
                return;

            var port = (byte)cycle.Address;
            if (cycle.Kind == BusCycleKind.IoWrite &&
                (port is >= 0x10 and <= 0x13 || port == 0x1C) ||
                cycle.Kind == BusCycleKind.MemoryWrite &&
                (cycle.Address is >= 0xFA00 and < 0xFA20 || cycle.Address is >= 0x3000 and < 0x3020))
                _interesting.Add($"{cycle.Kind}:{cycle.Address:X4}={cycle.Value:X2}");
        }

        public string Summary()
            => string.Join(',', _counts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")) +
               ";interesting=" + string.Join('|', _interesting);
    }
}

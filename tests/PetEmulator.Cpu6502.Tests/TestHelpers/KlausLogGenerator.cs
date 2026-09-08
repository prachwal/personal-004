using System;
using System.IO;
using System.Text;
using Cpu6502;
using Cpu6502.Variants;

namespace Cpu6502.Tests.TestHelpers;

public class KlausLogGenerator
{
    private readonly Cpu6502 _cpu;
    private readonly IMemoryBus _memory;
    private readonly StringBuilder _logBuilder = new();
    private const ulong MaxCycles = 100_000_000;
    private ushort _lastPc;
    private int _repeatCount;

    public KlausLogGenerator()
    {
        _memory = new FlatMemory();
        _cpu = new Cpu6502Classic(_memory);
    }
    public KlausLogGenerator(Cpu6502 cpu, IMemoryBus memory)
    {
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    public void GenerateNonBcdLog(string outputPath, int maxLines = 0)
        => GenerateLog(outputPath, "Klaus Dormann Non-BCD", false, maxLines);
    public void GenerateBcdLog(string outputPath, int maxLines = 0)
        => GenerateLog(outputPath, "Klaus Dormann BCD", true, maxLines);

    private void GenerateLog(string outputPath, string testName, bool testBcd, int maxLines)
    {
        _logBuilder.Clear();
        LoadTestRom();
        SetupCpu(testBcd);
        var state = _cpu.GetState();
        ulong initialCycles = state.Cycle;
        _lastPc = state.PC;
        _repeatCount = 0;
        _logBuilder.AppendLine($"; {testName} Trace Log");
        _logBuilder.AppendLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _logBuilder.AppendLine($"; CPU Variant: {_cpu.GetType().Name}");
        _logBuilder.AppendLine($"; BCD Mode: {testBcd}");
        _logBuilder.AppendLine();
        int lineCount = 0;
        while (state.Cycle - initialCycles < MaxCycles)
        {
            LogState();
            lineCount++;
            if (maxLines > 0 && lineCount >= maxLines) break;
            _cpu.StepInstruction();
            state = _cpu.GetState();
            if (state.PC == _lastPc) _repeatCount++; else { _lastPc = state.PC; _repeatCount = 0; }
            if (_repeatCount > 100) { _logBuilder.AppendLine(); _logBuilder.AppendLine($"; --- Test ended: CPU in loop at PC=${state.PC:X4} ---"); break; }
        }
        File.WriteAllText(outputPath, _logBuilder.ToString());
    }
    private void LoadTestRom()
    {
        var romPath = Path.Combine("Data", "6502_functional_test.bin");
        if (!File.Exists(romPath)) throw new FileNotFoundException("ROM not found: " + romPath);
        var romData = File.ReadAllBytes(romPath);
        for (int i = 0; i < Math.Min(romData.Length, 65536); i++) _memory.Write((ushort)i, romData[i]);
    }
    private void SetupCpu(bool testBcd)
    {
        _memory.Write(0xFFFC, 0x00); _memory.Write(0xFFFD, 0x04); _cpu.Reset(); _cpu.SetFlag(Cpu6502.FlagD, testBcd);
    }
    private void LogState()
    {
        var state = _cpu.GetState();
        ushort pc = state.PC;
        byte b1 = _memory.Read(pc), b2 = _memory.Read((ushort)(pc + 1)), b3 = _memory.Read((ushort)(pc + 2));
        _logBuilder.AppendLine($"{pc:X4}  {b1:X2} {b2:X2} {b3:X2} A:{state.A:X2} X:{state.X:X2} Y:{state.Y:X2} P:{state.P:X2} SP:{state.SP:X2} CYC:{state.Cycle}");
    }
}

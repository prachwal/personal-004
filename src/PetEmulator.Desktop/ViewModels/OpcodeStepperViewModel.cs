using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Core;
using PetEmulator.Cpu6502;
using PetEmulator.Cpu6502.Variants;
using PetEmulator.Desktop.Infrastructure;
using CpuCore = PetEmulator.Cpu6502.Cpu6502;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class OpcodeStepperViewModel : ObservableObject, IShellModule
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private FlatMemoryBus _memory = null!;
    private CpuCore _cpu = null!;

    [ObservableProperty]
    private CpuVariantEntry? _selectedVariant;

    public OpcodeStepperViewModel()
    {
        Variants =
        [
            new("MOS 6502 Classic", (memory) => new Cpu6502Classic(memory)),
            new("WDC 65C02", (memory) => new Cpu6502Cmos65C02(memory)),
            new("WDC/Rockwell R65C02S", (memory) => new Cpu6502WdcR65C02S(memory)),
            new("MOS 6510", (memory) => new Cpu6502Commodore6510(memory)),
            new("Ricoh 2A03 / NES", (memory) => new Cpu6502Nes(memory)),
            new("MOS 6507 / Atari", (memory) => new Cpu6502Atari6507(memory)),
        ];
        Registers = [];
        MemoryDiff = [];
        _timer.Tick += (_, _) => Step();
        SelectedVariant = Variants[0];
    }

    public string WindowTitle => "CPU Opcode Stepper";
    public IReadOnlyList<StatusField> StatusFields => SelectedVariant is null ? [new("Status", "Select a CPU")] :
        [new("CPU", SelectedVariant.Name), new("PC", $"0x{_cpu.PC:X4}"), new("Instructions", $"{_cpu.InstructionCount}")];
    public IReadOnlyList<CpuVariantEntry> Variants { get; }
    public ObservableCollection<CpuRegisterRow> Registers { get; }
    public ObservableCollection<MemoryDiffRow> MemoryDiff { get; }
    public bool IsRunning => _timer.IsEnabled;

    partial void OnSelectedVariantChanged(CpuVariantEntry? value)
    {
        if (value is null) return;
        _timer.Stop();
        _memory = new FlatMemoryBus(ushort.MaxValue + 1);
        _cpu = value.Create(_memory);
        ResetProgram();
        Refresh();
    }

    [RelayCommand]
    private void Run()
    {
        _timer.Start();
        OnPropertyChanged(nameof(IsRunning));
    }

    [RelayCommand]
    private void Pause()
    {
        _timer.Stop();
        OnPropertyChanged(nameof(IsRunning));
    }

    [RelayCommand]
    private void Step()
    {
        if (_cpu is null) return;
        var before = SnapshotMemory();
        _cpu.StepInstruction();
        MemoryDiff.Clear();
        for (var address = 0; address < ushort.MaxValue; address++)
        {
            var oldValue = before[address];
            var newValue = _memory.Read((ushort)address);
            if (oldValue != newValue)
                MemoryDiff.Add(new MemoryDiffRow((ushort)address, oldValue, newValue));
        }
        Refresh();
    }

    [RelayCommand]
    private void Reset()
    {
        _timer.Stop();
        ResetProgram();
        MemoryDiff.Clear();
        Refresh();
        OnPropertyChanged(nameof(IsRunning));
    }

    private void ResetProgram()
    {
        var program = new byte[] { 0xA9, 0x10, 0x69, 0x05, 0x85, 0x10, 0xE8, 0xA2, 0x03, 0xE0, 0x03, 0xD0, 0x01, 0xEA };
        for (var i = 0; i < program.Length; i++)
            _memory.Write((ushort)(0x8000 + i), program[i]);
        _memory.Write(0xFFFC, 0x00);
        _memory.Write(0xFFFD, 0x80);
        _cpu.Reset();
    }

    private byte[] SnapshotMemory()
    {
        var snapshot = new byte[ushort.MaxValue];
        for (var address = 0; address < snapshot.Length; address++)
            snapshot[address] = _memory.Read((ushort)address);
        return snapshot;
    }

    private void Refresh()
    {
        Registers.Clear();
        foreach (var register in ((IDebuggableProcessor)_cpu).GetRegisters())
            Registers.Add(new CpuRegisterRow(register.Key, register.Value));
        OnPropertyChanged(nameof(StatusFields));
    }

    public void Dispose() => _timer.Stop();
}

public sealed record CpuVariantEntry(string Name, Func<FlatMemoryBus, CpuCore> Create);
public sealed record CpuRegisterRow(string Name, ulong Value);
public sealed record MemoryDiffRow(ushort Address, byte Before, byte After);

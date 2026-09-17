using PetEmulator.Chips;
using PetEmulator.Audio;
using PetEmulator.Core;
using PetEmulator.Desktop.Infrastructure;
using PetEmulator.Desktop.Resources;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Vic20.Display;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mos6560DebugSession : ChipDebugSessionBase
{
    private readonly MOS6560 _chip;
    private readonly VicPreviewViewModel _preview;
    private readonly IAudioOutput _audioOutput;
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("ChipTester");

    public Mos6560DebugSession(string romsRoot)
        : this(CreateState(romsRoot)) { }

    private Mos6560DebugSession(VicState state)
        : base("MOS 6560 VIC", ChipDescriptions.Get("Mos6560"), state.Definition)
    {
        _chip = state.Chip;
        _preview = state.Preview;
        _audioOutput = CreateAudioOutput();
        _preview.Refresh();
    }

    public override object? Visual => _preview;
    protected override int TimerCyclesPerTick => (int)Math.Round(MOS6560.Phi2Ntsc * 0.020);
    protected override void AfterRefresh() => _preview?.Refresh();
    public override void Dispose()
    {
        base.Dispose();
        try
        {
            _audioOutput.Dispose();
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "MOS6560 audio output dispose failed.");
        }
    }

    /// <summary>Platform audio backend with silent fallback - no backend exists for
    /// Windows/macOS yet, and without the fallback the whole Chip Tester died on
    /// construction there (see Vic20MachineViewModel for the same pattern).</summary>
    private IAudioOutput CreateAudioOutput()
    {
        try
        {
            var output = AudioOutputFactory.CreateDefault();
            output.Start(_chip);
            Log.LogInformation("MOS6560 audio backend started: {Backend}.", output.GetType().Name);
            return output;
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex,
                "No platform audio backend ({ErrorType}: {Message}); continuing silent.", ex.GetType().Name, ex.Message);
            var silent = AudioOutputFactory.CreateNull();
            silent.Start(_chip);
            return silent;
        }
    }

    private static VicState CreateState(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var chip = new MOS6560("Chip Tester VIC");
        var memory = new FlatMemoryBus(ushort.MaxValue + 1);
        var preview = new VicPreviewViewModel(new Vic20RasterDisplay(memory, chip), memory);
        chip.Reset();
        chip.Write(2, 22);
        chip.Write(3, 46);
        chip.Write(5, 0x04);
        chip.Write(0x0E, 0x2F);
        chip.Write(0x0F, 0x1E);
        chip.Write(0x0A, 0xFF);
        chip.Write(0x0B, 0xFF);
        chip.Write(0x0C, 0xFF);
        chip.Write(0x0D, 0xFF);
        FillPattern(memory, chip, Path.Combine(romsRoot, "vic20", "vic20-chargen.bin"));

        return new VicState(chip, new ChipDebugDefinition(
            () => chip.Tick(1), () => { chip.Reset(); FillPattern(memory, chip, Path.Combine(romsRoot, "vic20", "vic20-chargen.bin")); },
            name => chip.Read(ParseRegister(name)),
            (name, value) => chip.Write(ParseRegister(name), value),
            Enumerable.Range(0, MOS6560.RegisterCount)
                .Select(index => (Name: $"R{index:X1}", Read: (Func<byte>)(() => chip.Read((ushort)index))))
                .ToArray(), []), preview);
    }

    private static void FillPattern(FlatMemoryBus memory, MOS6560 chip, string chargenPath)
    {
        const ushort screen = 0x8000;
        const ushort characters = 0x9000;
        const ushort colors = 0x9400;
        const string message = "CHIP TESTER VIC IMAGE + SOUND";
        var font = File.ReadAllBytes(chargenPath);
        for (var index = 0; index < 256; index++)
            for (var row = 0; row < 8; row++)
                memory.Write((ushort)(characters + index * 8 + row),
                    index * 8 + row < font.Length ? font[index * 8 + row] : (byte)0);

        for (var row = 0; row < chip.Rows; row++)
            for (var col = 0; col < chip.Columns; col++)
            {
                var position = row * chip.Columns + col;
                var code = position >= 2 && position - 2 < message.Length
                    ? ToVicScreenCode(message[position - 2])
                    : (byte)((row + col) % 32 + 1);
                memory.Write((ushort)(screen + position), code);
                memory.Write((ushort)(colors + position), (byte)((row + col) % 16));
            }
    }

    private static byte ToVicScreenCode(char value) => value is >= 'A' and <= 'Z'
        ? (byte)(value - 'A' + 1)
        : value == '+' ? (byte)0x2B : (byte)0x20;

    private static ushort ParseRegister(string name) => Convert.ToUInt16(name.TrimStart('R'), 16);

    private sealed record VicState(MOS6560 Chip, ChipDebugDefinition Definition, VicPreviewViewModel Preview);
}

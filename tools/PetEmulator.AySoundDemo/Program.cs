using System.Buffers.Binary;
using System.Text.Json;
using PetEmulator.Audio;
using PetEmulator.Chips;

namespace PetEmulator.AySoundDemo;

internal static class Program
{
    private const int SampleRate = 44_100;
    private const double Clock = 1_000_000;
    private const string DefaultOutput = "build/ay38910-capability-demo.wav";
    private const string DefaultManifest = "build/ay38910-capability-demo.json";

    private sealed record Section(string Name, double Seconds, Action<Ay38910> Configure, double? ExpectedHz = null);

    public static int Main(string[] args)
    {
        var outputPath = args.Length > 0 ? args[0] : DefaultOutput;
        var manifestPath = args.Length > 1 ? args[1] : DefaultManifest;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);

        var sections = CreateSections();
        var totalFrames = sections.Sum(section => checked((int)Math.Round(section.Seconds * SampleRate)));
        var chip = new Ay38910 { Clock = Clock, SampleRate = SampleRate };

        using (var stream = File.Create(outputPath))
        using (var writer = new BinaryWriter(stream))
        {
            WriteWaveHeader(writer, totalFrames);
            var frameOffset = 0;
            foreach (var section in sections)
            {
                chip.Reset();
                section.Configure(chip);
                var frameCount = checked((int)Math.Round(section.Seconds * SampleRate));
                Console.WriteLine($"SECTION {section.Name}: start_frame={frameOffset} frames={frameCount} seconds={section.Seconds:0.###} expected_hz={(section.ExpectedHz?.ToString("0.###") ?? "n/a")}");
                Console.WriteLine($"  registers_written: {string.Join(" ", chip.Registers.Select((value, register) => $"R{register}=0x{value:X2}"))}");
                Console.WriteLine($"  R7=0x{chip.Registers[7]:X2} R8=0x{chip.Registers[8]:X2} R9=0x{chip.Registers[9]:X2} R10=0x{chip.Registers[10]:X2} tone_hz=[{chip.ToneHz(0):0.###}, {chip.ToneHz(1):0.###}, {chip.ToneHz(2):0.###}] noise_hz={chip.NoiseHz:0.###} envelope_hz={chip.EnvelopeHz:0.###}");
                WriteRenderedFrames(writer, chip, frameCount);
                frameOffset += frameCount;
            }
        }

        var manifest = sections.Select((section, index) => new
        {
            section.Name,
            StartFrame = sections.Take(index).Sum(item => checked((int)Math.Round(item.Seconds * SampleRate))),
            Frames = checked((int)Math.Round(section.Seconds * SampleRate)),
            section.ExpectedHz,
        });
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(new { sample_rate = SampleRate, channels = 2, sections = manifest }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"WROTE {outputPath} ({totalFrames} frames, 16-bit PCM stereo)");
        Console.WriteLine($"WROTE {manifestPath}");
        return 0;
    }

    private static List<Section> CreateSections()
    {
        var sections = new List<Section>();
        AddTone(sections, "tone-a4", 440, 1.0);
        AddTone(sections, "tone-csharp5", 554.37, 1.0);
        AddTone(sections, "tone-e5", 659.25, 1.0);
        sections.Add(new Section("tone-chord-a4-csharp5-e5", 1.0, chip =>
        {
            SetTone(chip, 0, 440); SetTone(chip, 1, 554.37); SetTone(chip, 2, 659.25);
            Write(chip, 7, 0x38); Write(chip, 8, 0x0F); Write(chip, 9, 0x0F); Write(chip, 10, 0x0F);
        }));

        foreach (var period in new[] { 1, 8, 31 })
        {
            sections.Add(new Section($"noise-period-{period}", 0.6, chip =>
            {
                Write(chip, 6, period); Write(chip, 7, 0x37); Write(chip, 8, 0x0F); Write(chip, 9, 0); Write(chip, 10, 0);
            }));
        }

        foreach (var shape in new[] { 0x00, 0x04, 0x08, 0x0A, 0x0C, 0x0E })
        {
            sections.Add(new Section($"envelope-0x{shape:X2}", 1.6, chip =>
            {
                SetTone(chip, 0, 440); Write(chip, 7, 0x3E); Write(chip, 8, 0x10); Write(chip, 11, 244); Write(chip, 12, 0); Write(chip, 13, shape);
            }));
        }

        sections.Add(new Section("mixer-tone-only", 0.7, chip =>
        {
            SetTone(chip, 0, 440); Write(chip, 7, 0x3E); Write(chip, 8, 0x0F);
        }));
        sections.Add(new Section("mixer-noise-only", 0.7, chip =>
        {
            Write(chip, 6, 8); Write(chip, 7, 0x37); Write(chip, 8, 0x0F);
        }));
        sections.Add(new Section("mixer-tone-plus-noise", 0.7, chip =>
        {
            SetTone(chip, 0, 440); Write(chip, 6, 8); Write(chip, 7, 0x36); Write(chip, 8, 0x0F);
        }));
        sections.Add(new Section("mixer-all-disabled-silent", 0.5, chip =>
        {
            Write(chip, 7, 0xFF); Write(chip, 8, 0); Write(chip, 9, 0); Write(chip, 10, 0);
        }));
        return sections;
    }

    private static void AddTone(List<Section> sections, string name, double targetHz, double seconds) =>
        sections.Add(new Section(name, seconds, chip =>
        {
            SetTone(chip, 0, targetHz); Write(chip, 7, 0x3E); Write(chip, 8, 0x0F);
        }, ToneFrequency(targetHz)));

    private static double ToneFrequency(double targetHz) => Clock / (16 * Math.Max(1, Math.Round(Clock / (16 * targetHz))));

    private static void SetTone(Ay38910 chip, int channel, double targetHz)
    {
        var period = (int)Math.Max(1, Math.Round(Clock / (16 * targetHz)));
        Write(chip, channel * 2, period & 0xFF);
        Write(chip, channel * 2 + 1, period >> 8);
    }

    private static void Write(Ay38910 chip, int register, int value)
    {
        chip.WritePort(0xA0, (byte)register);
        chip.WritePort(0xA1, (byte)value);
    }

    private static void WriteRenderedFrames(BinaryWriter writer, Ay38910 chip, int frameCount)
    {
        var frames = new AudioFrame[4096];
        while (frameCount > 0)
        {
            var count = Math.Min(frameCount, frames.Length);
            chip.Render(frames.AsSpan(0, count));
            for (var i = 0; i < count; i++)
            {
                writer.Write(ToPcm16(frames[i].Left));
                writer.Write(ToPcm16(frames[i].Right));
            }
            frameCount -= count;
        }
    }

    private static short ToPcm16(float value) => (short)Math.Clamp(Math.Round(value * short.MaxValue), short.MinValue, short.MaxValue);

    private static void WriteWaveHeader(BinaryWriter writer, int frameCount)
    {
        const short channels = 2;
        const short bitsPerSample = 16;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var dataSize = checked(frameCount * blockAlign);
        writer.Write("RIFF"u8.ToArray()); writer.Write(checked(36 + dataSize)); writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray()); writer.Write(16); writer.Write((short)1); writer.Write(channels);
        writer.Write(SampleRate); writer.Write(SampleRate * blockAlign); writer.Write(blockAlign); writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray()); writer.Write(dataSize);
    }
}

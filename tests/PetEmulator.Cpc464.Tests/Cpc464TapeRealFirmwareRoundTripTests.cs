using NUnit.Framework;
using PetEmulator.TestSupport;

namespace PetEmulator.Cpc464.Tests;

/// <summary>End-to-end real-firmware tape round trip: type a BASIC program through the emulated
/// keyboard matrix (the classic BYTE-magazine Sieve of Eratosthenes, also exercising '(' ')' '=' '*'
/// ':', punctuation no earlier test typed - and which caught two real matrix bugs, see
/// Cpc464ScreenOcr.KeyFor's own history), SAVE it via the real CSAVE routine (answering the
/// firmware's own "Press PLAY and REC then any key" prompt - skip that and the motor never turns on,
/// which is what silently produced zero recorded pulses the first time this was tried), reset onto a
/// second machine, LOAD it back (same "Press PLAY then any key" prompt), then LIST it on both
/// machines and compare: LISTing tokenizes keywords back into text through the same firmware code
/// path on both sides, so any rendering quirk from tokenization (this OCR table isn't trained on
/// keyword glyphs, which render differently from plain typed letters) cancels out in the diff instead
/// of masquerading as a real mismatch - a raw-RAM byte comparison was tried first and abandoned: it
/// kept finding the loaded program's bytes "missing" even when LIST proved the reload was byte-exact,
/// which just means this ROM's program-area layout isn't the fixed address a flat search assumed.</summary>
public sealed class Cpc464TapeRealFirmwareRoundTripTests
{
    private static readonly string[] SieveProgram =
    [
        "10 DIM A(8190)",
        "20 FOR I=2 TO 8190",
        "30 IF A(I)=0 THEN P=P+1:FOR J=I*I TO 8190 STEP I:A(J)=1:NEXT J",
        "40 NEXT I",
        "50 PRINT P",
    ];

    [Test]
    [Category("BinaryBoot")]
    public void SieveProgram_SurvivesTypeSaveResetLoad()
    {
        var rom = File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "roms", "cpc464", "cpc464.rom"));
        var ocr = Cpc464ScreenOcr.BuildTable(rom);

        var recorder = Boot(rom);
        foreach (var line in SieveProgram) { Type(recorder, line); PressEnter(recorder); }
        var referenceListing = List(recorder, ocr);
        Assert.That(referenceListing, Does.Contain("8190"), $"typed program's own LIST didn't show '8190' - the oracle itself is broken:\n{referenceListing}");

        Type(recorder, "SAVE\"TEST\"");
        PressEnter(recorder);
        recorder.Run(5_000_000);
        PressEnter(recorder); // "Press PLAY and REC then any key"
        var saveMotorOff = RunUntilMotorOff(recorder, budget: 30_000_000);
        Assert.That(saveMotorOff, Is.True, "real-firmware SAVE never finished (motor stayed on) within budget");
        Assert.That(ScreenText(recorder, ocr), Does.Not.Contain("ERROR"), "SAVE reported an error");

        Assert.That(recorder.Bus.Cassette.TryGetRecordedPulses(out var pulses), Is.True);
        Assert.That(pulses, Is.Not.Empty);

        var loader = Boot(rom);
        loader.Bus.Cassette.LoadPulses(pulses);
        Type(loader, "LOAD\"TEST\"");
        PressEnter(loader);
        loader.Run(2_000_000);
        PressEnter(loader); // "Press PLAY then any key"
        var loadMotorOff = RunUntilMotorOff(loader, budget: 30_000_000);
        Assert.That(loadMotorOff, Is.True, "real-firmware LOAD never finished (motor stayed on) within budget");
        Assert.That(ScreenText(loader, ocr), Does.Not.Contain("ERROR"), "LOAD reported an error");

        var loadedListing = List(loader, ocr);
        Assert.That(loadedListing, Is.EqualTo(referenceListing),
            "the program LOAD produced doesn't LIST the same as the program that was typed and SAVEd");
    }

    private static Cpc464Machine Boot(byte[] rom)
    {
        var machine = new Cpc464Machine(rom);
        machine.Run(2_000_000);
        return machine;
    }

    private static bool RunUntilMotorOff(Cpc464Machine machine, ulong budget)
    {
        const ulong chunk = 5_000_000;
        for (ulong used = 0; used < budget; used += chunk)
        {
            machine.Run(chunk);
            if (!machine.Bus.Cassette.MotorOn) return true;
        }
        return false;
    }

    /// <summary>Types LIST and returns just the listing text, from the echoed "LIST" command up to
    /// (not including) the "Ready" prompt that follows it - concatenated across screen rows with no
    /// row separators, so a program line the real 40-column display wraps across two rows (line 30
    /// here is 63 characters) reads back as one continuous logical line instead of needing the
    /// wrap point re-aligned by hand. This also means the comparison doesn't care that the recorder
    /// and loader machines have different unrelated scroll history above the listing (different
    /// typed commands, different SAVE/LOAD prompts) - only the sliced-out listing itself is compared.</summary>
    private static string List(Cpc464Machine machine, IReadOnlyDictionary<string, char> ocr)
    {
        Type(machine, "LIST");
        PressEnter(machine);
        machine.Run(1_000_000);
        var flat = ScreenTextFlat(machine, ocr);
        var start = flat.LastIndexOf("LIST", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"couldn't find the echoed LIST command on screen:\n{flat}");
        var end = flat.IndexOf("?EADY", start, StringComparison.Ordinal); // 'R' renders differently in this prompt - see Cpc464ScreenOcr's own note
        Assert.That(end, Is.GreaterThan(start), $"couldn't find 'Ready' after LIST's output:\n{flat}");
        return flat[start..end];
    }

    private static string ScreenText(Cpc464Machine machine, IReadOnlyDictionary<string, char> ocr)
    {
        machine.Bus.GateArray.RenderFrame();
        var pixels = machine.Bus.GateArray.Pixels;
        var sb = new System.Text.StringBuilder();
        for (var row = 0; row < 25; row++)
            sb.Append(ScreenTextOcr.DecodeRow(pixels, Cpc464ScreenOcr.ScreenWidth, 40, Cpc464ScreenOcr.CellWidth, Cpc464ScreenOcr.CellHeight, row, ocr)).Append('\n');
        return sb.ToString();
    }

    private static string ScreenTextFlat(Cpc464Machine machine, IReadOnlyDictionary<string, char> ocr)
    {
        machine.Bus.GateArray.RenderFrame();
        var pixels = machine.Bus.GateArray.Pixels;
        var sb = new System.Text.StringBuilder();
        for (var row = 0; row < 25; row++)
            sb.Append(ScreenTextOcr.DecodeRow(pixels, Cpc464ScreenOcr.ScreenWidth, 40, Cpc464ScreenOcr.CellWidth, Cpc464ScreenOcr.CellHeight, row, ocr));
        return sb.ToString();
    }

    /// <summary>Types via the real matrix - the same character set (letters, digits, space, '"',
    /// '(' ')' '=' '*' ':') <see cref="Cpc464ScreenOcr"/> trains its glyph table against, so both use
    /// one verified source of truth for what each physical key position produces.</summary>
    private static void Type(Cpc464Machine machine, string text)
    {
        foreach (var ch in text)
        {
            var (row, col, shift) = Cpc464ScreenOcr.KeyFor(ch);
            Press(machine, row, col, shift);
        }
    }

    private static void PressEnter(Cpc464Machine machine) => Press(machine, 2, 2, false);

    private static void Press(Cpc464Machine machine, byte row, byte col, bool shift)
    {
        const int ShiftColumn = 5;
        if (shift) machine.Bus.Keyboard.SetKey(2, ShiftColumn, true);
        machine.Bus.Keyboard.SetKey(row, col, true);
        machine.Run(25_000);
        machine.Bus.Keyboard.SetKey(row, col, false);
        if (shift) machine.Bus.Keyboard.SetKey(2, ShiftColumn, false);
        // 60,000, not the 15,000 Cpc464BootTests gets away with: completing a keyword like "DIM"
        // triggers real-time tokenization/redraw work in the firmware's line editor that needs more
        // idle time before the next keypress than a plain character does - too short a gap here
        // dropped the next key's Shift line entirely (reproduced: typing "DIM A(" this way silently
        // turned '(' into '8', the unshifted glyph at the same matrix cell).
        machine.Run(60_000);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}

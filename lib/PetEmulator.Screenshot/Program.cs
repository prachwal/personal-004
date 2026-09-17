using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PetEmulator.Desktop;
using PetEmulator.Desktop.Models;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Desktop.Views;
using PetEmulator.Core.Keyboard;

namespace PetEmulator.Screenshot;

/// <summary>
/// Renders <see cref="MainWindow"/> off-screen (Avalonia.Headless + Skia - no OS screenshot
/// utility or real X display needed, works identically under xvfb or fully headless CI) and
/// saves one frame to a PNG. Built because this dev environment has no screenshot tool
/// (import/scrot/xwd) and installing one needs root it doesn't have.
///
/// Usage: dotnet run --project lib/PetEmulator.Screenshot -- [--machine name]
///   [--ticks N] [--out path.png] [--cartridge path.crt] [--type "text"]
///
/// `--machine` matches any label in MainWindowViewModel.ModuleChoices (including nested PET/
/// SuperPET profile entries), case- and punctuation-insensitive prefix match - e.g. "vic20"
/// matches "VIC-20", "trs80" matches "TRS-80 Model I", "kaypro" matches "Kaypro II". This is
/// deliberately NOT a per-machine if/else: every machine already implements IMachineViewModel
/// and is registered in ModuleChoices, so a new machine gets a screenshot for free the moment
/// it's added there - the tool doesn't get a bespoke case.
/// </summary>
internal static class Program
{
    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia();

    public static int Main(string[] args)
    {
        var machine = ArgValue(args, "--machine") ?? "pet";
        var ticks = int.Parse(ArgValue(args, "--ticks") ?? "150");
        var outPath = ArgValue(args, "--out") ?? $"build/screenshots/{machine}.png";

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        BuildAvaloniaApp().SetupWithoutStarting();

        var window = new MainWindow();
        window.Show();

        var viewModel = (MainWindowViewModel)window.DataContext!;

        // Every selectable entry, PET/SuperPET's nested per-profile children included - the same
        // flatten MainWindowViewModel's own constructor uses to pick its initial machine.
        var entries = viewModel.ModuleChoices
            .SelectMany(entry => entry.Create is not null ? (IEnumerable<ModuleMenuEntry>)[entry] : entry.Children ?? [])
            .ToList();
        var selected = entries.FirstOrDefault(e => Normalize(e.Label).StartsWith(Normalize(machine), StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"No machine matches '--machine {machine}'. Known: {string.Join(", ", entries.Select(e => e.Label))}");
        viewModel.SwitchMachineCommand.Execute(selected);

        var machineViewModel = (IMachineViewModel)viewModel.CurrentModule;

        var cartridgePath = ArgValue(args, "--cartridge");
        if (cartridgePath is not null)
        {
            if (machineViewModel is not Vic20MachineViewModel vic20)
                throw new ArgumentException($"--cartridge only applies to VIC-20, not '{selected.Label}'.");

            // Mounted before any Tick() runs, same as Vic20DebuggerSession's "cartridge" command -
            // the machine's CPU hasn't executed a single instruction yet at this point (only
            // Vic20Machine's constructor-time Reset() has run), so the KERNAL's own cold-start
            // still gets to discover the freshly-inserted cartridge signature normally; no extra
            // Reset() call needed here.
            vic20.LoadCartridge(cartridgePath);
        }

        // Stop viewModel's own 20ms render-loop DispatcherTimer as early as possible - this tool
        // never needs it (it drives every Tick() manually below) and it is actively dangerous to
        // leave armed: window.KeyPress/KeyRelease (used by the routed --type path further down)
        // call Avalonia's own HeadlessWindowExtensions.RunJobsOnImpl internally, which itself
        // calls the same unbounded Dispatcher.UIThread.RunJobs() this file's own end-of-run
        // comment already warned about - "draining the job queue with one still armed never
        // returns" (a self-re-enqueuing timer means the queue is never empty). That hazard used
        // to be worked around only for the FINAL RunJobs() call below (by disposing viewModel
        // first) but not for window.KeyPress/KeyRelease's own internal one, which hung for hours
        // (not a busy spin - genuinely blocked) the first time --type ran without --direct on a
        // machine with real on-screen content. Disposing here, before either the tick loop or
        // --type ever call anything Avalonia-routed, removes the hazard at its source instead of
        // dodging it call-by-call. CurrentModule.Dispose() (audio output) is harmless this early
        // too - nothing after this point needs live audio.
        viewModel.Dispose();

        // Tick the machine directly the same number of times real wall-clock ticks would -
        // PetMachineViewModel/Vic20MachineViewModel.Tick() raises FrameReady synchronously, which
        // the child View's code-behind handles immediately (Screen.UpdateFrame), so the
        // WriteableBitmap content is already current after this loop.
        for (var i = 0; i < ticks; i++)
            machineViewModel.Tick();

        var typeText = ArgValue(args, "--type");
        if (typeText is not null)
        {
            // Real routed key events through window.KeyPress/KeyRelease (Avalonia.Headless input
            // simulation) - the SAME path a real OS keypress takes (focus -> bubble -> Window's
            // KeyDown/KeyUp handlers -> MainWindowViewModel.HandleKey), unlike calling
            // machine.Keyboard.Press directly. Built specifically to catch focus-routing bugs
            // (see docs/vic20/rendering-fixes.md's keyboard-focus regression) that a direct-call
            // test can't see.
            var direct = ArgValue(args, "--direct") is not null;
            foreach (var ch in typeText)
            {
                var key = ToAvaloniaKey(ch);
                if (key is null)
                    continue;
                var hk = ToHostKey(key.Value);
                var atKey = ToAtKeyboardKey(ch);
                var cell = atKey is { } physicalKey
                    ? new PetEmulator.Vic20.Keyboard.Vic20KeyboardMap().Translate(physicalKey)
                    : null;
                Console.WriteLine($"typing '{ch}' -> {key}, hostKey={hk}, atKey={atKey}, cell={cell}");

                if (direct)
                    machineViewModel.HandleKey(key.Value, PetEmulator.Pet.Keyboard.HostKeyEventKind.Press);
                else
                    window.KeyPress(key.Value, RawInputModifiers.None, PhysicalKey.None, null);
                machineViewModel.Tick(); // 20,000 instructions - plenty of hold time (TextTyper needs only 8-12K)

                if (direct)
                    machineViewModel.HandleKey(key.Value, PetEmulator.Pet.Keyboard.HostKeyEventKind.Release);
                else
                    window.KeyRelease(key.Value, RawInputModifiers.None, PhysicalKey.None, null);
                machineViewModel.Tick();
            }
        }

        // viewModel (and its DispatcherTimer) was already disposed above, before anything
        // Avalonia-routed ran - safe to pump the dispatcher now to flush the one pending
        // layout/render pass EmulatorScreenControl.InvalidateVisual() actually needs.
        Dispatcher.UIThread.RunJobs();

        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Headless capture returned no frame.");
        using (var stream = File.Create(outPath))
            frame.Save(stream, new PngBitmapEncoderOptions());

        Console.WriteLine($"Saved {Path.GetFullPath(outPath)} ({frame.PixelSize.Width}x{frame.PixelSize.Height}) after {ticks} ticks of '{machine}'.");
        return 0;
    }

    private static string? ArgValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string Normalize(string s) =>
        new([.. s.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

    // Diagnostic-only mirror of PetEmulator.Desktop.KeyMapping.ToHostKey (internal, not worth
    // exposing cross-project for a debug print) - just enough cases for this tool's own --type.
    private static string? ToHostKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => "Key" + key,
        >= Key.D0 and <= Key.D9 => "Digit" + (key - Key.D0),
        Key.Space => "Space",
        Key.Return => "Enter",
        _ => null,
    };

    private static Key? ToAvaloniaKey(char ch) => char.ToUpperInvariant(ch) switch
    {
        >= 'A' and <= 'Z' => Key.A + (char.ToUpperInvariant(ch) - 'A'),
        >= '0' and <= '9' => Key.D0 + (ch - '0'),
        ' ' => Key.Space,
        '\n' or '\r' => Key.Return,
        _ => null,
    };

    private static AtKeyboardKey? ToAtKeyboardKey(char ch)
    {
        var upper = char.ToUpperInvariant(ch);
        if (upper is >= 'A' and <= 'Z')
            return (AtKeyboardKey)((int)AtKeyboardKey.A + upper - 'A');
        if (upper is >= '1' and <= '9')
            return (AtKeyboardKey)((int)AtKeyboardKey.D1 + upper - '1');
        return upper switch
        {
            '0' => AtKeyboardKey.D0,
            ' ' => AtKeyboardKey.Space,
            '\n' or '\r' => AtKeyboardKey.Enter,
            _ => null,
        };
    }
}

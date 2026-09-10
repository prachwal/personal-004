using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PetEmulator.Desktop;
using PetEmulator.Desktop.Input;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Desktop.Views;

namespace PetEmulator.Screenshot;

/// <summary>
/// Renders <see cref="MainWindow"/> off-screen (Avalonia.Headless + Skia - no OS screenshot
/// utility or real X display needed, works identically under xvfb or fully headless CI) and
/// saves one frame to a PNG. Built because this dev environment has no screenshot tool
/// (import/scrot/xwd) and installing one needs root it doesn't have.
///
/// Usage: dotnet run --project src/PetEmulator.Screenshot -- [--machine pet|vic20]
///   [--ticks N] [--out path.png]
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
        if (machine.Equals("vic20", StringComparison.OrdinalIgnoreCase))
        {
            var entry = viewModel.MachineChoices.First(e => e.Label == "VIC-20");
            viewModel.SwitchMachineCommand.Execute(entry);
        }

        // Tick the machine directly the same number of times real wall-clock ticks would -
        // PetMachineViewModel/Vic20MachineViewModel.Tick() raises FrameReady synchronously, which
        // the child View's code-behind handles immediately (Screen.UpdateFrame), so the
        // WriteableBitmap content is already current after this loop.
        for (var i = 0; i < ticks; i++)
            viewModel.CurrentMachine.Tick();

        var typeText = ArgValue(args, "--type");
        if (typeText is not null)
        {
            // Real routed key events through window.KeyPress/KeyRelease (Avalonia.Headless input
            // simulation) - the SAME path a real OS keypress takes (focus -> bubble -> Window's
            // KeyDown/KeyUp handlers -> MainWindowViewModel.HandleKey), unlike calling
            // machine.Keyboard.Press directly. Built specifically to catch focus-routing bugs
            // (see docs/vic20-rendering-fixes.md's keyboard-focus regression) that a direct-call
            // test can't see.
            var direct = ArgValue(args, "--direct") is not null;
            foreach (var ch in typeText)
            {
                var key = ToAvaloniaKey(ch);
                if (key is null)
                    continue;
                var hk = ToHostKey(key.Value);
                var cell = hk is null ? null : new PetEmulator.Vic20.Keyboard.Vic20KeyboardMap().Translate(hk);
                Console.WriteLine($"typing '{ch}' -> {key}, hostKey={hk}, cell={cell}");

                if (direct)
                    viewModel.CurrentMachine.HandleKey(key.Value, PetEmulator.Pet.Keyboard.HostKeyEventKind.Press);
                else
                    window.KeyPress(key.Value, RawInputModifiers.None, PhysicalKey.None, null);
                viewModel.CurrentMachine.Tick(); // 20,000 instructions - plenty of hold time (TextTyper needs only 8-12K)

                if (direct)
                    viewModel.CurrentMachine.HandleKey(key.Value, PetEmulator.Pet.Keyboard.HostKeyEventKind.Release);
                else
                    window.KeyRelease(key.Value, RawInputModifiers.None, PhysicalKey.None, null);
                viewModel.CurrentMachine.Tick();
            }
        }

        // viewModel's own DispatcherTimer (the real 20ms render-loop cadence a running app uses)
        // is still live at this point and would keep re-enqueueing itself forever under
        // RunJobs() - a headless dispatcher fast-forwards time for pending timers instead of
        // waiting on the wall clock, so draining the queue with one still armed never
        // terminates. Stop it before pumping the dispatcher to flush the one pending
        // layout/render pass PetScreenControl.InvalidateVisual() actually needs.
        viewModel.Dispose();
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
}

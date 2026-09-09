using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PetEmulator.Desktop;

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
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PetEmulator.Desktop;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.KeyboardShot;

/// <summary>
/// Renders one on-screen keyboard layout off-screen (Avalonia.Headless + Skia - no X display
/// needed) and saves it to a PNG, for visual comparison against real hardware photos.
///
/// Usage: dotnet run --project tools/PetEmulator.KeyboardShot -- [--layout kaypro|cpc464] [--out path.png]
/// Layouts: kaypro (Kaypro II), cpc464 (Amstrad CPC464) - each must have an EmulatorKeyboardLayout factory.
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        var layoutName = ArgValue(args, "--layout") ?? "kaypro";
        var outPath = ArgValue(args, "--out") ?? $"build/screenshots/{layoutName}-keyboard.png";

        var layout = layoutName.ToLowerInvariant() switch
        {
            "kaypro" => KayproKeyboardLayoutFactory.Build(),
            "cpc464" => CpcKeyboardLayoutFactory.Build(),
            _ => throw new ArgumentException($"Unknown keyboard layout '--layout {layoutName}'. Known: kaypro, cpc464."),
        };
        Console.WriteLine($"Layout '{layout.Id}': {layout.Keys.Count} keys, design size {layout.DesignSize}.");

        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia()
            .SetupWithoutStarting();

        var view = new EmulatorKeyboardView();
        view.Configure(layout, (_, _) => { });
        var window = new Window
        {
            Content = view,
            Width = layout.DesignSize.Width + 32,
            Height = layout.DesignSize.Height + 32,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Headless capture returned no frame.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        using (var stream = File.Create(outPath))
            frame.Save(stream, new PngBitmapEncoderOptions());

        Console.WriteLine($"Saved {Path.GetFullPath(outPath)} ({frame.PixelSize.Width}x{frame.PixelSize.Height}).");
        return 0;
    }

    private static string? ArgValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

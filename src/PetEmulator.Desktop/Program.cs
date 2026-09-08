using Avalonia;

namespace PetEmulator.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .LogToTrace();
}

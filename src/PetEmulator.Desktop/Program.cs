using Avalonia;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;

namespace PetEmulator.Desktop;

internal static class Program
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("Startup");

    [STAThread]
    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.LogCritical(
                e.ExceptionObject as Exception ?? new InvalidOperationException($"Non-exception thrown: {e.ExceptionObject}"),
                "AppDomain.UnhandledException (terminating={Terminating}).", e.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.LogError(e.Exception, "TaskScheduler.UnobservedTaskException.");
            e.SetObserved();
        };

        Log.LogInformation(
            "PetEmulator.Desktop starting args=[{Args}] os={Os} runtime={Runtime} 64bit={Is64Bit} baseDir={BaseDir} cwd={Cwd} PET_EMULATOR_ROMS={Roms} PET_EMULATOR_LOG_LEVEL={Level} PET_EMULATOR_CONSOLE_LOG={Console}.",
            string.Join(' ', args), Environment.OSVersion, Environment.Version, Environment.Is64BitProcess,
            AppContext.BaseDirectory, Environment.CurrentDirectory,
            Environment.GetEnvironmentVariable("PET_EMULATOR_ROMS") ?? "(unset)",
            Environment.GetEnvironmentVariable("PET_EMULATOR_LOG_LEVEL") ?? "(unset)",
            Environment.GetEnvironmentVariable("PET_EMULATOR_CONSOLE_LOG") ?? "(unset)");
        Log.LogInformation("Session log: {LogFile}.", EmulatorLogging.LogFilePath);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Log.LogInformation("Avalonia lifetime exited normally.");
        }
        catch (Exception ex)
        {
            // Previously this propagated with no log anywhere - on WinExe there is not even
            // a console to show it. The log file now always has the full chain.
            Log.LogCritical(ex, "Unhandled exception escaped the Avalonia lifetime.");
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

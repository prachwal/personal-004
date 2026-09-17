using Microsoft.Extensions.Logging;

namespace PetEmulator.Core.Logging;

/// <summary>
/// The single shared <see cref="ILogger"/> factory for every emulator project (machines, CLI,
/// Desktop). Backed by a small built-in file sink - MEL ships no file provider, and a file is
/// what this repo needs (a WinExe has no console to show anything on).
///
/// Levels come from <c>PET_EMULATOR_LOG_LEVEL</c> (default <see cref="LogLevel.Debug"/> - full
/// detail to the file). The console mirror is on in an interactive terminal and off when stdout
/// is redirected (so scripts parsing CLI output stay clean); <c>PET_EMULATOR_CONSOLE_LOG=1/0</c>
/// forces it either way. A WinExe has no console at all - there the file is the log.
///
/// Thread-safe, never throws out of a log call, needs no host/DI setup - which is exactly why
/// the machine libraries can take an optional <c>ILogger?</c> (defaulting to
/// <see cref="NullLogger.Instance"/>) instead of dragging a logging framework through every
/// constructor and static parser.
/// </summary>
public static class EmulatorLogging
{
    private static readonly object Sync = new();
    private static readonly string? FilePath;

    static EmulatorLogging()
    {
        MinimumLevel = ParseLevel(Environment.GetEnvironmentVariable("PET_EMULATOR_LOG_LEVEL"), LogLevel.Debug);
        ConsoleMirror = Environment.GetEnvironmentVariable("PET_EMULATOR_CONSOLE_LOG") switch
        {
            "1" or "true" or "TRUE" => true,
            "0" or "false" or "FALSE" => false,
            // Interactive terminal: mirror on so logs are visible. Piped/redirected output
            // (scripts parsing CLI results): mirror off so machine logs don't pollute stdout.
            // A WinExe has no console at all - nothing to mirror to, the file is the log.
            _ => !IsOutputRedirected(),
        };

        string? path = null;
        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "logs");
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception)
            {
                // Published/single-file layouts or locked-down folders may not allow
                // writing next to the binaries - fall back to the temp directory instead.
                directory = Path.Combine(Path.GetTempPath(), "PetEmulator", "logs");
                Directory.CreateDirectory(directory);
            }

            path = Path.Combine(directory, $"petemulator-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
            File.WriteAllText(path, $"PetEmulator log started {DateTimeOffset.Now:O} pid={Environment.ProcessId}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            // Console-only mode: never let logger setup take the app down.
            try
            {
                Console.Error.WriteLine($"[EmulatorLogging] file log unavailable: {ex.Message}");
            }
            catch (Exception)
            {
                // Last resort: truly nowhere to write.
            }

            path = null;
        }

        FilePath = path;
    }

    /// <summary>Full path of the current session log, or a placeholder when file logging is unavailable.</summary>
    public static string LogFilePath => FilePath ?? "(file log unavailable - console only)";

    /// <summary>Minimum level written to the file (and console mirror).</summary>
    public static LogLevel MinimumLevel { get; }

    /// <summary>Whether log lines are also mirrored to the console.</summary>
    public static bool ConsoleMirror { get; }

    /// <summary>Creates a logger with a short category name (e.g. "VIC20", "Shell").</summary>
    public static ILogger CreateLogger(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return new EmulatorFileLogger(category);
    }

    /// <summary>Creates a logger categorized by <typeparamref name="T"/>.</summary>
    public static ILogger<T> CreateLogger<T>() => new EmulatorFileLogger<T>();

    /// <summary>Formats an exception chain (type, message, HResult, stack, inner exceptions).</summary>
    public static string FormatException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var writer = new StringWriter();
        var depth = 0;
        for (var current = exception; current is not null; current = current.InnerException)
        {
            writer.WriteLine($"{new string(' ', depth * 2)}{current.GetType().FullName} (HResult=0x{current.HResult:X8}): {current.Message}");
            if (!string.IsNullOrEmpty(current.StackTrace))
                writer.WriteLine($"{new string(' ', depth * 2)}{current.StackTrace}");
            depth++;
            if (depth > 10)
            {
                writer.WriteLine("(inner exception chain truncated)");
                break;
            }
        }

        return writer.ToString();
    }

    internal static void Write(string category, LogLevel level, string message, Exception? exception)
    {
        if (level < MinimumLevel)
            return;

        var line = $"{DateTimeOffset.Now:O} [{level}] [{category}] {message}";
        if (exception is not null)
            line += $"{Environment.NewLine}{FormatException(exception)}";

        lock (Sync)
        {
            try
            {
                if (ConsoleMirror)
                {
                    if (level >= LogLevel.Error)
                        Console.Error.WriteLine(line);
                    else
                        Console.WriteLine(line);
                }

                if (FilePath is not null)
                    File.AppendAllText(FilePath, line + Environment.NewLine);
            }
            catch (Exception)
            {
                // A logger must never throw - drop the line instead.
            }
        }
    }

    private sealed class NullScopeInstance : IDisposable
    {
        public static readonly NullScopeInstance Instance = new();

        public void Dispose()
        {
        }
    }

    private static LogLevel ParseLevel(string? value, LogLevel fallback) =>
        Enum.TryParse(value, ignoreCase: true, out LogLevel level) ? level : fallback;

    private static bool IsOutputRedirected()
    {
        try
        {
            return Console.IsOutputRedirected;
        }
        catch (Exception)
        {
            // No console attached at all (WinExe) - treat as redirected so nothing attempts
            // console writes; the file log carries everything.
            return true;
        }
    }

    private sealed class EmulatorFileLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScopeInstance.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Write(category, logLevel, formatter(state, exception), exception);
        }
    }

    private sealed class EmulatorFileLogger<T> : ILogger<T>
    {
        private readonly EmulatorFileLogger _inner = new(typeof(T).FullName ?? typeof(T).Name);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}

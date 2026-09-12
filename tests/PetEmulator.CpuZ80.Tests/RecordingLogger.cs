using Microsoft.Extensions.Logging;

namespace PetEmulator.CpuZ80.Tests;

/// <summary>
/// Minimal ILogger test double: records formatted messages, respects a
/// configurable minimum level (so "off by default" is actually testable),
/// no mocking framework needed for something this small.
/// </summary>
internal sealed class RecordingLogger<T>(LogLevel minLevel) : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
            Messages.Add(formatter(state, exception));
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose()
        {
        }
    }
}

using Api;
using Microsoft.Extensions.Logging;

namespace Api.Test;

public class LoggerMock<T>: ILogger<T> {
    public volatile bool Enabled = false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (Enabled) lock(this) System.Console.Error.WriteLine(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => Enabled;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

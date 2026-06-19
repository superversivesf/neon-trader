using Microsoft.Extensions.Logging;

namespace NeonTrader.Core;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly LogLevel _minLevel;

    public FileLoggerProvider(string path, LogLevel minLevel = LogLevel.Information)
    {
        _writer = new StreamWriter(path, append: false) { AutoFlush = true };
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(categoryName, _writer, _minLevel);

    public void Dispose() => _writer.Dispose();
}

internal sealed class FileLogger : ILogger
{
    private readonly string _category;
    private readonly StreamWriter _writer;
    private readonly LogLevel _minLevel;

    public FileLogger(string category, StreamWriter writer, LogLevel minLevel)
    {
        _category = category;
        _writer = writer;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {_category}: {message}";
        if (exception != null)
            line += $"\n{exception}";

        _writer.WriteLine(line);
    }
}

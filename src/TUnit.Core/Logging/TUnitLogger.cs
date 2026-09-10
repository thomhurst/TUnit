namespace TUnit.Core.Logging;

public abstract class TUnitLogger : ILogger
{
    public abstract ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

    public abstract void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

    public virtual bool IsEnabled(LogLevel logLevel)
    {
        return GlobalContext.Current.GlobalLogger.IsEnabled(logLevel);
    }
}

using System;
using System.Threading.Tasks;
using TUnit.Core.Logging;
using Microsoft.Testing.Platform.Extensions;

namespace SimplifiedArchitectureTest;

public class SimpleLogger : ILogger
{
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        Console.WriteLine($"[{logLevel}] {message}");
        if (exception != null)
        {
            Console.WriteLine(exception);
        }
    }

    public ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Log(logLevel, state, exception, formatter);
        return default;
    }
}

public class SimpleExtension : IExtension
{
    public string Uid => "SimplifiedArchitectureTest";
    public string Version => "1.0.0";
    public string DisplayName => "Simplified Architecture Test";
    public string Description => "Test runner for simplified architecture";
    
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
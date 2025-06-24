using System;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using TUnit.Engine.Logging;

namespace SimplifiedArchitectureTest;

public class MockLogger : TUnitFrameworkLogger
{
    public MockLogger() : base(
        new SimpleExtension(),
        new ConsoleOutputDevice(),
        new DummyLogger(),
        new DummyCommandLineOptions())
    {
    }
}

class ConsoleOutputDevice : IOutputDevice
{
    public Task DisplayAsync(IOutputDeviceDataProducer producer, IOutputDeviceData data)
    {
        if (data is FormattedTextOutputDeviceData text)
        {
            Console.WriteLine(text.Text);
        }
        return Task.CompletedTask;
    }

    public void Dispose() { }

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}

class DummyLogger : ILogger
{
    public bool IsEnabled(Microsoft.Testing.Platform.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Testing.Platform.Logging.LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }

    public Task LogAsync<TState>(Microsoft.Testing.Platform.Logging.LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Log(logLevel, state, exception, formatter);
        return Task.CompletedTask;
    }
}

class DummyCommandLineOptions : ICommandLineOptions
{
    public bool IsOptionSet(string optionName) => false;
    
    public bool TryGetOptionArgumentList(string optionName, out string[]? arguments)
    {
        arguments = null;
        return false;
    }
}
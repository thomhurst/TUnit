# Logging

TUnit provides a flexible logging system that captures all test output and routes it to configurable destinations called "log sinks".

## Basic Usage

By default, TUnit intercepts any logs to `Console.WriteLine()` and correlates them to the test that triggered the log using the current async context.

```csharp
[Test]
public async Task MyTest()
{
    Console.WriteLine("This output is captured and associated with this test");
}
```

### Logger Objects

For more control, use `TestContext.Current.GetDefaultLogger()` to get a logger instance:

```csharp
[Test]
public async Task MyTest()
{
    var logger = TestContext.Current!.GetDefaultLogger();
    logger.LogInformation("Information message");
    logger.LogWarning("Warning message");
    logger.LogError("Error message");
}
```

This logger can integrate with other logging frameworks like Microsoft.Extensions.Logging for ASP.NET applications.

## Log Sinks

TUnit uses a sink-based architecture where all output is routed through registered log sinks. Each sink decides how to handle the messages - write to files, stream to IDEs, send to external services, etc.

### Built-in Sinks

TUnit automatically registers these sinks based on your execution context:

| Sink | When Registered | Purpose |
|------|-----------------|---------|
| **TestOutputSink** | Always | Captures output for test results shown after execution |
| **ConsoleOutputSink** | `--output Detailed` | Writes real-time output to the console |
| **RealTimeOutputSink** | IDE clients (VS, Rider) | Streams output to IDE test explorers |

### Creating Custom Log Sinks

Implement the `ILogSink` interface to create a custom sink:

```csharp
using TUnit.Core;
using TUnit.Core.Logging;

public class FileLogSink : ILogSink, IAsyncDisposable
{
    private readonly StreamWriter _writer;

    public FileLogSink(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true);
    }

    public bool IsEnabled(LogLevel level)
    {
        // Return false to skip processing for performance
        return level >= LogLevel.Information;
    }

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        // Get test name from context if available
        var testName = context is TestContext tc
            ? tc.TestDetails.TestName
            : "Unknown";

        _writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] [{testName}] {message}");

        if (exception != null)
        {
            _writer.WriteLine(exception.ToString());
        }
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        Log(level, message, exception, context);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.FlushAsync();
        await _writer.DisposeAsync();
    }
}
```

### Registering Custom Sinks

Register your sink in a `[Before(Assembly)]` hook so it's active before any tests run:

```csharp
public class TestSetup
{
    [Before(Assembly)]
    public static void SetupLogging()
    {
        // Register by instance (for sinks needing configuration)
        TUnitLoggerFactory.AddSink(new FileLogSink("test-output.log"));

        // Or register by type (for simple sinks)
        TUnitLoggerFactory.AddSink<DebugLogSink>();
    }
}
```

Sinks that implement `IDisposable` or `IAsyncDisposable` are automatically disposed when the test session ends.

### Context Information

The `context` parameter provides information about where the log originated:

```csharp
public void Log(LogLevel level, string message, Exception? exception, Context? context)
{
    switch (context)
    {
        case TestContext tc:
            // During test execution
            var testName = tc.TestDetails.TestName;
            var className = tc.TestDetails.ClassType.Name;
            break;

        case ClassHookContext chc:
            // During [Before(Class)] or [After(Class)] hooks
            var classType = chc.ClassType;
            break;

        case AssemblyHookContext ahc:
            // During [Before(Assembly)] or [After(Assembly)] hooks
            var assembly = ahc.Assembly;
            break;

        case null:
            // Outside test execution
            break;
    }
}
```

### Example: Seq/Serilog Integration

Here's an example sink that sends logs to Seq:

```csharp
public class SeqLogSink : ILogSink, IDisposable
{
    private readonly Serilog.ILogger _logger;

    public SeqLogSink(string seqUrl)
    {
        _logger = new LoggerConfiguration()
            .WriteTo.Seq(seqUrl)
            .CreateLogger();
    }

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        var serilogLevel = level switch
        {
            LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };

        var testName = context is TestContext tc ? tc.TestDetails.TestName : "Unknown";

        _logger
            .ForContext("TestName", testName)
            .Write(serilogLevel, exception, message);
    }

    public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        Log(level, message, exception, context);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        (_logger as IDisposable)?.Dispose();
    }
}
```

## Log Levels

TUnit uses the same log level as provided to the Microsoft.Testing.Platform via command line:

```bash
dotnet run --log-level Warning
```

Available levels (from least to most severe):
- `Trace`
- `Debug`
- `Information` (default)
- `Warning`
- `Error`
- `Critical`

## Custom Loggers

You can also create custom loggers by inheriting from `DefaultLogger`:

```csharp
public class TestHeaderLogger : DefaultLogger
{
    private bool _hasOutputHeader;

    public TestHeaderLogger(Context context) : base(context) { }

    protected override string GenerateMessage(string message, Exception? exception, LogLevel logLevel)
    {
        var baseMessage = base.GenerateMessage(message, exception, logLevel);

        if (!_hasOutputHeader && Context is TestContext testContext)
        {
            _hasOutputHeader = true;
            var testId = $"{testContext.TestDetails.ClassType.Name}.{testContext.TestDetails.TestName}";
            return $"--- {testId} ---\n{baseMessage}";
        }

        return baseMessage;
    }
}
```

### Available Extension Points

- `Context` - Protected property to access the associated context
- `GenerateMessage(message, exception, logLevel)` - Override to customize message formatting
- `WriteToOutput(message, isError)` - Override to customize synchronous output
- `WriteToOutputAsync(message, isError)` - Override for asynchronous output

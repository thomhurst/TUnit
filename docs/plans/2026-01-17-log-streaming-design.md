# Log Streaming Plugin System Design

**Date:** 2026-01-17
**Issue:** [#4478 - Stream logs](https://github.com/thomhurst/TUnit/issues/4478)
**Status:** Draft

## Problem Statement

Currently, when using TUnit's logging with `TestContext.GetDefaultLogger()`, log output only appears in the IDE (e.g., Rider) after test completion. Users expect real-time log streaming during test execution, similar to NUnit's behavior.

```csharp
[Test]
public async Task X()
{
    for (int i = 0; i < 3; i += 1)
    {
        TestContext.Current!.GetDefaultLogger().LogInformation(i.ToString());
        await Task.Delay(1000);
    }
}
```

**Current behavior:** All 3 log messages appear after the test completes.
**Expected behavior:** Each log message appears as it's written.

## Root Cause

Microsoft Testing Platform has two output channels:
1. **Real-time:** `IOutputDevice.DisplayAsync()` - streams directly to IDEs during execution
2. **Historical:** `StandardOutputProperty` on `TestNodeUpdateMessage` - bundled at test completion

`DefaultLogger` writes to `context.OutputWriter` (historical) and `OriginalConsoleOut` (console), but never uses `IOutputDevice.DisplayAsync()` for real-time IDE streaming.

## Solution: Plugin-Based Log Sink System

Inspired by ASP.NET Core's logging architecture, we'll introduce a plugin system that:
1. Allows multiple log destinations (sinks)
2. Enables real-time streaming via `IOutputDevice`
3. Maintains backward compatibility with historical capture
4. Opens extensibility for custom sinks (Seq, file, etc.)

## Design

### Core Interfaces (TUnit.Core)

#### ILogSink

```csharp
namespace TUnit.Core.Logging;

/// <summary>
/// Represents a destination for log messages. Implement this interface
/// to create custom log sinks (e.g., file, Seq, Application Insights).
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Asynchronously logs a message.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The formatted message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="context">The current context (TestContext, ClassHookContext, etc.), or null if outside test execution.</param>
    ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context);

    /// <summary>
    /// Synchronously logs a message.
    /// </summary>
    void Log(LogLevel level, string message, Exception? exception, Context? context);

    /// <summary>
    /// Determines if this sink should receive messages at the specified level.
    /// </summary>
    bool IsEnabled(LogLevel level);
}
```

**Design notes:**
- Both sync and async methods match existing `ILogger` pattern
- `Context?` is nullable for console output outside test execution
- Sinks can cast to `TestContext` when they need test-specific info (test name, class, etc.)
- `IsEnabled` allows sinks to filter by level for performance
- If sink implements `IAsyncDisposable`, TUnit calls it at session end

#### TUnitLoggerFactory

```csharp
namespace TUnit.Core.Logging;

/// <summary>
/// Factory for configuring and managing log sinks.
/// </summary>
public static class TUnitLoggerFactory
{
    private static readonly List<ILogSink> Sinks = new();
    private static readonly object Lock = new();

    /// <summary>
    /// Registers a log sink to receive log messages.
    /// Call this in [Before(Assembly)] or before tests run.
    /// </summary>
    public static void AddSink(ILogSink sink)
    {
        lock (Lock)
        {
            Sinks.Add(sink);
        }
    }

    /// <summary>
    /// Registers a log sink by type. TUnit will instantiate it.
    /// </summary>
    public static void AddSink<TSink>() where TSink : ILogSink, new()
    {
        AddSink(new TSink());
    }

    internal static IReadOnlyList<ILogSink> GetSinks() => Sinks;

    internal static async ValueTask DisposeAllAsync()
    {
        foreach (var sink in Sinks)
        {
            if (sink is IAsyncDisposable disposable)
            {
                try
                {
                    await disposable.DisposeAsync();
                }
                catch
                {
                    // Swallow disposal errors
                }
            }
        }
        Sinks.Clear();
    }
}
```

### Routing Changes

#### DefaultLogger Modifications

```csharp
// In DefaultLogger.WriteToOutput / WriteToOutputAsync:

protected virtual void WriteToOutput(string message, bool isError)
{
    var level = isError ? LogLevel.Error : LogLevel.Information;

    // Historical capture (unchanged)
    if (isError)
        context.ErrorOutputWriter.WriteLine(message);
    else
        context.OutputWriter.WriteLine(message);

    // Real-time streaming to sinks (new)
    foreach (var sink in TUnitLoggerFactory.GetSinks())
    {
        if (!sink.IsEnabled(level))
            continue;

        try
        {
            sink.Log(level, message, exception: null, context);
        }
        catch (Exception ex)
        {
            GlobalContext.Current.OriginalConsoleError.WriteLine(
                $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}");
        }
    }
}

protected virtual async ValueTask WriteToOutputAsync(string message, bool isError)
{
    var level = isError ? LogLevel.Error : LogLevel.Information;

    // Historical capture (unchanged)
    if (isError)
        await context.ErrorOutputWriter.WriteLineAsync(message);
    else
        await context.OutputWriter.WriteLineAsync(message);

    // Real-time streaming to sinks (new)
    foreach (var sink in TUnitLoggerFactory.GetSinks())
    {
        if (!sink.IsEnabled(level))
            continue;

        try
        {
            await sink.LogAsync(level, message, exception: null, context);
        }
        catch (Exception ex)
        {
            await GlobalContext.Current.OriginalConsoleError.WriteLineAsync(
                $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}");
        }
    }
}
```

#### Console Interceptor Modifications

Route `Console.WriteLine` through sinks for real-time streaming:

```csharp
// In StandardOutConsoleInterceptor, after writing to context:

private void RouteToSinks(string? value)
{
    if (string.IsNullOrEmpty(value))
        return;

    var sinks = TUnitLoggerFactory.GetSinks();
    if (sinks.Count == 0)
        return;

    var context = Context.Current; // may be null outside test execution

    foreach (var sink in sinks)
    {
        if (!sink.IsEnabled(LogLevel.Information))
            continue;

        try
        {
            sink.Log(LogLevel.Information, value, exception: null, context);
        }
        catch (Exception ex)
        {
            // Write to original console to avoid recursion
            GlobalContext.Current.OriginalConsoleError.WriteLine(
                $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}");
        }
    }
}
```

### Engine's Built-in Sink (TUnit.Engine)

```csharp
namespace TUnit.Engine.Logging;

/// <summary>
/// Built-in sink that streams logs to IDEs via Microsoft Testing Platform's IOutputDevice.
/// Automatically registered by TUnit.Engine at startup.
/// </summary>
internal class OutputDeviceLogSink : ILogSink
{
    private readonly IOutputDevice _outputDevice;
    private readonly LogLevel _minLevel;

    public OutputDeviceLogSink(IOutputDevice outputDevice, LogLevel minLevel = LogLevel.Information)
    {
        _outputDevice = outputDevice;
        _minLevel = minLevel;
    }

    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        // Fire and forget for sync path - IOutputDevice is async-only
        _ = LogAsync(level, message, exception, context);
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!IsEnabled(level))
            return;

        var color = GetConsoleColor(level);

        await _outputDevice.DisplayAsync(
            this,
            new FormattedTextOutputDeviceData(message)
            {
                ForegroundColor = new SystemConsoleColor { ConsoleColor = color }
            },
            CancellationToken.None);
    }

    private static ConsoleColor GetConsoleColor(LogLevel level) => level switch
    {
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Debug => ConsoleColor.Gray,
        _ => ConsoleColor.White
    };
}
```

**Registration during test session initialization:**

```csharp
// In TUnitTestFramework or test session initialization:
var outputDevice = serviceProvider.GetRequiredService<IOutputDevice>();
TUnitLoggerFactory.AddSink(new OutputDeviceLogSink(outputDevice));
```

## Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  Test Code                                                      │
│  - TestContext.GetDefaultLogger().LogInformation("...")         │
│  - Console.WriteLine("...")                                     │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│  DefaultLogger / Console Interceptor                            │
│  1. Write to context.OutputWriter (historical capture)          │
│  2. Route to all registered ILogSink instances                  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
│ OutputDevice    │ │ User's Seq  │ │ User's File     │
│ LogSink         │ │ LogSink     │ │ LogSink         │
│ (built-in)      │ │ (custom)    │ │ (custom)        │
└────────┬────────┘ └──────┬──────┘ └────────┬────────┘
         │                 │                  │
         ▼                 ▼                  ▼
   IDE Real-time      Seq Server         Log File
```

## User Registration Example

```csharp
[assembly: Before(Assembly)]
public static class LoggingSetup
{
    public static Task BeforeAssembly()
    {
        // Add custom sinks
        TUnitLoggerFactory.AddSink(new SeqLogSink("http://localhost:5341"));
        TUnitLoggerFactory.AddSink<FileLogSink>();
        return Task.CompletedTask;
    }
}

// Example custom sink
public class FileLogSink : ILogSink, IAsyncDisposable
{
    private readonly StreamWriter _writer;

    public FileLogSink()
    {
        _writer = new StreamWriter("test-log.txt", append: true);
    }

    public bool IsEnabled(LogLevel level) => true;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        var testName = (context as TestContext)?.TestDetails.TestName ?? "N/A";
        _writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] [{testName}] {message}");
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        var testName = (context as TestContext)?.TestDetails.TestName ?? "N/A";
        await _writer.WriteLineAsync($"[{DateTime.Now:HH:mm:ss}] [{level}] [{testName}] {message}");
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
    }
}
```

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `TUnit.Core/Logging/ILogSink.cs` | Create | New sink interface |
| `TUnit.Core/Logging/TUnitLoggerFactory.cs` | Create | Sink registration |
| `TUnit.Core/Logging/DefaultLogger.cs` | Modify | Route to sinks |
| `TUnit.Core/Logging/StandardOutConsoleInterceptor.cs` | Modify | Route console to sinks |
| `TUnit.Engine/Logging/OutputDeviceLogSink.cs` | Create | Built-in IDE streaming sink |
| `TUnit.Engine/Services/TUnitTestFramework.cs` | Modify | Register OutputDeviceLogSink |

## Error Handling

- Sink failures are caught and logged to `OriginalConsoleError`
- Failures do not break tests or stop other sinks from receiving messages
- Disposal errors are swallowed during cleanup

## Backward Compatibility

- No breaking changes to existing APIs
- Historical capture via `context.OutputWriter` unchanged
- Existing behavior preserved if no custom sinks registered
- `OutputDeviceLogSink` registered automatically by Engine

## Future Considerations

- Built-in sinks package (file, JSON, etc.)
- Structured logging support with semantic properties
- Log level configuration per sink
- Async batching for high-throughput scenarios

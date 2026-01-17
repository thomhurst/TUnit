# Log Streaming Plugin System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable real-time log streaming to IDEs during test execution via a plugin-based ILogSink system.

**Architecture:** Introduce `ILogSink` interface in TUnit.Core that receives log messages. `TUnitLoggerFactory` manages sink registration. `DefaultLogger` and console interceptors route to all registered sinks. TUnit.Engine registers `OutputDeviceLogSink` at startup which uses `IOutputDevice.DisplayAsync()` for real-time IDE streaming.

**Tech Stack:** .NET, Microsoft Testing Platform, IOutputDevice

**Design Document:** `docs/plans/2026-01-17-log-streaming-design.md`

---

## Task 1: Create ILogSink Interface

**Files:**
- Create: `TUnit.Core/Logging/ILogSink.cs`

**Step 1: Create the interface file**

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

**Step 2: Verify it compiles**

Run: `dotnet build TUnit.Core/TUnit.Core.csproj -c Release`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add TUnit.Core/Logging/ILogSink.cs
git commit -m "feat(logging): add ILogSink interface for log destinations"
```

---

## Task 2: Create TUnitLoggerFactory

**Files:**
- Create: `TUnit.Core/Logging/TUnitLoggerFactory.cs`

**Step 1: Create the factory class**

```csharp
namespace TUnit.Core.Logging;

/// <summary>
/// Factory for configuring and managing log sinks.
/// </summary>
public static class TUnitLoggerFactory
{
    private static readonly List<ILogSink> _sinks = [];
    private static readonly Lock _lock = new();

    /// <summary>
    /// Registers a log sink to receive log messages.
    /// Call this in [Before(Assembly)] or before tests run.
    /// </summary>
    public static void AddSink(ILogSink sink)
    {
        lock (_lock)
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// Registers a log sink by type. TUnit will instantiate it.
    /// </summary>
    public static void AddSink<TSink>() where TSink : ILogSink, new()
    {
        AddSink(new TSink());
    }

    /// <summary>
    /// Gets all registered sinks. For internal use.
    /// </summary>
    internal static IReadOnlyList<ILogSink> GetSinks()
    {
        lock (_lock)
        {
            return _sinks.ToArray();
        }
    }

    /// <summary>
    /// Disposes all sinks that implement IAsyncDisposable.
    /// Called at end of test session.
    /// </summary>
    internal static async ValueTask DisposeAllAsync()
    {
        ILogSink[] sinksToDispose;
        lock (_lock)
        {
            sinksToDispose = _sinks.ToArray();
            _sinks.Clear();
        }

        foreach (var sink in sinksToDispose)
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
    }

    /// <summary>
    /// Clears all registered sinks. For testing purposes.
    /// </summary>
    internal static void Clear()
    {
        lock (_lock)
        {
            _sinks.Clear();
        }
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build TUnit.Core/TUnit.Core.csproj -c Release`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add TUnit.Core/Logging/TUnitLoggerFactory.cs
git commit -m "feat(logging): add TUnitLoggerFactory for sink registration"
```

---

## Task 3: Add Internal Sink Routing Helper

**Files:**
- Create: `TUnit.Core/Logging/LogSinkRouter.cs`

**Step 1: Create router helper to avoid code duplication**

```csharp
namespace TUnit.Core.Logging;

/// <summary>
/// Internal helper for routing log messages to all registered sinks.
/// </summary>
internal static class LogSinkRouter
{
    public static void RouteToSinks(LogLevel level, string message, Exception? exception, Context? context)
    {
        var sinks = TUnitLoggerFactory.GetSinks();
        if (sinks.Count == 0)
        {
            return;
        }

        foreach (var sink in sinks)
        {
            if (!sink.IsEnabled(level))
            {
                continue;
            }

            try
            {
                sink.Log(level, message, exception, context);
            }
            catch (Exception ex)
            {
                // Write to original console to avoid recursion
                GlobalContext.Current.OriginalConsoleError.WriteLine(
                    $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}");
            }
        }
    }

    public static async ValueTask RouteToSinksAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        var sinks = TUnitLoggerFactory.GetSinks();
        if (sinks.Count == 0)
        {
            return;
        }

        foreach (var sink in sinks)
        {
            if (!sink.IsEnabled(level))
            {
                continue;
            }

            try
            {
                await sink.LogAsync(level, message, exception, context);
            }
            catch (Exception ex)
            {
                // Write to original console to avoid recursion
                await GlobalContext.Current.OriginalConsoleError.WriteLineAsync(
                    $"[TUnit] Log sink {sink.GetType().Name} failed: {ex.Message}");
            }
        }
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build TUnit.Core/TUnit.Core.csproj -c Release`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add TUnit.Core/Logging/LogSinkRouter.cs
git commit -m "feat(logging): add LogSinkRouter helper for sink dispatch"
```

---

## Task 4: Modify DefaultLogger to Route to Sinks

**Files:**
- Modify: `TUnit.Core/Logging/DefaultLogger.cs`

**Step 1: Update WriteToOutput to route to sinks**

Find the `WriteToOutput` method (around line 125) and replace with:

```csharp
/// <summary>
/// Writes the message to the output.
/// Override this method to customize how messages are written.
/// </summary>
/// <param name="message">The formatted message to write.</param>
/// <param name="isError">True if this is an error-level message.</param>
protected virtual void WriteToOutput(string message, bool isError)
{
    var level = isError ? LogLevel.Error : LogLevel.Information;

    // Historical capture
    if (isError)
    {
        context.ErrorOutputWriter.WriteLine(message);
    }
    else
    {
        context.OutputWriter.WriteLine(message);
    }

    // Real-time streaming to sinks
    LogSinkRouter.RouteToSinks(level, message, null, context);
}
```

**Step 2: Update WriteToOutputAsync to route to sinks**

Find the `WriteToOutputAsync` method (around line 146) and replace with:

```csharp
/// <summary>
/// Asynchronously writes the message to the output.
/// Override this method to customize how messages are written.
/// </summary>
/// <param name="message">The formatted message to write.</param>
/// <param name="isError">True if this is an error-level message.</param>
/// <returns>A task representing the async operation.</returns>
protected virtual async ValueTask WriteToOutputAsync(string message, bool isError)
{
    var level = isError ? LogLevel.Error : LogLevel.Information;

    // Historical capture
    if (isError)
    {
        await context.ErrorOutputWriter.WriteLineAsync(message);
    }
    else
    {
        await context.OutputWriter.WriteLineAsync(message);
    }

    // Real-time streaming to sinks
    await LogSinkRouter.RouteToSinksAsync(level, message, null, context);
}
```

**Step 3: Verify it compiles**

Run: `dotnet build TUnit.Core/TUnit.Core.csproj -c Release`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add TUnit.Core/Logging/DefaultLogger.cs
git commit -m "feat(logging): route DefaultLogger output to registered sinks"
```

---

## Task 5: Modify Console Interceptor to Route to Sinks

**Files:**
- Modify: `TUnit.Engine/Logging/StandardOutConsoleInterceptor.cs`

**Step 1: Find the interceptor and understand its structure**

Read the file first to understand how it intercepts console output.

Run: Read `TUnit.Engine/Logging/StandardOutConsoleInterceptor.cs`

**Step 2: Add sink routing after console capture**

The interceptor likely has Write/WriteLine methods that capture output. Add routing to sinks after capturing. The exact modification depends on the file structure, but the pattern is:

After any line that writes to the context's output (like `Context.Current?.OutputWriter?.WriteLine(value)`), add:

```csharp
// Route to sinks for real-time streaming
LogSinkRouter.RouteToSinks(LogLevel.Information, value?.ToString() ?? string.Empty, null, Context.Current);
```

**Step 3: Add using statement if needed**

Add at top of file:
```csharp
using TUnit.Core.Logging;
```

**Step 4: Verify it compiles**

Run: `dotnet build TUnit.Engine/TUnit.Engine.csproj -c Release`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add TUnit.Engine/Logging/StandardOutConsoleInterceptor.cs
git commit -m "feat(logging): route Console.WriteLine to registered sinks"
```

---

## Task 6: Modify Console Error Interceptor (if separate)

**Files:**
- Modify: `TUnit.Engine/Logging/StandardErrorConsoleInterceptor.cs` (if exists)

**Step 1: Check if file exists and apply same pattern**

If there's a separate error interceptor, apply the same changes as Task 5 but use `LogLevel.Error`:

```csharp
LogSinkRouter.RouteToSinks(LogLevel.Error, value?.ToString() ?? string.Empty, null, Context.Current);
```

**Step 2: Verify it compiles**

Run: `dotnet build TUnit.Engine/TUnit.Engine.csproj -c Release`
Expected: Build succeeded

**Step 3: Commit (if changes made)**

```bash
git add TUnit.Engine/Logging/StandardErrorConsoleInterceptor.cs
git commit -m "feat(logging): route Console.Error to registered sinks"
```

---

## Task 7: Create OutputDeviceLogSink in TUnit.Engine

**Files:**
- Create: `TUnit.Engine/Logging/OutputDeviceLogSink.cs`

**Step 1: Create the sink that streams to IDEs**

```csharp
using Microsoft.Testing.Platform.OutputDevice;
using TUnit.Core;
using TUnit.Core.Logging;

namespace TUnit.Engine.Logging;

/// <summary>
/// Built-in sink that streams logs to IDEs via Microsoft Testing Platform's IOutputDevice.
/// Automatically registered by TUnit.Engine at startup.
/// </summary>
internal class OutputDeviceLogSink : ILogSink, IOutputDeviceDataProducer
{
    private readonly IOutputDevice _outputDevice;
    private readonly LogLevel _minLevel;

    public OutputDeviceLogSink(IOutputDevice outputDevice, LogLevel minLevel = LogLevel.Information)
    {
        _outputDevice = outputDevice;
        _minLevel = minLevel;
    }

    public string Uid => "TUnit.OutputDeviceLogSink";
    public string Version => typeof(OutputDeviceLogSink).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    public string DisplayName => "TUnit Log Sink";
    public string Description => "Streams test logs to IDE in real-time";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    public void Log(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        // Fire and forget for sync path - IOutputDevice is async-only
        _ = LogAsync(level, message, exception, context);
    }

    public async ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        try
        {
            var color = GetConsoleColor(level);

            await _outputDevice.DisplayAsync(
                this,
                new FormattedTextOutputDeviceData(message)
                {
                    ForegroundColor = new SystemConsoleColor { ConsoleColor = color }
                },
                CancellationToken.None);
        }
        catch
        {
            // Swallow errors - logging should not break tests
        }
    }

    private static ConsoleColor GetConsoleColor(LogLevel level) => level switch
    {
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Debug => ConsoleColor.Gray,
        LogLevel.Trace => ConsoleColor.DarkGray,
        _ => ConsoleColor.White
    };
}
```

**Step 2: Verify it compiles**

Run: `dotnet build TUnit.Engine/TUnit.Engine.csproj -c Release`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add TUnit.Engine/Logging/OutputDeviceLogSink.cs
git commit -m "feat(logging): add OutputDeviceLogSink for real-time IDE streaming"
```

---

## Task 8: Register OutputDeviceLogSink at Startup

**Files:**
- Modify: Find the test framework initialization (likely `TUnit.Engine/Services/TUnitTestFramework.cs` or similar)

**Step 1: Find where IOutputDevice is available**

Search for where `IOutputDevice` is injected or retrieved from the service provider.

Run: `grep -r "IOutputDevice" TUnit.Engine/ --include="*.cs"`

**Step 2: Register the sink during initialization**

At the point where `IOutputDevice` is available (likely in a constructor or initialization method), add:

```csharp
// Register the built-in sink for real-time IDE streaming
var outputDeviceSink = new OutputDeviceLogSink(outputDevice);
TUnitLoggerFactory.AddSink(outputDeviceSink);
```

Add using statement:
```csharp
using TUnit.Core.Logging;
```

**Step 3: Verify it compiles**

Run: `dotnet build TUnit.Engine/TUnit.Engine.csproj -c Release`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add TUnit.Engine/Services/*.cs
git commit -m "feat(logging): register OutputDeviceLogSink at test session startup"
```

---

## Task 9: Dispose Sinks at Session End

**Files:**
- Modify: Find session cleanup code (likely `TUnit.Engine/Services/TUnitTestFramework.cs` or `OnTestSessionFinishing` handler)

**Step 1: Find session end hook**

Search for cleanup or disposal code:

Run: `grep -r "OnTestSessionFinishing\|Dispose\|Cleanup" TUnit.Engine/Services/ --include="*.cs"`

**Step 2: Add sink disposal**

At session end, add:

```csharp
await TUnitLoggerFactory.DisposeAllAsync();
```

**Step 3: Verify it compiles**

Run: `dotnet build TUnit.Engine/TUnit.Engine.csproj -c Release`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add TUnit.Engine/Services/*.cs
git commit -m "feat(logging): dispose sinks at test session end"
```

---

## Task 10: Write Unit Tests for TUnitLoggerFactory

**Files:**
- Create: `TUnit.UnitTests/LogSinkTests.cs`

**Step 1: Create test file with basic tests**

```csharp
using TUnit.Core.Logging;

namespace TUnit.UnitTests;

public class LogSinkTests
{
    [Before(Test)]
    public void Setup()
    {
        TUnitLoggerFactory.Clear();
    }

    [After(Test)]
    public void Cleanup()
    {
        TUnitLoggerFactory.Clear();
    }

    [Test]
    public void AddSink_RegistersSink()
    {
        // Arrange
        var sink = new TestLogSink();

        // Act
        TUnitLoggerFactory.AddSink(sink);

        // Assert
        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).Contains(sink);
    }

    [Test]
    public void AddSink_Generic_CreatesSinkInstance()
    {
        // Act
        TUnitLoggerFactory.AddSink<TestLogSink>();

        // Assert
        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).HasCount().EqualTo(1);
        await Assert.That(sinks[0]).IsTypeOf<TestLogSink>();
    }

    [Test]
    public async Task DisposeAllAsync_DisposesAsyncDisposableSinks()
    {
        // Arrange
        var sink = new DisposableTestLogSink();
        TUnitLoggerFactory.AddSink(sink);

        // Act
        await TUnitLoggerFactory.DisposeAllAsync();

        // Assert
        await Assert.That(sink.WasDisposed).IsTrue();
        await Assert.That(TUnitLoggerFactory.GetSinks()).IsEmpty();
    }

    [Test]
    public void Clear_RemovesAllSinks()
    {
        // Arrange
        TUnitLoggerFactory.AddSink(new TestLogSink());
        TUnitLoggerFactory.AddSink(new TestLogSink());

        // Act
        TUnitLoggerFactory.Clear();

        // Assert
        await Assert.That(TUnitLoggerFactory.GetSinks()).IsEmpty();
    }

    private class TestLogSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
            Messages.Add(message);
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            Messages.Add(message);
            return ValueTask.CompletedTask;
        }
    }

    private class DisposableTestLogSink : TestLogSink, IAsyncDisposable
    {
        public bool WasDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
```

**Step 2: Run tests**

Run: `dotnet run --project TUnit.UnitTests/TUnit.UnitTests.csproj -c Release --no-build -f net8.0 -- --treenode-filter "/*/*/LogSinkTests/*"`
Expected: All tests pass

**Step 3: Commit**

```bash
git add TUnit.UnitTests/LogSinkTests.cs
git commit -m "test(logging): add unit tests for TUnitLoggerFactory"
```

---

## Task 11: Write Unit Tests for LogSinkRouter

**Files:**
- Modify: `TUnit.UnitTests/LogSinkTests.cs`

**Step 1: Add router tests to the test file**

```csharp
public class LogSinkRouterTests
{
    [Before(Test)]
    public void Setup()
    {
        TUnitLoggerFactory.Clear();
    }

    [After(Test)]
    public void Cleanup()
    {
        TUnitLoggerFactory.Clear();
    }

    [Test]
    public void RouteToSinks_SendsMessageToAllEnabledSinks()
    {
        // Arrange
        var sink1 = new TestLogSink();
        var sink2 = new TestLogSink();
        TUnitLoggerFactory.AddSink(sink1);
        TUnitLoggerFactory.AddSink(sink2);

        // Act
        LogSinkRouter.RouteToSinks(LogLevel.Information, "test message", null, null);

        // Assert
        await Assert.That(sink1.Messages).Contains("test message");
        await Assert.That(sink2.Messages).Contains("test message");
    }

    [Test]
    public void RouteToSinks_SkipsDisabledSinks()
    {
        // Arrange
        var enabledSink = new TestLogSink();
        var disabledSink = new TestLogSink { MinLevel = LogLevel.Error };
        TUnitLoggerFactory.AddSink(enabledSink);
        TUnitLoggerFactory.AddSink(disabledSink);

        // Act
        LogSinkRouter.RouteToSinks(LogLevel.Information, "test message", null, null);

        // Assert
        await Assert.That(enabledSink.Messages).Contains("test message");
        await Assert.That(disabledSink.Messages).IsEmpty();
    }

    [Test]
    public void RouteToSinks_ContinuesAfterSinkFailure()
    {
        // Arrange
        var failingSink = new FailingLogSink();
        var workingSink = new TestLogSink();
        TUnitLoggerFactory.AddSink(failingSink);
        TUnitLoggerFactory.AddSink(workingSink);

        // Act - should not throw
        LogSinkRouter.RouteToSinks(LogLevel.Information, "test message", null, null);

        // Assert - working sink still received message
        await Assert.That(workingSink.Messages).Contains("test message");
    }

    private class TestLogSink : ILogSink
    {
        public List<string> Messages { get; } = [];
        public LogLevel MinLevel { get; set; } = LogLevel.Trace;

        public bool IsEnabled(LogLevel level) => level >= MinLevel;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
            Messages.Add(message);
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            Messages.Add(message);
            return ValueTask.CompletedTask;
        }
    }

    private class FailingLogSink : ILogSink
    {
        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
            throw new InvalidOperationException("Sink failed");
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            throw new InvalidOperationException("Sink failed");
        }
    }
}
```

**Step 2: Run tests**

Run: `dotnet run --project TUnit.UnitTests/TUnit.UnitTests.csproj -c Release -f net8.0 -- --treenode-filter "/*/*/LogSinkRouterTests/*"`
Expected: All tests pass

**Step 3: Commit**

```bash
git add TUnit.UnitTests/LogSinkTests.cs
git commit -m "test(logging): add unit tests for LogSinkRouter"
```

---

## Task 12: Integration Test - End to End

**Files:**
- Create: `TUnit.UnitTests/LogStreamingIntegrationTests.cs`

**Step 1: Create integration test**

```csharp
using TUnit.Core.Logging;

namespace TUnit.UnitTests;

public class LogStreamingIntegrationTests
{
    [Before(Test)]
    public void Setup()
    {
        TUnitLoggerFactory.Clear();
    }

    [After(Test)]
    public void Cleanup()
    {
        TUnitLoggerFactory.Clear();
    }

    [Test]
    public async Task DefaultLogger_RoutesToRegisteredSinks()
    {
        // Arrange
        var captureSink = new CapturingLogSink();
        TUnitLoggerFactory.AddSink(captureSink);

        var testContext = TestContext.Current;
        var logger = testContext!.GetDefaultLogger();

        // Act
        await logger.LogInformationAsync("Hello from test");

        // Assert
        await Assert.That(captureSink.Messages).Contains(m => m.Contains("Hello from test"));
    }

    private class CapturingLogSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
            Messages.Add(message);
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            Messages.Add(message);
            return ValueTask.CompletedTask;
        }
    }
}
```

**Step 2: Run test**

Run: `dotnet run --project TUnit.UnitTests/TUnit.UnitTests.csproj -c Release -f net8.0 -- --treenode-filter "/*/*/LogStreamingIntegrationTests/*"`
Expected: Test passes

**Step 3: Commit**

```bash
git add TUnit.UnitTests/LogStreamingIntegrationTests.cs
git commit -m "test(logging): add integration test for log streaming"
```

---

## Task 13: Run Full Test Suite

**Files:** None (verification only)

**Step 1: Build entire solution**

Run: `dotnet build TUnit.sln -c Release`
Expected: Build succeeded

**Step 2: Run unit tests**

Run: `dotnet run --project TUnit.UnitTests/TUnit.UnitTests.csproj -c Release --no-build -f net8.0`
Expected: All tests pass

**Step 3: Run analyzer tests**

Run: `dotnet run --project TUnit.Analyzers.Tests/TUnit.Analyzers.Tests.csproj -c Release -f net8.0`
Expected: All tests pass

---

## Task 14: Update Public API Surface (if using PublicAPI analyzers)

**Files:**
- Modify: `TUnit.Core/PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt`

**Step 1: Check if public API tracking is used**

Run: `ls TUnit.Core/PublicAPI*.txt 2>/dev/null || echo "No PublicAPI files"`

**Step 2: If files exist, add new public types**

Add to `PublicAPI.Unshipped.txt`:
```
TUnit.Core.Logging.ILogSink
TUnit.Core.Logging.ILogSink.IsEnabled(TUnit.Core.Logging.LogLevel) -> bool
TUnit.Core.Logging.ILogSink.Log(TUnit.Core.Logging.LogLevel, string!, System.Exception?, TUnit.Core.Context?) -> void
TUnit.Core.Logging.ILogSink.LogAsync(TUnit.Core.Logging.LogLevel, string!, System.Exception?, TUnit.Core.Context?) -> System.Threading.Tasks.ValueTask
TUnit.Core.Logging.TUnitLoggerFactory
TUnit.Core.Logging.TUnitLoggerFactory.AddSink(TUnit.Core.Logging.ILogSink!) -> void
TUnit.Core.Logging.TUnitLoggerFactory.AddSink<TSink>() -> void
```

**Step 3: Commit**

```bash
git add TUnit.Core/PublicAPI*.txt
git commit -m "docs: update public API surface for log sink types"
```

---

## Task 15: Final Verification and Squash (Optional)

**Step 1: Verify all tests pass**

Run: `dotnet run --project TUnit.UnitTests/TUnit.UnitTests.csproj -c Release -f net8.0`
Expected: All tests pass including new log sink tests

**Step 2: Review git log**

Run: `git log --oneline -15`

**Step 3: Create final summary commit or squash if desired**

If keeping granular commits:
```bash
git push -u origin feature/log-streaming
```

If squashing:
```bash
git rebase -i main
# Squash commits as desired
git push -u origin feature/log-streaming
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 1 | Create ILogSink interface | `TUnit.Core/Logging/ILogSink.cs` |
| 2 | Create TUnitLoggerFactory | `TUnit.Core/Logging/TUnitLoggerFactory.cs` |
| 3 | Create LogSinkRouter helper | `TUnit.Core/Logging/LogSinkRouter.cs` |
| 4 | Modify DefaultLogger | `TUnit.Core/Logging/DefaultLogger.cs` |
| 5-6 | Modify Console Interceptors | `TUnit.Engine/Logging/Standard*ConsoleInterceptor.cs` |
| 7 | Create OutputDeviceLogSink | `TUnit.Engine/Logging/OutputDeviceLogSink.cs` |
| 8 | Register sink at startup | `TUnit.Engine/Services/*.cs` |
| 9 | Dispose sinks at session end | `TUnit.Engine/Services/*.cs` |
| 10-12 | Write tests | `TUnit.UnitTests/LogSink*.cs` |
| 13 | Full test suite verification | - |
| 14 | Update public API | `TUnit.Core/PublicAPI*.txt` |
| 15 | Final verification | - |

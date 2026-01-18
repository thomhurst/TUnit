# IDE Streaming Sink Design

**Date:** 2026-01-18
**Issue:** [#4495](https://github.com/thomhurst/TUnit/issues/4495)
**Status:** Approved

## Overview

Implement real-time IDE output streaming via a new `IdeStreamingSink` that sends test output to IDEs during test execution, not just at completion.

## Background

PR #4493 introduced an extensible log sink framework (`ILogSink`, `TUnitLoggerFactory`). A previous attempt at IDE streaming was removed due to output duplication issues. This design takes a simpler approach.

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Duplication handling** | Cumulative replacement | Each update sends full output; IDE displays latest. Simple, no coordination needed. |
| **Throttling** | 1 second intervals | Prevents flooding IDE with rapid writes. Uses latest snapshot per interval. |
| **Output types** | Both stdout and stderr | Stream `StandardOutputProperty` and `StandardErrorProperty` |
| **Activation** | On by default for IDE | Uses existing `VerbosityService.IsIdeClient` detection |

## Architecture

### Data Flow

```
Console.WriteLine()
  -> StandardOutConsoleInterceptor
    -> LogSinkRouter routes to all sinks:
        -> TestOutputSink: accumulates to Context.OutputWriter (always)
        -> IdeStreamingSink: marks test as "dirty" (IDE only)
            -> Timer fires every 1s per test
            -> GetStandardOutput() + GetErrorOutput()
            -> Send TestNodeUpdateMessage with InProgressTestNodeStateProperty
```

### Class Structure

```csharp
internal sealed class IdeStreamingSink : ILogSink, IAsyncDisposable
{
    private readonly TUnitMessageBus _messageBus;
    private readonly SessionUid _sessionUid;
    private readonly ConcurrentDictionary<string, TestStreamingState> _activeTests = new();
    private readonly TimeSpan _throttleInterval = TimeSpan.FromSeconds(1);
}

private sealed class TestStreamingState : IDisposable
{
    public TestContext TestContext { get; }
    public Timer Timer { get; }
    public bool IsDirty { get; set; }  // Has new output since last send
}
```

### Core Logic

1. **On `Log()` call with `TestContext`:**
   - Get or create `TestStreamingState` for this test
   - Mark as dirty (`IsDirty = true`)
   - Timer is already running (started on first log)

2. **On timer tick (every 1 second):**
   - Check if test completed (passive cleanup) - if so, dispose and remove
   - If `IsDirty` is false, skip (no new output)
   - Set `IsDirty = false`
   - Call `testContext.GetStandardOutput()` and `GetErrorOutput()`
   - Send `TestNodeUpdateMessage` with `InProgressTestNodeStateProperty` + output properties

3. **On dispose:** Cancel all timers, clear dictionary

### TestNode Creation

```csharp
private TestNode CreateOutputUpdateNode(TestContext testContext, string? output, string? error)
{
    var properties = new List<IProperty>
    {
        InProgressTestNodeStateProperty.CachedInstance
    };

    if (!string.IsNullOrEmpty(output))
        properties.Add(new StandardOutputProperty(output));

    if (!string.IsNullOrEmpty(error))
        properties.Add(new StandardErrorProperty(error));

    return new TestNode
    {
        Uid = new TestNodeUid(testContext.TestDetails.TestId),
        DisplayName = testContext.GetDisplayName(),
        Properties = new PropertyBag(properties)
    };
}
```

### Registration

In `TUnitServiceProvider.cs`:

```csharp
// After existing sink registrations
if (VerbosityService.IsIdeClient)
{
    TUnitLoggerFactory.AddSink(new IdeStreamingSink(
        MessageBus,
        context.Request.Session.SessionUid));
}
```

## Files to Modify

| File | Change |
|------|--------|
| `TUnit.Engine/Logging/IdeStreamingSink.cs` | **Create** - new sink implementation |
| `TUnit.Engine/Framework/TUnitServiceProvider.cs` | **Modify** - register sink for IDE clients |

## Testing Strategy

1. Manual testing in Visual Studio and Rider
2. Verify output appears during long-running tests
3. Verify no duplication at test completion
4. Verify console mode is unaffected

## Related

- PR #4493 - Extensible log sink architecture
- Issue #4478 - Original user feature request

---
sidebar_position: 4
---

# Programmatic Configuration

## Overview

The `context.Settings` API lets you configure TUnit settings directly in code. This is useful when you want discoverable, version-controlled defaults for your test suite without relying on command-line flags or environment variables.

Settings are organized into logical groups:

- `Timeouts` — test and hook timeout durations
- `Parallelism` — concurrent test execution limits
- `Execution` — runtime behavior such as fail-fast
- `Display` — output and display options

## Usage

Set values inside a `[Before(HookType.TestDiscovery)]` hook so they are applied before any tests are discovered or executed. The `context.Settings` property provides direct access:

```csharp
using TUnit.Core;

public class TestSetup
{
    [Before(HookType.TestDiscovery)]
    public static Task Configure(BeforeTestDiscoveryContext context)
    {
        context.Settings.Timeouts.DefaultTestTimeout = TimeSpan.FromMinutes(5);
        context.Settings.Timeouts.DefaultHookTimeout = TimeSpan.FromMinutes(2);
        context.Settings.Execution.FailFast = true;

        return Task.CompletedTask;
    }
}
```

Place this class anywhere in your test project. TUnit will discover and run the hook automatically.

Settings are accessed exclusively through `context.Settings` in the discovery hook, which ensures they are configured at the correct point in the TUnit lifecycle.

## Settings Reference

### `context.Settings.Timeouts`

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultTestTimeout` | `TimeSpan` | 30 minutes | Maximum duration for a single test before it is cancelled. |
| `DefaultHookTimeout` | `TimeSpan` | 5 minutes | Maximum duration for a single hook (`[Before]`/`[After]`) before it is cancelled. |
| `ForcefulExitTimeout` | `TimeSpan` | 30 seconds | Grace period before the process is forcefully terminated after a cancellation. |
| `ProcessExitHookDelay` | `TimeSpan` | 500 ms | Delay before process-exit hooks run, allowing pending I/O to flush. |

### `context.Settings.Parallelism`

| Property | Type | Default | Description |
|---|---|---|---|
| `MaximumParallelTests` | `int?` | `null` (4 x CPU cores) | Maximum number of tests that can execute concurrently. Set to `null` to use the default heuristic. |

### `context.Settings.Display`

| Property | Type | Default | Description |
|---|---|---|---|
| `DetailedStackTrace` | `bool` | `false` | Includes TUnit internal frames in stack traces. By default, internal frames are hidden to keep failure output focused on user code. |

### `context.Settings.Execution`

| Property | Type | Default | Description |
|---|---|---|---|
| `FailFast` | `bool` | `false` | Cancels the remaining test run after the first test failure. |

## Precedence

When the same setting is configured in multiple places, the following priority order applies (highest wins):

1. **Command-line flag** (e.g., `--maximum-parallel-tests 8`)
2. **Environment variable** (e.g., `TUNIT_MAX_PARALLEL_TESTS=8`)
3. **`context.Settings` (code)** — values set in a `[Before(HookType.TestDiscovery)]` hook
4. **Built-in default**

### Example

Your test project sets a conservative parallelism limit in code:

```csharp
context.Settings.Parallelism.MaximumParallelTests = 1;
```

A developer on a powerful machine can override this for a local run without changing code:

```bash
dotnet run --project MyTests -- --maximum-parallel-tests 8
```

The command-line flag takes precedence, so 8 parallel tests will be used.

## When to Set

Set most values via `context.Settings` inside a `[Before(HookType.TestDiscovery)]` hook. This is the earliest point in the TUnit lifecycle where user code runs and ensures your values are in place before test discovery begins. Setting values later (for example in a `[Before(HookType.TestSession)]` hook) may have no effect for settings that are read during discovery.

The exception is **`Timeouts.DefaultHookTimeout`**, which is captured at hook registration time before discovery hooks run. Use the `[Timeout]` attribute on individual hook methods for reliable per-hook timeout control.

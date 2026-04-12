---
sidebar_position: 4
---

# Programmatic Configuration

## Overview

The `TUnitSettings` API lets you configure TUnit settings directly in code. This is useful when you want discoverable, version-controlled defaults for your test suite without relying on command-line flags or environment variables.

Settings are organized into logical groups:

- `TUnitSettings.Timeouts` — test and hook timeout durations
- `TUnitSettings.Parallelism` — concurrent test execution limits
- `TUnitSettings.Execution` — runtime behavior such as fail-fast
- `TUnitSettings.Display` — output and display options

## Usage

Set values inside a `[Before(HookType.TestDiscovery)]` hook so they are applied before any tests are discovered or executed:

```csharp
using TUnit.Core;
using TUnit.Core.Settings;

public class TestSetup
{
    [Before(HookType.TestDiscovery)]
    public static Task Configure(BeforeTestDiscoveryContext context)
    {
        TUnitSettings.Timeouts.DefaultTestTimeout = TimeSpan.FromMinutes(5);
        TUnitSettings.Timeouts.DefaultHookTimeout = TimeSpan.FromMinutes(2);
        TUnitSettings.Parallelism.MaximumParallelTests = 4;
        TUnitSettings.Execution.FailFast = true;

        return Task.CompletedTask;
    }
}
```

Place this class anywhere in your test project. TUnit will discover and run the hook automatically.

## Settings Reference

### `TUnitSettings.Timeouts`

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultTestTimeout` | `TimeSpan` | 30 minutes | Maximum duration for a single test before it is cancelled. |
| `DefaultHookTimeout` | `TimeSpan` | 5 minutes | Maximum duration for a single hook (`[Before]`/`[After]`) before it is cancelled. |
| `ForcefulExitTimeout` | `TimeSpan` | 30 seconds | Grace period before the process is forcefully terminated after a cancellation. |
| `ProcessExitHookDelay` | `TimeSpan` | 500 ms | Delay before process-exit hooks run, allowing pending I/O to flush. |

### `TUnitSettings.Parallelism`

| Property | Type | Default | Description |
|---|---|---|---|
| `MaximumParallelTests` | `int?` | `null` (4 x CPU cores) | Maximum number of tests that can execute concurrently. Set to `null` to use the default heuristic. |

### `TUnitSettings.Display`

| Property | Type | Default | Description |
|---|---|---|---|
| `DetailedStackTrace` | `bool` | `false` | Includes TUnit internal frames in stack traces. By default, internal frames are hidden to keep failure output focused on user code. |

### `TUnitSettings.Execution`

| Property | Type | Default | Description |
|---|---|---|---|
| `FailFast` | `bool` | `false` | Cancels the remaining test run after the first test failure. |

## Precedence

When the same setting is configured in multiple places, the following priority order applies (highest wins):

1. **Command-line flag** (e.g., `--maximum-parallel-tests 8`)
2. **Environment variable** (e.g., `TUNIT_MAX_PARALLEL_TESTS=8`)
3. **`TUnitSettings` (code)** — values set in a `[Before(HookType.TestDiscovery)]` hook
4. **Built-in default**

### Example

Your test project sets a conservative parallelism limit in code:

```csharp
TUnitSettings.Parallelism.MaximumParallelTests = 1;
```

A developer on a powerful machine can override this for a local run without changing code:

```bash
dotnet run --project MyTests -- --maximum-parallel-tests 8
```

The command-line flag takes precedence, so 8 parallel tests will be used.

## When to Set

Always set `TUnitSettings` values inside a `[Before(HookType.TestDiscovery)]` hook. This is the earliest point in the TUnit lifecycle and ensures your values are in place before test discovery begins. Setting values later (for example in a `[Before(HookType.TestSession)]` hook) may have no effect for settings that are read during discovery.

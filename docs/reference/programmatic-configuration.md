# Programmatic Configuration

## Overview[​](#overview "Direct link to Overview")

The `context.Settings` API lets you configure TUnit settings directly in code. This is useful when you want discoverable, version-controlled defaults for your test suite without relying on command-line flags or environment variables.

Settings are organized into logical groups:

* `Timeouts` — test and hook timeout durations
* `Parallelism` — concurrent test execution limits
* `Execution` — runtime behavior such as fail-fast
* `Display` — output and display options
* `Reporting` — HTML report generation and publishing
* `Mocks` — defaults for TUnit.Mocks when the package is referenced

## Usage[​](#usage "Direct link to Usage")

Set values inside a `[Before(HookType.TestDiscovery)]` hook so they are applied before any tests are discovered or executed. The `context.Settings` property provides direct access:

```
using TUnit.Core;

using TUnit.Mocks;



public class TestSetup

{

    [Before(HookType.TestDiscovery)]

    public static Task Configure(BeforeTestDiscoveryContext context)

    {

        context.Settings.Timeouts.DefaultTestTimeout = TimeSpan.FromMinutes(5);

        context.Settings.Timeouts.DefaultHookTimeout = TimeSpan.FromMinutes(2);

        context.Settings.Execution.FailFast = true;

        context.Settings.Reporting.HtmlReportEnabled = false;

        context.Settings.Mocks.DefaultMode = MockBehavior.Strict;



        return Task.CompletedTask;

    }

}
```

Place this class anywhere in your test project. TUnit will discover and run the hook automatically.

Settings are accessed exclusively through `context.Settings` in the discovery hook, which ensures they are configured at the correct point in the TUnit lifecycle.

## Settings Reference[​](#settings-reference "Direct link to Settings Reference")

### `context.Settings.Timeouts`[​](#contextsettingstimeouts "Direct link to contextsettingstimeouts")

| Property               | Type       | Default    | Description                                                                       |
| ---------------------- | ---------- | ---------- | --------------------------------------------------------------------------------- |
| `DefaultTestTimeout`   | `TimeSpan` | 30 minutes | Maximum duration for a single test before it is cancelled.                        |
| `DefaultHookTimeout`   | `TimeSpan` | 5 minutes  | Maximum duration for a single hook (`[Before]`/`[After]`) before it is cancelled. |
| `ForcefulExitTimeout`  | `TimeSpan` | 30 seconds | Grace period before the process is forcefully terminated after a cancellation.    |
| `ProcessExitHookDelay` | `TimeSpan` | 500 ms     | Delay before process-exit hooks run, allowing pending I/O to flush.               |

### `context.Settings.Parallelism`[​](#contextsettingsparallelism "Direct link to contextsettingsparallelism")

| Property               | Type   | Default                | Description                                                                                        |
| ---------------------- | ------ | ---------------------- | -------------------------------------------------------------------------------------------------- |
| `MaximumParallelTests` | `int?` | `null` (4 x CPU cores) | Maximum number of tests that can execute concurrently. Set to `null` to use the default heuristic. |

### `context.Settings.Display`[​](#contextsettingsdisplay "Direct link to contextsettingsdisplay")

| Property             | Type   | Default | Description                                                                                                                         |
| -------------------- | ------ | ------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `DetailedStackTrace` | `bool` | `false` | Includes TUnit internal frames in stack traces. By default, internal frames are hidden to keep failure output focused on user code. |

### `context.Settings.Execution`[​](#contextsettingsexecution "Direct link to contextsettingsexecution")

| Property   | Type   | Default | Description                                                  |
| ---------- | ------ | ------- | ------------------------------------------------------------ |
| `FailFast` | `bool` | `false` | Cancels the remaining test run after the first test failure. |

### `context.Settings.Reporting`[​](#contextsettingsreporting "Direct link to contextsettingsreporting")

| Property                | Type   | Default | Description                                                                  |
| ----------------------- | ------ | ------- | ---------------------------------------------------------------------------- |
| `HtmlReportEnabled`     | `bool` | `true`  | Generates the HTML test report.                                              |
| `JsonReportEnabled`     | `bool` | `true`  | Generates the machine-readable JSON sidecar used by report aggregation.      |
| `ArtifactUploadEnabled` | `bool` | `true`  | Uploads the HTML report as an artifact when supported by the CI environment. |

The corresponding `TUNIT_DISABLE_HTML_REPORTER`, `TUNIT_DISABLE_JSON_REPORT`, and `TUNIT_DISABLE_ARTIFACT_UPLOAD` environment variables take precedence over these values.

### `context.Settings.Mocks`[​](#contextsettingsmocks "Direct link to contextsettingsmocks")

Available when `TUnit.Mocks` is referenced.

| Property      | Type           | Default              | Description                                                                                                                            |
| ------------- | -------------- | -------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `DefaultMode` | `MockBehavior` | `MockBehavior.Loose` | Default behavior for mocks created without an explicit mode. Set to `MockBehavior.Strict` to make unconfigured calls throw by default. |

## Precedence[​](#precedence "Direct link to Precedence")

When the same setting is configured in multiple places, the following priority order applies (highest wins):

1. **Command-line flag** (e.g., `--maximum-parallel-tests 8`)
2. **Environment variable** (e.g., `TUNIT_MAX_PARALLEL_TESTS=8`)
3. **`context.Settings` (code)** — values set in a `[Before(HookType.TestDiscovery)]` hook
4. **Built-in default**

### Example[​](#example "Direct link to Example")

Your test project sets a conservative parallelism limit in code:

```
public static void Configure(BeforeTestDiscoveryContext context)

{

    context.Settings.Parallelism.MaximumParallelTests = 1;

}
```

A developer on a powerful machine can override this for a local run without changing code:

```
dotnet run --project MyTests -- --maximum-parallel-tests 8
```

The command-line flag takes precedence, so 8 parallel tests will be used.

## When to Set[​](#when-to-set "Direct link to When to Set")

Set most values via `context.Settings` inside a `[Before(HookType.TestDiscovery)]` hook. This is the earliest point in the TUnit lifecycle where user code runs and ensures your values are in place before test discovery begins. Setting values later (for example in a `[Before(HookType.TestSession)]` hook) may have no effect for settings that are read during discovery.

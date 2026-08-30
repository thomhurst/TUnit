# Logging

TUnit provides a flexible logging system that captures all test output and routes it to configurable destinations called "log sinks".

## Basic Usage[​](#basic-usage "Direct link to Basic Usage")

By default, TUnit intercepts any logs to `Console.WriteLine()` and correlates them to the test that triggered the log using the current async context.

```
using TUnit.Core.Logging;



[Test]

public async Task MyTest()

{

    Console.WriteLine("This output is captured and associated with this test");

}
```

### Logger Objects[​](#logger-objects "Direct link to Logger Objects")

For more control, use `TestContext.Current.GetDefaultLogger()` to get a logger instance:

```
using TUnit.Core.Logging;



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

## Cross-Thread Output Correlation[​](#cross-thread-output-correlation "Direct link to Cross-Thread Output Correlation")

By default, TUnit uses `AsyncLocal` to track which test is running on the current async flow. This works automatically when your code runs on the same async context as the test — for example, calling `Console.WriteLine()` from test code, or from services invoked directly by the test.

However, when work runs on a **different thread or async context** — such as inside a gRPC handler, message queue consumer, MCP server, or background service — the `AsyncLocal` context is not inherited, and TUnit cannot automatically determine which test the output belongs to.

### The Problem[​](#the-problem "Direct link to The Problem")

```
[Test]

public async Task MyTest()

{

    // ✅ This works — same async context as the test

    Console.WriteLine("This is captured by the test");



    // Start some background work that processes on its own thread

    await myService.ProcessAsync(requestId);



    // ❌ Inside ProcessAsync, if it runs on a different thread,

    // Console.WriteLine output won't be associated with this test

}
```

### The Solution: `TestContext.MakeCurrent()`[​](#the-solution-testcontextmakecurrent "Direct link to the-solution-testcontextmakecurrent")

Use `TestContext.MakeCurrent()` to associate a scope with a specific test. All `Console.Write`, `Console.WriteLine`, and `ILogger` calls within that scope are routed to the correct test's output.

```
using (testContext.MakeCurrent())

{

    // All output here is attributed to the test

    Console.WriteLine("This goes to the right test");

    await ProcessRequest();

}

// Previous context is automatically restored
```

### Automatic Correlation via OpenTelemetry[​](#automatic-correlation-via-opentelemetry "Direct link to Automatic Correlation via OpenTelemetry")

If your application uses OpenTelemetry (or any framework that propagates W3C `traceparent`/`baggage` headers), test context correlation works **automatically** — no `MakeCurrent()` or manual header-passing needed.

TUnit sets the test context ID as `Activity` baggage (`tunit.test.id`) on each test's span. When a framework propagates that Activity across a boundary (e.g., an HTTP call from a test to a shared service host), `TestContext.Current` on the receiving side automatically resolves the originating test from the propagated baggage.

This works out of the box with OTel-instrumented:

* **HTTP** (via `HttpClient` / ASP.NET Core)
* **gRPC** (via `Grpc.Net.Client` / ASP.NET Core gRPC)
* **Messaging** (via libraries like MassTransit or NServiceBus with OTel support)

```
// Test side — nothing special needed beyond normal OTel setup

[Test]

public async Task MyTest()

{

    // HttpClient propagates Activity automatically when OTel is configured

    var response = await _httpClient.GetAsync("/api/process");

}



// Server side — TestContext.Current resolves automatically

public async Task<IResult> HandleProcess()

{

    // TestContext.Current is non-null here because the Activity

    // carrying tunit.test.id baggage was propagated via HTTP headers

    Console.WriteLine("This output is attributed to the test");

    return Results.Ok();

}
```

note

This requires .NET 8+ and an active OpenTelemetry `TracerProvider` (or `ActivityListener`) subscribed to the `"TUnit"` source **on the test runner side**. The baggage is only set when the test runner's ActivitySource has listeners — without this, no Activity is created and the baggage cannot propagate. See [OpenTelemetry Tracing](/docs/examples/opentelemetry.md) for setup instructions.

For applications **without** Activity propagation, use the manual `MakeCurrent()` approach described below.

### How to Get the Test Context[​](#how-to-get-the-test-context "Direct link to How to Get the Test Context")

Your background service needs a way to know **which test** to correlate to. The typical pattern is to propagate the test's unique ID through your protocol (HTTP header, gRPC metadata, message property, etc.), then look it up on the receiving side with `TestContext.GetById()`.

#### Step 1: Send the Test ID[​](#step-1-send-the-test-id "Direct link to Step 1: Send the Test ID")

From your test, include `TestContext.Current!.Id` in the request:

```
[Test]

public async Task MyTest()

{

    var request = new MyRequest

    {

        Payload = "test data",

        TestId = TestContext.Current!.Id  // Propagate the test ID

    };



    await myService.SendAsync(request);



    var output = TestContext.Current!.GetStandardOutput();

    await Assert.That(output).Contains("processed");

}
```

#### Step 2: Resolve and Activate on the Receiving Side[​](#step-2-resolve-and-activate-on-the-receiving-side "Direct link to Step 2: Resolve and Activate on the Receiving Side")

In your handler, extract the test ID and call `MakeCurrent()`:

```
public async Task HandleAsync(MyRequest request)

{

    // Look up the test context by the propagated ID

    if (TestContext.GetById(request.TestId) is { } testContext)

    {

        using (testContext.MakeCurrent())

        {

            // All output here is attributed to the originating test

            Console.WriteLine("processed");

            await DoWork(request);

        }

    }

}
```

### Protocol-Specific Examples[​](#protocol-specific-examples "Direct link to Protocol-Specific Examples")

#### gRPC Server Interceptor[​](#grpc-server-interceptor "Direct link to gRPC Server Interceptor")

```
public class TUnitGrpcInterceptor : Interceptor

{

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(

        TRequest request,

        ServerCallContext context,

        UnaryServerMethod<TRequest, TResponse> continuation)

    {

        var testId = context.RequestHeaders.GetValue("x-tunit-test-id");



        if (testId is not null && TestContext.GetById(testId) is { } testContext)

        {

            using (testContext.MakeCurrent())

            {

                return await continuation(request, context);

            }

        }



        return await continuation(request, context);

    }

}
```

#### Message Queue Consumer[​](#message-queue-consumer "Direct link to Message Queue Consumer")

```
public class OrderConsumer : IConsumer<OrderMessage>

{

    public async Task Consume(ConsumeContext<OrderMessage> context)

    {

        var testId = context.Headers.Get<string>("x-tunit-test-id");



        if (testId is not null && TestContext.GetById(testId) is { } testContext)

        {

            using (testContext.MakeCurrent())

            {

                await ProcessOrder(context.Message);

            }

        }

    }

}
```

#### ASP.NET Core (Built-In)[​](#aspnet-core-built-in "Direct link to ASP.NET Core (Built-In)")

For ASP.NET Core, `TUnit.AspNetCore` handles this automatically. The `TUnitTestIdHandler` propagates the test ID via an HTTP header, and the `TUnitTestContextMiddleware` calls `MakeCurrent()` on the server side. See [ASP.NET Core Integration Testing](/docs/examples/aspnet.md#tunit-logging-integration) for details.

### Key Points[​](#key-points "Direct link to Key Points")

* `MakeCurrent()` returns a disposable scope — always use it with `using` to ensure the previous context is restored
* `TestContext.GetById()` returns `null` if the ID is not found (e.g., if the test has already completed), so always null-check
* `MakeCurrent()` is safe for concurrent tests — each call sets its own `AsyncLocal` scope independently
* The scope only affects the current async flow — other threads/tasks are not affected unless they inherit the `ExecutionContext`

## Log Sinks[​](#log-sinks "Direct link to Log Sinks")

TUnit uses a sink-based architecture where all output is routed through registered log sinks. Each sink decides how to handle the messages - write to files, stream to IDEs, send to external services, etc.

### Built-in Sinks[​](#built-in-sinks "Direct link to Built-in Sinks")

TUnit automatically registers these sinks based on your execution context:

| Sink                  | When Registered     | Purpose                                                |
| --------------------- | ------------------- | ------------------------------------------------------ |
| **TestOutputSink**    | Always              | Captures output for test results shown after execution |
| **ConsoleOutputSink** | `--output Detailed` | Writes real-time output to the console                 |

### Creating Custom Log Sinks[​](#creating-custom-log-sinks "Direct link to Creating Custom Log Sinks")

Implement the `ILogSink` interface to create a custom sink:

```
using TUnit.Core;

using TUnit.Core.Logging;

using Serilog;

using LogLevel = TUnit.Core.Logging.LogLevel;



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

            ? tc.Metadata.TestName

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

### Registering Custom Sinks[​](#registering-custom-sinks "Direct link to Registering Custom Sinks")

Register your sink in a `[Before(TestDiscovery)]` hook so it's active before any tests are discovered or run:

```
public class TestSetup

{

    [Before(TestDiscovery)]

    public static void SetupLogging()

    {

        // Register by instance (for sinks needing configuration)

        TUnit.Core.Logging.TUnitLoggerFactory.AddSink(new FileLogSink("test-output.log"));



        // Or register by type (for simple sinks)

        TUnit.Core.Logging.TUnitLoggerFactory.AddSink<DebugLogSink>();

    }

}
```

Sinks that implement `IDisposable` or `IAsyncDisposable` are automatically disposed when the test session ends.

### Context Information[​](#context-information "Direct link to Context Information")

The `context` parameter provides information about where the log originated:

```
using LogLevel = TUnit.Core.Logging.LogLevel;



public void Log(LogLevel level, string message, Exception? exception, Context? context)

{

    switch (context)

    {

        case TestContext tc:

            // During test execution

            var testName = tc.Metadata.TestName;

            var className = tc.Metadata.TestDetails.ClassType.Name;

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

### Example: Seq/Serilog Integration[​](#example-seqserilog-integration "Direct link to Example: Seq/Serilog Integration")

Here's an example sink that sends logs to Seq:

```
using TUnit.Core.Logging;

using Serilog;

using LogLevel = TUnit.Core.Logging.LogLevel;



public class SeqLogSink : ILogSink, IDisposable

{

    private readonly Serilog.ILogger _logger;



    public SeqLogSink(string seqUrl)

    {

        _logger = new Serilog.LoggerConfiguration()

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



        var testName = context is TestContext tc ? tc.Metadata.TestName : "Unknown";



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

## Microsoft.Extensions.Logging Integration[​](#microsoftextensionslogging-integration "Direct link to Microsoft.Extensions.Logging Integration")

TUnit core has no dependency on `Microsoft.Extensions.Logging`. If your application uses `ILogger`, install `TUnit.AspNetCore` or `TUnit.Logging.Microsoft` to bridge the two.

### ASP.NET Core[​](#aspnet-core "Direct link to ASP.NET Core")

Install the `TUnit.AspNetCore` package:

```
dotnet add package TUnit.AspNetCore
```

When using `TestWebApplicationFactory`, logging is fully automatic. See the [ASP.NET Core Integration Testing](/docs/examples/aspnet.md#tunit-logging-integration) docs for details.

### Standalone (No ASP.NET Core)[​](#standalone-no-aspnet-core "Direct link to Standalone (No ASP.NET Core)")

For `IHost`-based apps or generic DI scenarios, install the `TUnit.Logging.Microsoft` package:

```
dotnet add package TUnit.Logging.Microsoft
```

Then register TUnit as a logging provider:

```
using TUnit.Logging.Microsoft;



var host = Host.CreateDefaultBuilder()

    .ConfigureLogging(logging =>

    {

        TUnit.Logging.Microsoft.LoggingBuilderExtensions.AddTUnit(logging, TestContext.Current!);

    })

    .Build();
```

Or via `IServiceCollection`:

```
TUnit.Logging.Microsoft.ServiceCollectionExtensions.AddTUnitLogging(services, TestContext.Current!);
```

All `ILogger` output is routed through TUnit's console interceptor and sink pipeline, appearing in test output, IDE test explorers, and the console.

## Log Levels[​](#log-levels "Direct link to Log Levels")

TUnit uses the same log level as provided to the Microsoft.Testing.Platform via command line:

```
dotnet run --log-level Warning
```

Available levels (from least to most severe):

* `Trace`
* `Debug`
* `Information` (default)
* `Warning`
* `Error`
* `Critical`

## Custom Loggers[​](#custom-loggers "Direct link to Custom Loggers")

You can also create custom loggers by inheriting from `DefaultLogger`:

```
using TUnit.Core.Logging;

using LogLevel = TUnit.Core.Logging.LogLevel;



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

            var testId = $"{testContext.Metadata.TestDetails.ClassType.Name}.{testContext.Metadata.TestName}";

            return $"--- {testId} ---\n{baseMessage}";

        }



        return baseMessage;

    }

}
```

### Available Extension Points[​](#available-extension-points "Direct link to Available Extension Points")

* `Context` - Protected property to access the associated context
* `GenerateMessage(message, exception, logLevel)` - Override to customize message formatting
* `WriteToOutput(message, isError)` - Override to customize synchronous output
* `WriteToOutputAsync(message, isError)` - Override for asynchronous output

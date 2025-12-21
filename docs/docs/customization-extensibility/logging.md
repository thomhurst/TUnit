# Logging

By default, TUnit will intercept any logs to the `Console`, and attempt to correlate them to the test that triggered that log by the current async context that it is in.

So for most scenarios, you can just rely on `Console.WriteLine(...)`.

## Logger Objects
If you want a logger object, you can call `TestContext.Current.GetDefaultLogger()`, which will give you a logger that will write output to that test's output writer.

This logger can also be used to map to other logging interfaces (e.g. Microsoft.Extensions.Logging), so that for example, Asp.NET web apps can log to your test's context, so that you have a cleaner, more isolated log output.

## Log Level
TUnit will use the same log level as provided to the Microsoft.Testing.Platform, which is set on the command line when invoking the test suite. If not defined, the default log level should be `Trace`.

If you want to override this, you can inherit from `TUnitLogger` or `DefaultLogger` and override the `IsEnabled` method:

```csharp
 public override bool IsEnabled(LogLevel logLevel)
{
    return logLevel >= LogLevel.Error;
}
```

## Log Level Command Line
If you are executing tests via the command line, you can set the log level via the `--log-level` argument:

```
dotnet run --log-level Warning
```

The above will show only logs that are `Warning` or higher (e.g. `Error`, `Critical`) while executing the test.

## Custom Loggers

The `DefaultLogger` class is designed to be extensible. You can inherit from it to customize message formatting and output behavior.

### Available Extension Points

- `Context` - Protected property to access the associated context
- `GenerateMessage(string message, Exception? exception, LogLevel logLevel)` - Override to customize message formatting
- `WriteToOutput(string message, bool isError)` - Override to customize how messages are written
- `WriteToOutputAsync(string message, bool isError)` - Async version of WriteToOutput

### Example: Adding Test Headers

Here's an example of a custom logger that prepends a test identifier header before the first log message:

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

### Using Custom Loggers

Create an instance of your custom logger and use it directly:

```csharp
[Test]
public async Task MyTest()
{
    var logger = new TestHeaderLogger(TestContext.Current!);
    logger.LogInformation("This message will have a test header");
    logger.LogInformation("Subsequent messages won't repeat the header");
}
```

### Example: Custom Output Destinations

You can override the write methods to send output to additional destinations:

```csharp
public class MultiDestinationLogger : DefaultLogger
{
    private readonly TextWriter _additionalOutput;

    public MultiDestinationLogger(Context context, TextWriter additionalOutput)
        : base(context)
    {
        _additionalOutput = additionalOutput;
    }

    protected override void WriteToOutput(string message, bool isError)
    {
        base.WriteToOutput(message, isError);
        _additionalOutput.WriteLine(message);
    }
}
```

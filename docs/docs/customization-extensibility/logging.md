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

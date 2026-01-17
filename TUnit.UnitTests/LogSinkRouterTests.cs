using TUnit.Core.Logging;

namespace TUnit.UnitTests;

[NotInParallel]
public class LogSinkRouterTests
{
    [Before(Test)]
    public void SetUp()
    {
        // Ensure clean state before each test
        TUnitLoggerFactory.Clear();
    }

    [After(Test)]
    public async Task TearDown()
    {
        // Ensure clean state after each test
        await TUnitLoggerFactory.DisposeAllAsync();
    }

    [Test]
    public async Task RouteToSinks_CallsAllEnabledSinks()
    {
        var sink1 = new RecordingSink();
        var sink2 = new RecordingSink();
        TUnitLoggerFactory.AddSink(sink1);
        TUnitLoggerFactory.AddSink(sink2);

        LogSinkRouter.RouteToSinks(LogLevel.Information, "Test message", null, null);

        await Assert.That(sink1.Logs).Count().IsEqualTo(1);
        await Assert.That(sink2.Logs).Count().IsEqualTo(1);
        await Assert.That(sink1.Logs[0].Message).IsEqualTo("Test message");
        await Assert.That(sink1.Logs[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(sink2.Logs[0].Message).IsEqualTo("Test message");
    }

    [Test]
    public async Task RouteToSinks_SkipsDisabledSinks()
    {
        var enabledSink = new RecordingSink { Enabled = true };
        var disabledSink = new RecordingSink { Enabled = false };
        TUnitLoggerFactory.AddSink(enabledSink);
        TUnitLoggerFactory.AddSink(disabledSink);

        LogSinkRouter.RouteToSinks(LogLevel.Information, "Test message", null, null);

        await Assert.That(enabledSink.Logs).Count().IsEqualTo(1);
        await Assert.That(disabledSink.Logs).IsEmpty();
    }

    [Test]
    public async Task RouteToSinks_ContinuesOnSinkFailure()
    {
        var faultySink = new FaultySink();
        var goodSink = new RecordingSink();
        TUnitLoggerFactory.AddSink(faultySink);
        TUnitLoggerFactory.AddSink(goodSink);

        // Should not throw
        LogSinkRouter.RouteToSinks(LogLevel.Information, "Test message", null, null);

        // Good sink should still receive the message
        await Assert.That(goodSink.Logs).Count().IsEqualTo(1);
        await Assert.That(goodSink.Logs[0].Message).IsEqualTo("Test message");
    }

    [Test]
    public async Task RouteToSinks_PassesExceptionToSinks()
    {
        var sink = new RecordingSink();
        TUnitLoggerFactory.AddSink(sink);
        var exception = new InvalidOperationException("Test exception");

        LogSinkRouter.RouteToSinks(LogLevel.Error, "Error occurred", exception, null);

        await Assert.That(sink.Logs).Count().IsEqualTo(1);
        await Assert.That(sink.Logs[0].Exception).IsSameReferenceAs(exception);
    }

    [Test]
    public async Task RouteToSinks_DoesNothingWhenNoSinksRegistered()
    {
        // Should not throw when no sinks are registered
        LogSinkRouter.RouteToSinks(LogLevel.Information, "Test message", null, null);

        // Just verify no exception was thrown - test passes if we get here
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RouteToSinksAsync_CallsAllEnabledSinks()
    {
        var sink1 = new RecordingSink();
        var sink2 = new RecordingSink();
        TUnitLoggerFactory.AddSink(sink1);
        TUnitLoggerFactory.AddSink(sink2);

        await LogSinkRouter.RouteToSinksAsync(LogLevel.Information, "Test message", null, null);

        await Assert.That(sink1.Logs).Count().IsEqualTo(1);
        await Assert.That(sink2.Logs).Count().IsEqualTo(1);
        await Assert.That(sink1.Logs[0].Message).IsEqualTo("Test message");
        await Assert.That(sink1.Logs[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(sink2.Logs[0].Message).IsEqualTo("Test message");
    }

    [Test]
    public async Task RouteToSinksAsync_SkipsDisabledSinks()
    {
        var enabledSink = new RecordingSink { Enabled = true };
        var disabledSink = new RecordingSink { Enabled = false };
        TUnitLoggerFactory.AddSink(enabledSink);
        TUnitLoggerFactory.AddSink(disabledSink);

        await LogSinkRouter.RouteToSinksAsync(LogLevel.Information, "Test message", null, null);

        await Assert.That(enabledSink.Logs).Count().IsEqualTo(1);
        await Assert.That(disabledSink.Logs).IsEmpty();
    }

    [Test]
    public async Task RouteToSinksAsync_ContinuesOnSinkFailure()
    {
        var faultySink = new FaultySink();
        var goodSink = new RecordingSink();
        TUnitLoggerFactory.AddSink(faultySink);
        TUnitLoggerFactory.AddSink(goodSink);

        // Should not throw
        await LogSinkRouter.RouteToSinksAsync(LogLevel.Information, "Test message", null, null);

        // Good sink should still receive the message
        await Assert.That(goodSink.Logs).Count().IsEqualTo(1);
        await Assert.That(goodSink.Logs[0].Message).IsEqualTo("Test message");
    }

    [Test]
    public async Task RouteToSinksAsync_PassesExceptionToSinks()
    {
        var sink = new RecordingSink();
        TUnitLoggerFactory.AddSink(sink);
        var exception = new InvalidOperationException("Test exception");

        await LogSinkRouter.RouteToSinksAsync(LogLevel.Error, "Error occurred", exception, null);

        await Assert.That(sink.Logs).Count().IsEqualTo(1);
        await Assert.That(sink.Logs[0].Exception).IsSameReferenceAs(exception);
    }

    [Test]
    public async Task RouteToSinksAsync_DoesNothingWhenNoSinksRegistered()
    {
        // Should not throw when no sinks are registered
        await LogSinkRouter.RouteToSinksAsync(LogLevel.Information, "Test message", null, null);

        // Just verify no exception was thrown - test passes if we get here
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RouteToSinks_PassesCorrectLogLevel()
    {
        var sink = new RecordingSink();
        TUnitLoggerFactory.AddSink(sink);

        LogSinkRouter.RouteToSinks(LogLevel.Warning, "Warning message", null, null);
        LogSinkRouter.RouteToSinks(LogLevel.Error, "Error message", null, null);
        LogSinkRouter.RouteToSinks(LogLevel.Debug, "Debug message", null, null);

        await Assert.That(sink.Logs).Count().IsEqualTo(3);
        await Assert.That(sink.Logs[0].Level).IsEqualTo(LogLevel.Warning);
        await Assert.That(sink.Logs[1].Level).IsEqualTo(LogLevel.Error);
        await Assert.That(sink.Logs[2].Level).IsEqualTo(LogLevel.Debug);
    }

    [Test]
    public async Task RouteToSinks_SinkCanFilterByLogLevel()
    {
        var errorOnlySink = new LevelFilteredSink(LogLevel.Error);
        TUnitLoggerFactory.AddSink(errorOnlySink);

        LogSinkRouter.RouteToSinks(LogLevel.Information, "Info message", null, null);
        LogSinkRouter.RouteToSinks(LogLevel.Warning, "Warning message", null, null);
        LogSinkRouter.RouteToSinks(LogLevel.Error, "Error message", null, null);

        // Only error message should be logged
        await Assert.That(errorOnlySink.Logs).Count().IsEqualTo(1);
        await Assert.That(errorOnlySink.Logs[0].Level).IsEqualTo(LogLevel.Error);
    }

    [Test]
    public async Task RouteToSinksAsync_SinkCanFilterByLogLevel()
    {
        var errorOnlySink = new LevelFilteredSink(LogLevel.Error);
        TUnitLoggerFactory.AddSink(errorOnlySink);

        await LogSinkRouter.RouteToSinksAsync(LogLevel.Information, "Info message", null, null);
        await LogSinkRouter.RouteToSinksAsync(LogLevel.Warning, "Warning message", null, null);
        await LogSinkRouter.RouteToSinksAsync(LogLevel.Error, "Error message", null, null);

        // Only error message should be logged
        await Assert.That(errorOnlySink.Logs).Count().IsEqualTo(1);
        await Assert.That(errorOnlySink.Logs[0].Level).IsEqualTo(LogLevel.Error);
    }

    #region Mock Sinks

    private class RecordingSink : ILogSink
    {
        public List<(LogLevel Level, string Message, Exception? Exception, Context? Context)> Logs { get; } = [];
        public bool Enabled { get; set; } = true;

        public bool IsEnabled(LogLevel level) => Enabled;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
            => Logs.Add((level, message, exception, context));

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            Logs.Add((level, message, exception, context));
            return ValueTask.CompletedTask;
        }
    }

    private class FaultySink : ILogSink
    {
        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
            => throw new InvalidOperationException("Sink failure");

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
            => throw new InvalidOperationException("Sink failure");
    }

    private class LevelFilteredSink : ILogSink
    {
        private readonly LogLevel _minimumLevel;

        public LevelFilteredSink(LogLevel minimumLevel)
        {
            _minimumLevel = minimumLevel;
        }

        public List<(LogLevel Level, string Message, Exception? Exception, Context? Context)> Logs { get; } = [];

        public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
            => Logs.Add((level, message, exception, context));

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            Logs.Add((level, message, exception, context));
            return ValueTask.CompletedTask;
        }
    }

    #endregion
}

using TUnit.Core.Logging;

namespace TUnit.UnitTests;

/// <summary>
/// Integration tests that verify the end-to-end flow from DefaultLogger through LogSinkRouter to registered sinks.
/// </summary>
[NotInParallel]
public class LogSinkIntegrationTests
{
    private RecordingSink _sink = null!;

    [Before(Test)]
    public void SetUp()
    {
        TUnitLoggerFactory.Clear();
        _sink = new RecordingSink();
        TUnitLoggerFactory.AddSink(_sink);
    }

    [After(Test)]
    public async Task TearDown()
    {
        await TUnitLoggerFactory.DisposeAllAsync();
    }

    [Test]
    public async Task DefaultLogger_RoutesToRegisteredSink()
    {
        // Act
        TestContext.Current!.GetDefaultLogger().LogInformation("test message");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(1);
        await Assert.That(_sink.Logs[0].Message).Contains("test message");
    }

    [Test]
    public async Task DefaultLogger_RoutesMultipleMessages()
    {
        // Act
        var logger = TestContext.Current!.GetDefaultLogger();
        logger.LogInformation("message 1");
        logger.LogWarning("message 2");
        logger.LogError("message 3");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(3);
    }

    [Test]
    public async Task DefaultLogger_PassesCorrectLogLevels()
    {
        // Act
        var logger = TestContext.Current!.GetDefaultLogger();
        logger.LogInformation("info message");
        logger.LogWarning("warning message");
        logger.LogError("error message");

        // Assert - DefaultLogger converts to Information or Error based on level
        // LogLevel.Information and LogLevel.Warning both become LogLevel.Information in WriteToOutput
        // LogLevel.Error and above become LogLevel.Error
        await Assert.That(_sink.Logs).Count().IsEqualTo(3);
        await Assert.That(_sink.Logs[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(_sink.Logs[1].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(_sink.Logs[2].Level).IsEqualTo(LogLevel.Error);
    }

    [Test]
    public async Task DefaultLogger_IncludesLogLevelInMessage()
    {
        // Act
        TestContext.Current!.GetDefaultLogger().LogWarning("warning test");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(1);
        await Assert.That(_sink.Logs[0].Message).Contains("Warning:");
        await Assert.That(_sink.Logs[0].Message).Contains("warning test");
    }

    [Test]
    public async Task DefaultLogger_PassesContextToSink()
    {
        // Act
        TestContext.Current!.GetDefaultLogger().LogInformation("context test");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(1);
        await Assert.That(_sink.Logs[0].Context).IsNotNull();
        await Assert.That(_sink.Logs[0].Context).IsSameReferenceAs(TestContext.Current);
    }

    [Test]
    public async Task DefaultLogger_AsyncLogging_RoutesToSink()
    {
        // Act
        await TestContext.Current!.GetDefaultLogger().LogInformationAsync("async message");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(1);
        await Assert.That(_sink.Logs[0].Message).Contains("async message");
    }

    [Test]
    public async Task DefaultLogger_MultipleSinks_AllReceiveMessages()
    {
        // Arrange - add a second sink
        var secondSink = new RecordingSink();
        TUnitLoggerFactory.AddSink(secondSink);

        // Act
        TestContext.Current!.GetDefaultLogger().LogInformation("broadcast message");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(1);
        await Assert.That(secondSink.Logs).Count().IsEqualTo(1);
        await Assert.That(_sink.Logs[0].Message).Contains("broadcast message");
        await Assert.That(secondSink.Logs[0].Message).Contains("broadcast message");
    }

    [Test]
    public async Task DefaultLogger_DisabledSink_DoesNotReceiveMessages()
    {
        // Arrange - add a disabled sink
        var disabledSink = new RecordingSink { Enabled = false };
        TUnitLoggerFactory.AddSink(disabledSink);

        // Act
        TestContext.Current!.GetDefaultLogger().LogInformation("enabled only message");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(1); // Enabled sink receives it
        await Assert.That(disabledSink.Logs).IsEmpty(); // Disabled sink does not
    }

    [Test]
    public async Task DefaultLogger_LevelFilteredSink_OnlyReceivesMatchingLevels()
    {
        // Arrange - add a sink that only accepts Error or higher
        var errorOnlySink = new LevelFilteredSink(LogLevel.Error);
        TUnitLoggerFactory.AddSink(errorOnlySink);

        // Act
        var logger = TestContext.Current!.GetDefaultLogger();
        logger.LogInformation("info message");
        logger.LogWarning("warning message");
        logger.LogError("error message");

        // Assert
        await Assert.That(_sink.Logs).Count().IsEqualTo(3); // Default sink receives all
        await Assert.That(errorOnlySink.Logs).Count().IsEqualTo(1); // Filtered sink only receives error
        await Assert.That(errorOnlySink.Logs[0].Message).Contains("error message");
    }

    #region Recording Sinks

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

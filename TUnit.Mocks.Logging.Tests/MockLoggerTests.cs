using Microsoft.Extensions.Logging;
using TUnit.Mocks;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Logging;

namespace TUnit.Mocks.Logging.Tests;

public class MockLoggerTests
{
    [Test]
    public async Task CapturesLogEntries()
    {
        var logger = Mock.Logger();

        logger.LogInformation("Hello {Name}", "World");

        await Assert.That(logger.Entries).Count().IsEqualTo(1);
    }

    [Test]
    public async Task CapturesLogLevel()
    {
        var logger = Mock.Logger();

        logger.LogWarning("warning message");

        await Assert.That(logger.Entries[0].LogLevel).IsEqualTo(LogLevel.Warning);
    }

    [Test]
    public async Task CapturesFormattedMessage()
    {
        var logger = Mock.Logger();

        logger.LogInformation("User {UserId} logged in", 42);

        await Assert.That(logger.Entries[0].Message).Contains("42");
    }

    [Test]
    public async Task CapturesException()
    {
        var logger = Mock.Logger();
        var ex = new InvalidOperationException("test error");

        logger.LogError(ex, "Something failed");

        await Assert.That(logger.Entries[0].Exception).IsNotNull().And.IsTypeOf<InvalidOperationException>();
    }

    [Test]
    public async Task LatestEntryReturnsLastEntry()
    {
        var logger = Mock.Logger();

        logger.LogInformation("first");
        logger.LogWarning("second");
        logger.LogError("third");

        await Assert.That(logger.LatestEntry).IsNotNull();
        await Assert.That(logger.LatestEntry!.Message).IsEqualTo("third");
    }

    [Test]
    public async Task LatestEntryReturnsNullWhenEmpty()
    {
        var logger = Mock.Logger();

        await Assert.That(logger.LatestEntry).IsNull();
    }

    [Test]
    public async Task ClearRemovesAllEntries()
    {
        var logger = Mock.Logger();

        logger.LogInformation("one");
        logger.LogInformation("two");
        logger.Clear();

        await Assert.That(logger.Entries).Count().IsEqualTo(0);
    }

    [Test]
    public async Task IsEnabledAlwaysReturnsTrue()
    {
        var logger = Mock.Logger();

        await Assert.That(logger.IsEnabled(LogLevel.Trace)).IsTrue();
        await Assert.That(logger.IsEnabled(LogLevel.None)).IsTrue();
    }

    [Test]
    public async Task BeginScopeReturnsDisposable()
    {
        var logger = Mock.Logger();

        using var scope = logger.BeginScope("test scope");

        await Assert.That(scope).IsNotNull();
    }

    [Test]
    public async Task GenericLoggerHasCategoryName()
    {
        var logger = Mock.Logger<MockLoggerTests>();

        logger.LogInformation("test");

        await Assert.That(logger.Entries[0].CategoryName).IsEqualTo(nameof(MockLoggerTests));
    }

    [Test]
    public async Task GenericLoggerImplementsILoggerOfT()
    {
        ILogger<MockLoggerTests> logger = Mock.Logger<MockLoggerTests>();

        logger.LogInformation("typed logger test");

        var mockLogger = (MockLogger<MockLoggerTests>)logger;
        await Assert.That(mockLogger.Entries).Count().IsEqualTo(1);
    }
}

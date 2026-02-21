using Microsoft.Extensions.Logging;
using TUnit.Mock.Exceptions;
using TUnit.Mock.Logging;

namespace TUnit.Mock.Logging.Tests;

public class LogVerificationTests
{
    [Test]
    public void VerifyLogAtLevel()
    {
        var logger = new MockLogger();
        logger.LogError("error happened");

        logger.VerifyLog().AtLevel(LogLevel.Error).WasCalled(Times.Once);
    }

    [Test]
    public void VerifyLogContainingMessage()
    {
        var logger = new MockLogger();
        logger.LogInformation("User 42 logged in");
        logger.LogInformation("User 99 logged out");

        logger.VerifyLog().ContainingMessage("logged in").WasCalled(Times.Once);
    }

    [Test]
    public void VerifyLogWithExactMessage()
    {
        var logger = new MockLogger();
        logger.LogInformation("exact match");

        logger.VerifyLog().WithMessage("exact match").WasCalled(Times.Once);
    }

    [Test]
    public void VerifyLogWithException()
    {
        var logger = new MockLogger();
        logger.LogError(new InvalidOperationException("oops"), "failed");

        logger.VerifyLog().WithException<InvalidOperationException>().WasCalled(Times.Once);
    }

    [Test]
    public void VerifyLogWithCombinedFilters()
    {
        var logger = new MockLogger();
        logger.LogError(new InvalidOperationException("oops"), "operation failed");
        logger.LogError("generic error");
        logger.LogWarning(new InvalidOperationException("oops"), "not an error");

        logger.VerifyLog()
            .AtLevel(LogLevel.Error)
            .WithException<InvalidOperationException>()
            .WasCalled(Times.Once);
    }

    [Test]
    public void VerifyLogThrowsOnMismatch()
    {
        var logger = new MockLogger();
        logger.LogInformation("hello");

        Assert.Throws<MockVerificationException>(() =>
            logger.VerifyLog().AtLevel(LogLevel.Error).WasCalled(Times.AtLeastOnce));
    }

    [Test]
    public void VerifyWasNeverCalled()
    {
        var logger = new MockLogger();
        logger.LogInformation("info only");

        logger.VerifyLog().AtLevel(LogLevel.Error).WasNeverCalled();
    }

    [Test]
    public void VerifyWasNeverCalledThrowsWhenCalled()
    {
        var logger = new MockLogger();
        logger.LogError("error!");

        Assert.Throws<MockVerificationException>(() =>
            logger.VerifyLog().AtLevel(LogLevel.Error).WasNeverCalled());
    }

    [Test]
    public void ShorthandVerifyLog()
    {
        var logger = new MockLogger();
        logger.LogWarning("disk space low");

        logger.VerifyLog(LogLevel.Warning, "disk space");
    }

    [Test]
    public void ShorthandVerifyLogWithTimes()
    {
        var logger = new MockLogger();
        logger.LogWarning("retry attempt 1");
        logger.LogWarning("retry attempt 2");
        logger.LogWarning("retry attempt 3");

        logger.VerifyLog(LogLevel.Warning, "retry", Times.Exactly(3));
    }

    [Test]
    public void VerifyNoLog()
    {
        var logger = new MockLogger();
        logger.LogInformation("normal operation");

        logger.VerifyNoLog(LogLevel.Error);
    }

    [Test]
    public void VerifyNoLogs()
    {
        var logger = new MockLogger();

        logger.VerifyNoLogs();
    }

    [Test]
    public void VerifyNoLogsThrowsWhenLogged()
    {
        var logger = new MockLogger();
        logger.LogInformation("something");

        Assert.Throws<MockVerificationException>(() =>
            logger.VerifyNoLogs());
    }

    [Test]
    public async Task GetLogsByLevel()
    {
        var logger = new MockLogger();
        logger.LogInformation("info1");
        logger.LogWarning("warn1");
        logger.LogInformation("info2");

        var infoLogs = logger.GetLogs(LogLevel.Information);
        await Assert.That(infoLogs).Count().IsEqualTo(2);
    }

    [Test]
    public async Task GetLogsByMessage()
    {
        var logger = new MockLogger();
        logger.LogInformation("user logged in");
        logger.LogInformation("user logged out");
        logger.LogWarning("system alert");

        var userLogs = logger.GetLogs("user");
        await Assert.That(userLogs).Count().IsEqualTo(2);
    }

    [Test]
    public async Task GetMatchingEntriesReturnsFiltered()
    {
        var logger = new MockLogger();
        logger.LogInformation("hello");
        logger.LogError("error1");
        logger.LogError("error2");

        var errors = logger.VerifyLog()
            .AtLevel(LogLevel.Error)
            .GetMatchingEntries();

        await Assert.That(errors).Count().IsEqualTo(2);
    }

    [Test]
    public void VerifyTimesExactly()
    {
        var logger = new MockLogger();
        logger.LogInformation("a");
        logger.LogInformation("b");

        logger.VerifyLog().AtLevel(LogLevel.Information).WasCalled(Times.Exactly(2));
    }

    [Test]
    public void VerifyTimesAtLeast()
    {
        var logger = new MockLogger();
        logger.LogError("e1");
        logger.LogError("e2");
        logger.LogError("e3");

        logger.VerifyLog().AtLevel(LogLevel.Error).WasCalled(Times.AtLeast(2));
    }

    [Test]
    public void VerifyTimesAtMost()
    {
        var logger = new MockLogger();
        logger.LogWarning("w1");

        logger.VerifyLog().AtLevel(LogLevel.Warning).WasCalled(Times.AtMost(3));
    }

    [Test]
    public void VerifyTimesBetween()
    {
        var logger = new MockLogger();
        logger.LogInformation("a");
        logger.LogInformation("b");
        logger.LogInformation("c");

        logger.VerifyLog().AtLevel(LogLevel.Information).WasCalled(Times.Between(2, 5));
    }
}

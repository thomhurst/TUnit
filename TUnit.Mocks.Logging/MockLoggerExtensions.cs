using Microsoft.Extensions.Logging;

namespace TUnit.Mocks.Logging;

/// <summary>
/// Extension methods for fluent log verification on <see cref="MockLogger"/>.
/// </summary>
public static class MockLoggerExtensions
{
    /// <summary>
    /// Start building a log verification query.
    /// </summary>
    public static LogVerification VerifyLog(this MockLogger logger)
        => new(logger.Entries);

    /// <summary>
    /// Verify that a message was logged at the specified level.
    /// Shorthand for <c>logger.VerifyLog().AtLevel(level).ContainingMessage(message).WasCalled(times)</c>.
    /// </summary>
    public static void VerifyLog(this MockLogger logger, LogLevel level, string messageContains, Times times)
        => logger.VerifyLog().AtLevel(level).ContainingMessage(messageContains).WasCalled(times);

    /// <summary>
    /// Verify that a message was logged at the specified level at least once.
    /// </summary>
    public static void VerifyLog(this MockLogger logger, LogLevel level, string messageContains)
        => logger.VerifyLog().AtLevel(level).ContainingMessage(messageContains).WasCalled(Times.AtLeastOnce);

    /// <summary>
    /// Verify that nothing was logged at the specified level.
    /// </summary>
    public static void VerifyNoLog(this MockLogger logger, LogLevel level)
        => logger.VerifyLog().AtLevel(level).WasNeverCalled();

    /// <summary>
    /// Verify that nothing was logged at all.
    /// </summary>
    public static void VerifyNoLogs(this MockLogger logger)
        => logger.VerifyLog().WasNeverCalled();

    /// <summary>
    /// Get all entries logged at the specified level.
    /// </summary>
    public static IReadOnlyList<LogEntry> GetLogs(this MockLogger logger, LogLevel level)
        => logger.VerifyLog().AtLevel(level).GetMatchingEntries();

    /// <summary>
    /// Get all entries whose message contains the specified text.
    /// </summary>
    public static IReadOnlyList<LogEntry> GetLogs(this MockLogger logger, string messageContains)
        => logger.VerifyLog().ContainingMessage(messageContains).GetMatchingEntries();
}

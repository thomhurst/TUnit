namespace TUnit.Core.Interfaces;

/// <summary>
/// Reports failures raised by callbacks or other detached work to the currently executing test.
/// </summary>
/// <remarks>
/// Reporting a failure cancels the test execution token. Test code should observe that token so
/// it can stop during the framework's bounded cancellation grace period.
/// </remarks>
public interface ITestFailureSignal
{
    /// <summary>
    /// Reports an exception as the test failure.
    /// This method does not throw the supplied exception on the calling thread.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    void Report(Exception exception);

    /// <summary>
    /// Reports a reason as the test failure.
    /// This method does not throw on the calling thread.
    /// </summary>
    /// <param name="reason">The reason the test failed.</param>
    void Report(string reason);

    /// <summary>
    /// Attempts to report an exception as the test failure.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns><see langword="true"/> when this report was accepted; otherwise, <see langword="false"/>.</returns>
    bool TryReport(Exception exception);

    /// <summary>
    /// Attempts to report a reason as the test failure.
    /// </summary>
    /// <param name="reason">The reason the test failed.</param>
    /// <returns><see langword="true"/> when this report was accepted; otherwise, <see langword="false"/>.</returns>
    bool TryReport(string reason);
}

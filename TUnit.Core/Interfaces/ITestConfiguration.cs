namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test execution configuration properties.
/// Accessed via <see cref="TestDetails.Configuration"/>.
/// </summary>
public interface ITestConfiguration
{
    /// <summary>
    /// Gets the timeout duration for this test (may be null for no timeout).
    /// </summary>
    TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets the maximum number of retry attempts for this test.
    /// </summary>
    int RetryLimit { get; }
}

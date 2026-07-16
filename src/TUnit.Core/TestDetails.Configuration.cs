using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test execution configuration - implements <see cref="ITestConfiguration"/> interface
/// </summary>
public partial class TestDetails
{
    // Explicit interface implementation for ITestConfiguration
    TimeSpan? ITestConfiguration.Timeout => Timeout;
    int ITestConfiguration.RetryLimit => RetryLimit;
    int ITestConfiguration.RetryBackoffMs => RetryBackoffMs;
    double ITestConfiguration.RetryBackoffMultiplier => RetryBackoffMultiplier;
}

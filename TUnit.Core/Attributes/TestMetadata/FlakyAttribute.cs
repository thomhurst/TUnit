namespace TUnit.Core;

/// <summary>
/// Marks a test as a known flaky test - one that sometimes passes and sometimes fails without code changes.
/// </summary>
/// <remarks>
/// When a test marked with <see cref="FlakyAttribute"/> fails, the test output will indicate that the failure
/// is from a known flaky test. This helps teams track and manage unreliable tests without suppressing them entirely.
///
/// Unlike <see cref="SkipAttribute"/>, the test will still execute. Unlike <see cref="ExplicitAttribute"/>,
/// the test will run as part of the normal test suite.
///
/// Can be applied at the method or class level. When applied at a class level, all test methods
/// in the class are considered flaky.
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [Retry(3)]
/// [Flaky("Intermittent network timeout - see issue #1234")]
/// public async Task TestExternalServiceConnection()
/// {
///     // This test sometimes fails due to network conditions
/// }
///
/// [Test]
/// [Flaky]
/// public void TestWithRaceCondition()
/// {
///     // Known race condition, tracked for fixing
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class FlakyAttribute : TUnitAttribute
{
    /// <summary>
    /// Gets the reason why this test is considered flaky.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Marks the test as a known flaky test.
    /// </summary>
    public FlakyAttribute()
    {
    }

    /// <summary>
    /// Marks the test as a known flaky test with a reason.
    /// </summary>
    /// <param name="reason">A description of why the test is flaky, such as a bug tracker reference or known cause.</param>
    public FlakyAttribute(string reason)
    {
        Reason = reason;
    }
}

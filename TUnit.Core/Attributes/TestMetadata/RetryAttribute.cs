using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Specifies that a test method, test class, or assembly should be retried a specified number of times upon failure.
/// </summary>
/// <remarks>
/// When a test fails, the RetryAttribute causes the test to be re-executed up to the specified number of times.
/// The test is considered successful if any of the attempts complete without throwing an exception.
/// 
/// Retry can be applied at the method, class, or assembly level. When applied at a class level, all test methods
/// in the class will be retried on failure. When applied at the assembly level, it affects all tests in the assembly.
/// 
/// Method-level attributes take precedence over class-level attributes, which take precedence over assembly-level attributes.
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [Retry(3)]
/// public void TestThatMightFail()
/// {
///     // This test will be retried up to 3 times if it fails
/// }
/// 
/// // Example of a custom retry attribute with conditional logic
/// public class RetryOnNetworkErrorAttribute : RetryAttribute
/// {
///     public RetryOnNetworkErrorAttribute(int times) : base(times)
///     {
///     }
///
///     public override Task&lt;bool&gt; ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
///     {
///         return Task.FromResult(exception is HttpRequestException);
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class RetryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    /// <inheritdoc />
    public int Order => 0;

    /// <summary>
    /// Gets the maximum number of retry attempts that should be made for a failing test.
    /// </summary>
    /// <remarks>
    /// A test will be executed a maximum of (Times + 1) times:
    /// - The initial run
    /// - Up to 'Times' retry attempts
    /// </remarks>
    public int Times { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryAttribute"/> class with the specified number of retry attempts.
    /// </summary>
    /// <param name="times">The maximum number of retry attempts. Must be a non-negative integer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="times"/> is less than 0.</exception>
    public RetryAttribute(int times)
    {
        if (times < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(times), "Retry times must be positive");
        }

        Times = times;
    }

    /// <summary>
    /// Determines whether a test should be retried after a failure.
    /// </summary>
    /// <param name="context">The test context containing information about the test being executed.</param>
    /// <param name="exception">The exception that caused the test to fail.</param>
    /// <param name="currentRetryCount">The current retry count (1-based). The first retry is 1, the second is 2, and so on.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is true if the test should be retried; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method can be overridden in derived classes to implement conditional retry logic
    /// based on the specific exception type or other criteria.
    /// 
    /// The default implementation always returns true, meaning the test will always be retried
    /// up to the maximum number of attempts regardless of the exception type.
    /// </remarks>
    public virtual Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
    {
        return Task.FromResult(true);
    }


    /// <inheritdoc />
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.SetRetryCount(Times, ShouldRetry);
    }
}
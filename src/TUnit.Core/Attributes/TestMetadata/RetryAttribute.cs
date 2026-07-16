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
///
/// Optional retry policy properties:
/// - <see cref="BackoffMs"/>: Initial delay in milliseconds between retries (default 0 = no delay).
/// - <see cref="BackoffMultiplier"/>: Multiplier for exponential backoff (default 2.0).
/// - <see cref="RetryOnExceptionTypes"/>: Only retry when the exception matches one of the specified types.
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
/// // Retry with exponential backoff: waits 500ms, 1000ms, 2000ms between retries
/// [Test]
/// [Retry(3, BackoffMs = 500, BackoffMultiplier = 2.0)]
/// public void TestWithBackoff()
/// {
///     // This test will be retried with increasing delays
/// }
///
/// // Retry only on specific exception types
/// [Test]
/// [Retry(3, RetryOnExceptionTypes = new[] { typeof(HttpRequestException), typeof(TimeoutException) })]
/// public void TestWithExceptionFilter()
/// {
///     // This test will only be retried if the exception is HttpRequestException or TimeoutException
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
public class RetryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver, IScopedAttribute
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
    /// Gets or sets the initial delay in milliseconds before the first retry.
    /// Subsequent retries will be delayed by <c>BackoffMs * BackoffMultiplier^(attempt-1)</c>.
    /// </summary>
    /// <remarks>
    /// Default is 0 (no delay). When set to a positive value, exponential backoff is enabled.
    /// For example, with <c>BackoffMs = 100</c> and <c>BackoffMultiplier = 2.0</c>:
    /// - 1st retry: 100ms delay
    /// - 2nd retry: 200ms delay
    /// - 3rd retry: 400ms delay
    /// </remarks>
    public int BackoffMs { get; set; }

    /// <summary>
    /// Gets or sets the multiplier applied to <see cref="BackoffMs"/> for each subsequent retry attempt.
    /// </summary>
    /// <remarks>
    /// Default is 2.0 (doubling the delay each time). Only used when <see cref="BackoffMs"/> is greater than 0.
    /// Set to 1.0 for a constant delay between retries.
    /// </remarks>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the exception types that should trigger a retry.
    /// When set, only exceptions that are assignable to one of these types will cause a retry.
    /// </summary>
    /// <remarks>
    /// Default is null (retry on any exception). The check uses <see cref="Type.IsInstanceOfType"/>
    /// so derived exception types are also matched.
    /// </remarks>
    public Type[]? RetryOnExceptionTypes { get; set; }

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
    /// Can be overridden in derived classes to implement conditional retry logic
    /// based on the specific exception type or other criteria.
    ///
    /// The default implementation checks <see cref="RetryOnExceptionTypes"/> if set.
    /// If no exception type filter is configured, it returns true for any exception.
    /// </remarks>
    public virtual Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
    {
        if (RetryOnExceptionTypes is { Length: > 0 })
        {
            foreach (var type in RetryOnExceptionTypes)
            {
                if (type.IsInstanceOfType(exception))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }


    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetRetryLimit(Times, ShouldRetry);
        context.SetRetryBackoff(BackoffMs, BackoffMultiplier);
        return default(ValueTask);
    }

    public Type ScopeType => typeof(RetryAttribute);
}

using System.Diagnostics;
using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that an assertion passes within a specified timeout by polling repeatedly.
/// Useful for testing asynchronous or event-driven code where state changes take time to propagate.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class WaitsForAssertion<TValue> : Assertion<TValue>
{
    private readonly Func<IAssertionSource<TValue>, Assertion<TValue>> _assertionBuilder;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _pollingInterval;
    private TValue? _successfulValue;
    private bool _hasSuccessfulValue;

    public WaitsForAssertion(
        AssertionContext<TValue> context,
        Func<IAssertionSource<TValue>, Assertion<TValue>> assertionBuilder,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null)
        : base(context)
    {
        _assertionBuilder = assertionBuilder ?? throw new ArgumentNullException(nameof(assertionBuilder));
        _timeout = timeout;
        _pollingInterval = pollingInterval ?? TimeSpan.FromMilliseconds(10);

        if (_timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be positive", nameof(timeout));
        }

        if (_pollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentException("Polling interval must be positive", nameof(pollingInterval));
        }
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;
        var attemptCount = 0;

        using var cts = new CancellationTokenSource(_timeout);

        while (stopwatch.Elapsed < _timeout)
        {
            attemptCount++;

            try
            {
                var (currentValue, currentException) = await Context.Evaluation.ReevaluateAsync();
                var assertionSource = new ValueAssertion<TValue>(currentValue, "polled value");
                var assertion = _assertionBuilder(assertionSource);
                await assertion.AssertAsync();

                // Store the successful value so we can return it
                _successfulValue = currentValue;
                _hasSuccessfulValue = true;
                
                return AssertionResult.Passed;
            }
            catch (AssertionException ex)
            {
                lastException = ex;

                // Check if we've exceeded timeout before waiting
                if (stopwatch.Elapsed + _pollingInterval >= _timeout)
                {
                    break;
                }

                try
                {
                    await Task.Delay(_pollingInterval, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();

        var lastErrorMessage = lastException != null
            ? $"Last error: {ExtractAssertionMessage(lastException)}"
            : "No attempts were made";

        return AssertionResult.Failed(
            $"assertion did not pass within {_timeout.TotalMilliseconds:F0}ms after {attemptCount} attempts. {lastErrorMessage}");
    }

    /// <summary>
    /// Overrides AssertAsync to return the successfully polled value instead of the initial value.
    /// This allows users to capture the value that satisfied the assertion.
    /// </summary>
    public override async Task<TValue?> AssertAsync()
    {
        // Execute the assertion using the base implementation
        await base.AssertAsync();
        
        // Return the successful value if we have one, otherwise fall back to the context value
        return _hasSuccessfulValue ? _successfulValue : (await Context.GetAsync()).Value;
    }

    protected override string GetExpectation() =>
        $"assertion to pass within {_timeout.TotalMilliseconds:F0} milliseconds " +
        $"(polling every {_pollingInterval.TotalMilliseconds:F0}ms)";

    /// <summary>
    /// Extracts the core assertion message from an exception,
    /// removing the stack trace and location info for cleaner output.
    /// </summary>
    private static string ExtractAssertionMessage(Exception exception)
    {
        var message = exception.Message;
        var atIndex = message.IndexOf("\nat Assert.That", StringComparison.Ordinal);

        if (atIndex > 0)
        {
            message = message.Substring(0, atIndex).Trim();
        }

        return message;
    }
}

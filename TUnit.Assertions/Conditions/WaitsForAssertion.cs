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
    private readonly CancellationToken _cancellationToken;

    public WaitsForAssertion(
        AssertionContext<TValue> context,
        Func<IAssertionSource<TValue>, Assertion<TValue>> assertionBuilder,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
        : base(context)
    {
        _assertionBuilder = assertionBuilder ?? throw new ArgumentNullException(nameof(assertionBuilder));
        _timeout = timeout;
        _pollingInterval = pollingInterval ?? TimeSpan.FromMilliseconds(10);
        _cancellationToken = cancellationToken;

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

        // Link the supplied cancellation token with an internal timeout source so the polling
        // loop honours both: external cancellation propagates as OperationCanceledException,
        // and the internal timeout still produces the standard AssertionResult.Failed path.
        // When the caller did not supply a cancellable token, the linking step is skipped to
        // avoid the registration overhead.
        using var linkedCts = _cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken)
            : new CancellationTokenSource();
        linkedCts.CancelAfter(_timeout);

        while (stopwatch.Elapsed < _timeout)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            attemptCount++;

            try
            {
                var (currentValue, currentException) = await Context.Evaluation.ReevaluateAsync();
                var assertionSource = new ValueAssertion<TValue>(currentValue, "polled value");
                var assertion = _assertionBuilder(assertionSource);
                await assertion.AssertAsync();

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
                    await Task.Delay(_pollingInterval, linkedCts.Token);
                }
                catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
                {
                    throw;
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

        // Inner exception is the last AssertionException (not an originating cause) — kept whole
        // because it carries the formatted assertion message useful for debugging.
        return AssertionResult.Failed(
            $"assertion did not pass within {_timeout.TotalMilliseconds:F0}ms after {attemptCount} attempts. {lastErrorMessage}", lastException);
    }

    /// <summary>
    /// Executes the assertion and returns the resolved value upon success.
    /// This enables the pattern: Entity entity = await Assert.That(getEntity).WaitsFor(...);
    /// </summary>
    public override async Task<TValue?> AssertAsync()
    {
        // Execute the base assertion logic (which calls CheckAsync and handles the result)
        await base.AssertAsync();

        // After CheckAsync succeeds, the context contains the updated value
        // from the successful ReevaluateAsync call
        var (value, _) = await Context.GetAsync();
        return value;
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

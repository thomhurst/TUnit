namespace TUnit.Assertions.Core;

/// <summary>
/// Represents the evaluation context for an assertion chain.
/// Handles lazy evaluation, caching, and exception capture.
/// Shared by all assertions in a chain - evaluates once, caches result.
/// </summary>
/// <typeparam name="TValue">The type of value being evaluated</typeparam>
public sealed class EvaluationContext<TValue>
{
    private readonly Func<Task<(TValue?, Exception?)>>? _evaluator;
    private TValue? _value;
    private Exception? _exception;
    private bool _evaluated;
    private DateTimeOffset _startTime;
    private DateTimeOffset _endTime;

    /// <summary>
    /// Creates a context for lazy evaluation (delegates, funcs, tasks)
    /// </summary>
    public EvaluationContext(Func<Task<(TValue?, Exception?)>> evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <summary>
    /// Creates a context for immediate values (no evaluation needed)
    /// </summary>
    public EvaluationContext(TValue? value)
    {
        _value = value;
        _evaluated = true;
        _startTime = _endTime = DateTimeOffset.Now;
    }

    /// <summary>
    /// Gets the evaluated value and any exception that occurred.
    /// Evaluates once and caches the result for subsequent calls.
    /// </summary>
    public async Task<(TValue? Value, Exception? Exception)> GetAsync()
    {
        if (!_evaluated)
        {
            _startTime = DateTimeOffset.Now;
            (_value, _exception) = await _evaluator!();
            _endTime = DateTimeOffset.Now;
            _evaluated = true;
        }
        return (_value, _exception);
    }

    /// <summary>
    /// Creates a derived context by mapping the value to a different type.
    /// Used for type transformations like IsTypeOf&lt;T&gt;().
    /// The mapping function is only called if evaluation succeeds (no exception).
    /// </summary>
    public EvaluationContext<TNew> Map<TNew>(Func<TValue?, TNew?> mapper)
    {
        return new EvaluationContext<TNew>(async () =>
        {
            var (value, exception) = await GetAsync();
            if (exception != null)
            {
                return (default(TNew), exception);
            }

            try
            {
                return (mapper(value), null);
            }
            catch (Exception ex)
            {
                return (default(TNew), ex);
            }
        });
    }

    public EvaluationContext<TException> MapException<TException>() where TException : Exception
    {
        return new EvaluationContext<TException>(async () =>
        {
            var (value, exception) = await GetAsync();
            if (exception is TException tException)
            {
                return (tException, null);
            }

            return (null, exception);
        });
    }

    /// <summary>
    /// Gets the timing information for this evaluation.
    /// Only meaningful after evaluation has occurred.
    /// </summary>
    public (DateTimeOffset Start, DateTimeOffset End) GetTiming() => (_startTime, _endTime);
}

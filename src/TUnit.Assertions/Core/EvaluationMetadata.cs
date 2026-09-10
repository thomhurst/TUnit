namespace TUnit.Assertions.Core;

/// <summary>
/// Contains metadata about the evaluation of an assertion source value.
/// Includes the evaluated value, any exception that occurred, and timing information.
/// This is passed to CheckAsync to enable assertions to access execution timing and other metadata.
/// </summary>
/// <typeparam name="TValue">The type of value being evaluated</typeparam>
public readonly struct EvaluationMetadata<TValue>
{
    /// <summary>
    /// The evaluated value. May be null if evaluation failed or returned null.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Any exception that occurred during evaluation. Null if evaluation succeeded.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// The time when evaluation started.
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// The time when evaluation completed.
    /// </summary>
    public DateTimeOffset EndTime { get; }

    /// <summary>
    /// The duration of the evaluation.
    /// Computed as EndTime - StartTime.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Creates evaluation metadata from an evaluation result and timing information.
    /// </summary>
    /// <param name="value">The evaluated value</param>
    /// <param name="exception">Any exception that occurred during evaluation</param>
    /// <param name="startTime">When evaluation started</param>
    /// <param name="endTime">When evaluation completed</param>
    public EvaluationMetadata(TValue? value, Exception? exception, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        Value = value;
        Exception = exception;
        StartTime = startTime;
        EndTime = endTime;
    }
}

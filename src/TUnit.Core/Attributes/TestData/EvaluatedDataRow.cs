namespace TUnit.Core;

/// <summary>
/// Shares a row evaluated during discovery and its original factory across all consumers.
/// </summary>
internal sealed class EvaluatedDataRow(object?[]? row, Func<Task<object?[]?>> factory)
{
    private object?[]? _row = row;

    internal object?[]? Row => _row;

    // A null row retains the existing behavior of fetching from the original factory.
    internal Task<object?[]?> FetchAsync()
    {
        var evaluated = Interlocked.Exchange(ref _row, null);
        return evaluated is not null ? Task.FromResult<object?[]?>(evaluated) : factory();
    }
}

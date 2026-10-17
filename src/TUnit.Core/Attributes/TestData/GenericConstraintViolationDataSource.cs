using System.ComponentModel;
using TUnit.Core.Exceptions;

namespace TUnit.Core;

/// <summary>
/// Stands in for a data source whose inferred generic type arguments break the test's generic
/// constraints. The source generator cannot emit a call to the test with those type arguments, so
/// it emits this data source instead. Each of its rows throws
/// <see cref="GenericTypeResolutionException"/>, so the engine reports one failed test per row with
/// the same message that reflection mode produces, rather than the rows silently disappearing.
/// </summary>
/// <remarks>
/// This type is used by generated code and by the engine. It is not meant to be applied by users.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GenericConstraintViolationDataSource : IDataSourceAttribute
{
    private readonly IDataSourceAttribute? _rowSource;

    /// <summary>
    /// Creates a data source with a single row that fails test building with <paramref name="message"/>.
    /// </summary>
    /// <param name="message">Describes the constraint that the inferred type argument breaks.</param>
    public GenericConstraintViolationDataSource(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Creates a data source that yields one failing row for each row of <paramref name="rowSource"/>,
    /// so a data source with several rows reports one failed test per row, as reflection mode does.
    /// The rows' values are never generated.
    /// </summary>
    /// <param name="message">Describes the constraint that the inferred type argument breaks.</param>
    /// <param name="rowSource">The data source whose rows inferred the invalid type argument.</param>
    public GenericConstraintViolationDataSource(string message, IDataSourceAttribute rowSource)
    {
        Message = message;
        _rowSource = rowSource;
    }

    /// <summary>
    /// Gets the message of the <see cref="GenericTypeResolutionException"/> raised when a row is generated.
    /// </summary>
    public string Message { get; }

    /// <inheritdoc />
    /// <remarks>Always <c>false</c>: the rows must surface the constraint failure.</remarks>
    public bool SkipIfEmpty { get => false; set { } }

    /// <inheritdoc />
    /// <remarks>Always <c>false</c>: every row fails, so deferring them would gain nothing.</remarks>
    public bool DeferEnumeration { get => false; set { } }

    /// <inheritdoc />
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var message = Message;
        Func<Task<object?[]?>> failingRow = () => Task.FromException<object?[]?>(new GenericTypeResolutionException(message));

        var rowCount = _rowSource is null ? 1 : await CountRowsAsync(_rowSource, dataGeneratorMetadata).ConfigureAwait(false);

        // Never yield nothing: an empty or failing row source must still report the violation once.
        for (var i = 0; i < Math.Max(rowCount, 1); i++)
        {
            yield return failingRow;
        }
    }

    private static async Task<int> CountRowsAsync(IDataSourceAttribute rowSource, DataGeneratorMetadata dataGeneratorMetadata)
    {
        var count = 0;

        try
        {
            // Only the row factories are counted. They are not invoked, so no row values are created.
            await foreach (var _ in rowSource.GetDataRowsAsync(dataGeneratorMetadata).ConfigureAwait(false))
            {
                count++;
            }
        }
        catch (Exception)
        {
            // The row source cannot be enumerated without a valid instantiation (for example, an
            // instance data source). Report the violation for the rows counted so far (at least
            // once) instead of the enumeration error, which would hide the real cause.
        }

        return count;
    }
}

using System.ComponentModel;
using TUnit.Core.Exceptions;

namespace TUnit.Core;

/// <summary>
/// Stands in for a data source whose inferred generic type arguments break the test's generic
/// constraints. The source generator cannot emit a call to the test with those type arguments, so it
/// emits this data source instead. It wraps the original data source and yields one row for each of
/// its rows, and each row throws <see cref="GenericTypeResolutionException"/>, so the engine reports one
/// failed test per row with the same message that reflection mode produces, rather than the rows
/// silently disappearing. Without a wrapped data source it yields a single failing row.
/// </summary>
/// <remarks>
/// This type is used by generated code and by the engine. It is not meant to be applied by users. It
/// derives from <see cref="Attribute"/> because the engine adds a test's data source to the test's attributes.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class GenericConstraintViolationDataSource : Attribute, IDataSourceAttribute
{
    private GenericConstraintViolationDataSource(string message, IDataSourceAttribute? innerDataSource)
    {
        Message = message;
        InnerDataSource = innerDataSource;
    }

    /// <summary>
    /// Creates a data source with a single row that fails test building with <paramref name="message"/>.
    /// </summary>
    /// <param name="message">Describes the constraint that the inferred type argument breaks.</param>
    public static GenericConstraintViolationDataSource Create(string message)
    {
        return new GenericConstraintViolationDataSource(message, null);
    }

    /// <summary>
    /// Creates a data source that fails each row of <paramref name="innerDataSource"/> with <paramref name="message"/>.
    /// </summary>
    /// <param name="message">Describes the constraint that the inferred type argument breaks.</param>
    /// <param name="innerDataSource">The data source whose rows inferred the constraint-breaking type arguments.</param>
    /// <returns>
    /// A wrapper that implements <see cref="IAccessesInstanceData"/> when <paramref name="innerDataSource"/> does,
    /// so the engine still supplies the test class instance that the inner source needs to yield its rows.
    /// </returns>
    public static GenericConstraintViolationDataSource Create(string message, IDataSourceAttribute innerDataSource)
    {
        return innerDataSource is IAccessesInstanceData
            ? new InstanceDataAccessing(message, innerDataSource)
            : new GenericConstraintViolationDataSource(message, innerDataSource);
    }

    /// <summary>
    /// Gets the message of the <see cref="GenericTypeResolutionException"/> raised for each row.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the data source whose rows inferred the constraint-breaking type arguments, if any.
    /// </summary>
    public IDataSourceAttribute? InnerDataSource { get; }

    /// <inheritdoc />
    /// <remarks>Forwards to the inner data source, so an empty source behaves as it would without the wrapper.</remarks>
    public bool SkipIfEmpty
    {
        get => InnerDataSource?.SkipIfEmpty ?? false;
        set
        {
            if (InnerDataSource is not null)
            {
                InnerDataSource.SkipIfEmpty = value;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>Forwards to the inner data source.</remarks>
    public bool DeferEnumeration
    {
        get => InnerDataSource?.DeferEnumeration ?? false;
        set
        {
            if (InnerDataSource is not null)
            {
                InnerDataSource.DeferEnumeration = value;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var message = Message;

        if (InnerDataSource is null)
        {
            yield return () => Task.FromException<object?[]?>(new GenericTypeResolutionException(message));
            yield break;
        }

        await foreach (var rowFactory in InnerDataSource.GetDataRowsAsync(dataGeneratorMetadata))
        {
            // Run the row's own factory first, as reflection mode does before it resolves the row's type
            // arguments, so the factory's side effects happen and its own exceptions take precedence.
            // The exception carries the row so the engine can track its objects for disposal (#7123).
            yield return async () =>
            {
                var row = await rowFactory();
                throw new GenericConstraintViolationRowException(message, row);
            };
        }
    }

    private sealed class InstanceDataAccessing(string message, IDataSourceAttribute innerDataSource)
        : GenericConstraintViolationDataSource(message, innerDataSource), IAccessesInstanceData;
}

/// <summary>
/// Raised for a row of a <see cref="GenericConstraintViolationDataSource"/> that wraps another data source.
/// It carries the objects that the wrapped row's factory created, which the failed test never receives,
/// so the engine can track them for disposal. The engine reports a plain
/// <see cref="GenericTypeResolutionException"/> with the same message, as reflection mode does.
/// </summary>
internal sealed class GenericConstraintViolationRowException(string message, object?[]? row)
    : GenericTypeResolutionException(message)
{
    /// <summary>
    /// Gets the values that the wrapped row's factory returned, before unwrapping.
    /// </summary>
    public object?[]? Row { get; } = row;
}

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
    /// <remarks>Mirrors the inner data source, so an empty source behaves as it would without the wrapper.</remarks>
    public bool SkipIfEmpty { get => InnerDataSource?.SkipIfEmpty ?? false; set { } }

    /// <inheritdoc />
    /// <remarks>Mirrors the inner data source.</remarks>
    public bool DeferEnumeration { get => InnerDataSource?.DeferEnumeration ?? false; set { } }

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
            yield return async () =>
            {
                await rowFactory();
                throw new GenericTypeResolutionException(message);
            };
        }
    }

    private sealed class InstanceDataAccessing(string message, IDataSourceAttribute innerDataSource)
        : GenericConstraintViolationDataSource(message, innerDataSource), IAccessesInstanceData;
}

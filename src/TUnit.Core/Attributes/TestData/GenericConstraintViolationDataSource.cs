using System.ComponentModel;
using TUnit.Core.Exceptions;

namespace TUnit.Core;

/// <summary>
/// Stands in for a data source whose inferred generic type arguments break the test method's
/// generic constraints. The source generator cannot emit a call to the method with those type
/// arguments, so it emits this data source instead. Its single row throws
/// <see cref="GenericTypeResolutionException"/>, so the engine reports a failed test with the same
/// message that reflection mode produces, rather than the row silently disappearing.
/// </summary>
/// <remarks>
/// This type is used by generated code and is not meant to be applied by users.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GenericConstraintViolationDataSource : IDataSourceAttribute
{
    /// <summary>
    /// Creates a data source that fails test building with <paramref name="message"/>.
    /// </summary>
    /// <param name="message">Describes the constraint that the inferred type argument breaks.</param>
    public GenericConstraintViolationDataSource(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the message of the <see cref="GenericTypeResolutionException"/> raised when the row is generated.
    /// </summary>
    public string Message { get; }

    /// <inheritdoc />
    /// <remarks>Always <c>false</c>: the single row must surface the constraint failure.</remarks>
    public bool SkipIfEmpty { get => false; set { } }

    /// <inheritdoc />
    /// <remarks>Always <c>false</c>: there is a single row, so deferring it would gain nothing.</remarks>
    public bool DeferEnumeration { get => false; set { } }

    /// <inheritdoc />
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var message = Message;
        yield return () => Task.FromException<object?[]?>(new GenericTypeResolutionException(message));
        await default(ValueTask);
    }
}

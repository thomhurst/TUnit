using System.Collections.Concurrent;
using System.ComponentModel;

namespace TUnit.Core.Helpers;

/// <summary>
/// Readers for the <c>DisplayName</c>, <c>Skip</c> and <c>Categories</c> properties a custom data source
/// attribute declares, emitted by the source generator so the engine reads them without reflection.
/// Used by generated code; not intended to be called directly.
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class DataSourceRowMetadataReaders
{
    private static readonly ConcurrentDictionary<Type, Func<IDataSourceAttribute, TestDataRowMetadata?>> Readers = new();

    /// <summary>
    /// Registers how to read the row settings of <typeparamref name="T"/> and returns <paramref name="dataSource"/>.
    /// </summary>
    public static T Register<T>(T dataSource, Func<T, (string? DisplayName, string? Skip, string[]? Categories)> reader)
        where T : class, IDataSourceAttribute
    {
        // Runs for every data source the generated code creates; only the first of each type registers.
        if (!Readers.ContainsKey(typeof(T)))
        {
            Add(reader);
        }

        return dataSource;
    }

    // Separate so the capturing closure is allocated only when a reader is added.
    private static void Add<T>(Func<T, (string? DisplayName, string? Skip, string[]? Categories)> reader)
        where T : class, IDataSourceAttribute
    {
        Readers.TryAdd(typeof(T), source =>
        {
            var (displayName, skip, categories) = reader((T) source);

            return displayName is null && skip is null && categories is null
                ? null
                : new TestDataRowMetadata(displayName, null, skip, categories);
        });
    }

    internal static bool TryRead(IDataSourceAttribute dataSource, out TestDataRowMetadata? metadata)
    {
        if (Readers.TryGetValue(dataSource.GetType(), out var reader))
        {
            metadata = reader(dataSource);
            return true;
        }

        metadata = null;
        return false;
    }
}

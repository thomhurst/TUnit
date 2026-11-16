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
        // Runs for every data source the generated code creates; all but the first of each type pay one static read.
        if (!Registration<T>.Done)
        {
            Add(reader);
        }

        return dataSource;
    }

    // Separate so the capturing closure is allocated only when a reader is added.
    private static void Add<T>(Func<T, (string? DisplayName, string? Skip, string[]? Categories)> reader)
        where T : class, IDataSourceAttribute
    {
        // Replaces a reflection reader the engine added for an instance it met before the generated code ran.
        Readers[typeof(T)] = source =>
        {
            var (displayName, skip, categories) = reader((T) source);

            return TestDataRowMetadata.FromDataSource(displayName, skip, categories);
        };

        Registration<T>.Done = true;
    }

    /// <summary>
    /// The reader registered for <paramref name="type"/>, or the one <paramref name="fallback"/> creates, which
    /// is then kept: a row costs one lookup whichever kind of reader its data source has.
    /// </summary>
    internal static Func<IDataSourceAttribute, TestDataRowMetadata?> GetOrAdd(Type type, Func<Type, Func<IDataSourceAttribute, TestDataRowMetadata?>> fallback)
    {
        return Readers.GetOrAdd(type, fallback);
    }

    private static class Registration<T>
    {
        public static bool Done;
    }
}

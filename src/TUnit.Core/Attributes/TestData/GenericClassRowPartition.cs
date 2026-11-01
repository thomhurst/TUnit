using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// What a generic class instantiation uses for its constructor arguments when no row of its partitioned data
/// sources infers its class type arguments at run time.
/// </summary>
/// <remarks>Used by generated code. It is not meant to be used directly.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum GenericClassRowFallback
{
    /// <summary>
    /// A class row known at compile time infers the instantiation, so the partitioned sources contribute only
    /// the rows that infer it, possibly none.
    /// </summary>
    None,

    /// <summary>
    /// Every row of every class data source is used, converted to the constructor's parameter types (#7161).
    /// </summary>
    AllRows,

    /// <summary>
    /// The class has class-level [Arguments] rows and none of them infers the instantiation, so no class row
    /// supplies its constructor arguments. A single empty row is yielded, and the instance factory fails for it.
    /// </summary>
    NoClassRow,
}

/// <summary>
/// Splits the rows of class-level data sources whose row types the source generator cannot see (such as a
/// <see cref="MethodDataSourceAttribute"/> returning <c>IEnumerable&lt;object&gt;</c>) across the instantiations of
/// a generic test class, by the class type arguments that each row's values infer. Reflection mode groups class
/// rows the same way at discovery, so both modes run each row on the same instantiation (#7210).
/// </summary>
/// <remarks>Used by generated code. It is not meant to be used directly.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GenericClassRowPartition
{
    /// <summary>
    /// Wraps the class data sources of one instantiation so that the sources at <paramref name="partitionedIndices"/>
    /// yield only the rows whose values infer <paramref name="classTypeArguments"/>.
    /// </summary>
    /// <param name="classTypeArguments">The instantiation's class type arguments.</param>
    /// <param name="fallback">
    /// What to use when no partitioned row infers the instantiation. Unless it is <see cref="GenericClassRowFallback.None"/>,
    /// the sources that are not partitioned are used only in that case.
    /// </param>
    /// <param name="dataSources">The instantiation's class data sources, in declaration order.</param>
    /// <param name="partitionedIndices">The indices of the sources in <paramref name="dataSources"/> to partition.</param>
    /// <returns>The data sources to give the test, in the same order.</returns>
    public static IDataSourceAttribute[] Create(
        Type[] classTypeArguments,
        GenericClassRowFallback fallback,
        IDataSourceAttribute[] dataSources,
        params int[] partitionedIndices)
    {
        var coordinator = new Coordinator(classTypeArguments, fallback, dataSources, partitionedIndices);
        var result = new IDataSourceAttribute[dataSources.Length];

        for (var i = 0; i < dataSources.Length; i++)
        {
            var isPartitioned = Array.IndexOf(partitionedIndices, i) >= 0;
            result[i] = isPartitioned || fallback != GenericClassRowFallback.None
                ? new GenericClassRowPartitionDataSource(coordinator, dataSources[i], i, isPartitioned)
                : dataSources[i];
        }

        return result;
    }

    /// <summary>
    /// Whether a class row's values infer <paramref name="classTypeArguments"/>, as reflection mode infers class
    /// type arguments (<c>ReflectionGenericTypeResolver.DetermineGenericTypeArguments</c> in TUnit.Engine): with one
    /// type parameter, from the run-time type of the first non-null value; with several, from the run-time type of
    /// each leading value in turn. Keep the two in step.
    /// </summary>
    internal static bool InfersTypeArguments(object?[]? row, Type[] classTypeArguments)
    {
        if (row is null || row.Length == 0)
        {
            return false;
        }

        if (classTypeArguments.Length == 1)
        {
            foreach (var value in row)
            {
                if (value is not null)
                {
                    return value.GetType() == classTypeArguments[0];
                }
            }

            return false;
        }

        if (row.Length < classTypeArguments.Length)
        {
            return false;
        }

        for (var i = 0; i < classTypeArguments.Length; i++)
        {
            if (row[i]?.GetType() != classTypeArguments[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads every partitioned source once per test build and decides, across all of them, whether any row
    /// infers the instantiation. One build enumerates the class data sources in order, so the first wrapper
    /// it reaches evaluates its siblings too, and they reuse that evaluation.
    /// </summary>
    internal sealed class Coordinator(
        Type[] classTypeArguments,
        GenericClassRowFallback fallback,
        IDataSourceAttribute[] dataSources,
        int[] partitionedIndices)
    {
        private readonly object _lock = new();
        private object? _buildKey;
        private Task<Evaluation>? _evaluation;

        public Type[] ClassTypeArguments { get; } = classTypeArguments;

        public GenericClassRowFallback Fallback { get; } = fallback;

        public Task<Evaluation> GetEvaluationAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            // Each test build has its own context accessor, so a later build reads the sources again,
            // as it would without the partition.
            var buildKey = dataGeneratorMetadata.TestBuilderContext;
            TaskCompletionSource<Evaluation> completion;

            lock (_lock)
            {
                if (_evaluation is not null && ReferenceEquals(_buildKey, buildKey))
                {
                    return _evaluation;
                }

                completion = new TaskCompletionSource<Evaluation>(TaskCreationOptions.RunContinuationsAsynchronously);
                _buildKey = buildKey;
                _evaluation = completion.Task;
            }

            // Run the sources' code outside the lock
            _ = EvaluateAsync(dataGeneratorMetadata, completion);
            return completion.Task;
        }

        private async Task EvaluateAsync(DataGeneratorMetadata dataGeneratorMetadata, TaskCompletionSource<Evaluation> completion)
        {
            try
            {
                var rowsByIndex = new List<EvaluatedRow>?[dataSources.Length];
                var anyRowInfers = false;

                foreach (var index in partitionedIndices)
                {
                    var rows = new List<EvaluatedRow>();
                    await foreach (var rowFactory in dataSources[index].GetDataRowsAsync(dataGeneratorMetadata).ConfigureAwait(false))
                    {
                        var row = await rowFactory().ConfigureAwait(false);
                        var infers = InfersTypeArguments(row, ClassTypeArguments);
                        anyRowInfers |= infers;
                        rows.Add(new EvaluatedRow(rowFactory, row, infers));
                    }

                    rowsByIndex[index] = rows;
                }

                completion.SetResult(new Evaluation(rowsByIndex, anyRowInfers));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }
    }

    /// <summary>A row of a partitioned source, evaluated once to read its values' types.</summary>
    internal readonly record struct EvaluatedRow(Func<Task<object?[]?>> Factory, object?[]? Row, bool InfersTypeArguments);

    /// <summary>The evaluated rows of each partitioned source, by data source index, and whether any of them infers the instantiation.</summary>
    internal sealed record Evaluation(List<EvaluatedRow>?[] RowsByIndex, bool AnyRowInfers);
}

/// <summary>
/// One class data source of a generic test class instantiation, as partitioned by <see cref="GenericClassRowPartition"/>.
/// The engine treats <see cref="InnerDataSource"/> as the test's data source for attributes and property injection.
/// </summary>
internal sealed class GenericClassRowPartitionDataSource : Attribute, IDataSourceAttribute
{
    private readonly GenericClassRowPartition.Coordinator _coordinator;
    private readonly int _index;
    private readonly bool _isPartitioned;

    public GenericClassRowPartitionDataSource(
        GenericClassRowPartition.Coordinator coordinator,
        IDataSourceAttribute innerDataSource,
        int index,
        bool isPartitioned)
    {
        _coordinator = coordinator;
        InnerDataSource = innerDataSource;
        _index = index;
        _isPartitioned = isPartitioned;
    }

    /// <summary>The data source whose rows this partition yields a subset of.</summary>
    public IDataSourceAttribute InnerDataSource { get; }

    /// <inheritdoc />
    /// <remarks>
    /// A partition may be empty because its rows belong to other instantiations, as in reflection mode, where
    /// such a partition does not exist; so it never reports a skipped test of its own.
    /// </remarks>
    public bool SkipIfEmpty
    {
        get => false;
        set => InnerDataSource.SkipIfEmpty = value;
    }

    /// <inheritdoc />
    public bool DeferEnumeration
    {
        get => InnerDataSource.DeferEnumeration;
        set => InnerDataSource.DeferEnumeration = value;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var evaluation = await _coordinator.GetEvaluationAsync(dataGeneratorMetadata).ConfigureAwait(false);

        if (!evaluation.AnyRowInfers && _coordinator.Fallback == GenericClassRowFallback.NoClassRow)
        {
            if (_index == 0)
            {
                // No row supplies the constructor arguments. The first source yields one empty row for all
                // of them, and the instance factory fails for it once per method row, as in reflection mode
                yield return static () => Task.FromResult<object?[]?>([]);
            }

            yield break;
        }

        if (!_isPartitioned)
        {
            // Not partitioned and the fallback applies: used only when no partitioned row infers the instantiation
            if (!evaluation.AnyRowInfers)
            {
                await foreach (var rowFactory in InnerDataSource.GetDataRowsAsync(dataGeneratorMetadata).ConfigureAwait(false))
                {
                    yield return rowFactory;
                }
            }

            yield break;
        }

        var useEveryRow = !evaluation.AnyRowInfers && _coordinator.Fallback == GenericClassRowFallback.AllRows;
        foreach (var evaluatedRow in evaluation.RowsByIndex[_index] ?? [])
        {
            if (useEveryRow || evaluatedRow.InfersTypeArguments)
            {
                yield return FirstFetchReturnsEvaluatedRow(evaluatedRow);
            }
        }
    }

    /// <summary>
    /// The first fetch returns the values that were evaluated to choose the row, so no extra objects are made.
    /// Later fetches, such as for a repeated test, call the source's own factory, as they would without the partition.
    /// </summary>
    private static Func<Task<object?[]?>> FirstFetchReturnsEvaluatedRow(GenericClassRowPartition.EvaluatedRow evaluatedRow)
    {
        var evaluated = evaluatedRow.Row;
        var factory = evaluatedRow.Factory;
        return () =>
        {
            var row = Interlocked.Exchange(ref evaluated, null);
            return row is not null ? Task.FromResult<object?[]?>(row) : factory();
        };
    }
}

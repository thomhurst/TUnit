using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Core.Helpers;

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
        var consumerCount = 0;

        for (var i = 0; i < dataSources.Length; i++)
        {
            var isPartitioned = Array.IndexOf(partitionedIndices, i) >= 0;
            if (isPartitioned || fallback != GenericClassRowFallback.None)
            {
                result[i] = new GenericClassRowPartitionDataSource(coordinator, dataSources[i], i, isPartitioned);
                consumerCount++;
            }
            else
            {
                result[i] = dataSources[i];
            }
        }

        coordinator.ConsumerCount = consumerCount;
        return result;
    }

    /// <summary>
    /// Configures a generated class source with its declared inference and optional inferred-row filtering.
    /// Own-class null values are retained because their declared types supply the class type arguments.
    /// </summary>
    /// <remarks>Used only by generated code. Explicit instantiations retain raw rows for partition fallback.</remarks>
    /// <param name="source">The generated class data source.</param>
    /// <param name="filterRows">Whether to suppress empty fallback rows and all-null external rows immediately.</param>
    /// <param name="classTypeArguments">Declared class type arguments, when needed for runtime partitioning.</param>
    /// <returns>The configured source. Evaluated row values are reused on the first fetch only.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IDataSourceAttribute ConfigureInferredSource(IDataSourceAttribute source, bool filterRows, params Type[] classTypeArguments)
    {
        // Generated emission creates a fresh source instance for each closed instantiation.
        // Keep MethodDataSource's configuration on that instance: its reader and ToInstanceVariant
        // consume the same settings. Other source kinds have no inference properties, so their
        // declared constructor mapping is held weakly, without extending the source lifetime.
        // An immutable wrapper would also have to preserve attribute identity and conversion.
        if (source is MethodDataSourceAttribute methodSource)
        {
            methodSource.FilterClassRows = filterRows;
            if (filterRows)
            {
                // Inferred sources contribute only eligible class rows, including when empty.
                // Raw explicit/fallback sources retain their original empty-source behavior.
                methodSource.SkipIfEmpty = false;
            }
            methodSource.InferredClassTypeArguments = classTypeArguments.Length == 0 ? null : classTypeArguments;
        }
        else if (classTypeArguments.Length > 0)
        {
            DeclaredRowTypes.Add(source, classTypeArguments);
        }
        return source;
    }

    // Arguments rows infer through constructor positions at generation time, rather than
    // through the first non-null runtime value. Keep that mapping on the original attribute.
    private static readonly ConditionalWeakTable<IDataSourceAttribute, Type[]> DeclaredRowTypes = new();

    // External class rows need a runtime value to infer their type arguments in either mode.
    internal static bool HasNonNullValues(object?[] row) =>
        TestDataRowUnwrapper.UnwrapArray(row).Data.Any(value => value is not null);

    /// <summary>
    /// Invokes the factory of a row holding a <see cref="TestDataRow{T}"/> of <see cref="Func{TResult}"/> once, and
    /// returns a row that wraps its values with the same settings. Inferring the row's type arguments and building its
    /// test then read the same values, instead of each invoking the factory (#7210). Other rows are returned as they are.
    /// </summary>
    internal static object?[] EvaluateWrappedFactory(object?[] row)
    {
        if (row.Length != 1 || row[0] is not ITestDataRow wrapped)
        {
            return row;
        }

        var data = wrapped.GetData();
        if (data is null || !data.GetType().IsGenericType || data.GetType().GetGenericTypeDefinition() != typeof(Func<>))
        {
            return row;
        }

        // An object?[] is used as the row's values without being invoked or spread again
        var (values, _) = TestDataRowUnwrapper.UnwrapArray(row);
        return [new TestDataRow<object?[]>(values, wrapped.DisplayName, wrapped.Skip, wrapped.Categories, wrapped.DataExpression)];
    }

    /// <summary>
    /// Whether a class row's values infer <paramref name="classTypeArguments"/>, by <see cref="InferClassTypeArguments"/>.
    /// </summary>
    internal static bool InfersTypeArguments(object?[]? row, Type[] classTypeArguments)
    {
        if (row is null || InferClassTypeArguments(row, classTypeArguments.Length) is not { } inferred)
        {
            return false;
        }

        for (var i = 0; i < inferred.Length; i++)
        {
            if (inferred[i] != classTypeArguments[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The class type arguments that a class row's values infer. Both modes use this rule: reflection mode groups
    /// class rows by it at discovery (<c>ReflectionGenericTypeResolver.DetermineGenericTypeArguments</c> in
    /// TUnit.Engine), and source-generated mode partitions them by it at run time. A row holding a
    /// <see cref="TestDataRow{T}"/> infers from the values it wraps, which are the values the test receives. With one
    /// type parameter, the run-time type of the first non-null value is used; with several, the run-time type of each
    /// leading value in turn. Returns null when the values cannot infer every type parameter.
    /// </summary>
    internal static Type[]? InferClassTypeArguments(object?[] row, int typeParameterCount)
    {
        var (values, _) = TestDataRowUnwrapper.UnwrapArray(row);

        if (values.Length == 0 || typeParameterCount == 0)
        {
            return null;
        }

        if (typeParameterCount == 1)
        {
            foreach (var value in values)
            {
                if (value is not null)
                {
                    return [value.GetType()];
                }
            }

            return null;
        }

        if (values.Length < typeParameterCount)
        {
            return null;
        }

        var typeArguments = new Type[typeParameterCount];
        for (var i = 0; i < typeParameterCount; i++)
        {
            if (values[i] is not { } value)
            {
                return null;
            }

            typeArguments[i] = value.GetType();
        }

        return typeArguments;
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
        // One evaluation per test build, keyed on the build's context accessor. Builds of one instantiation can
        // overlap (for example, its test methods build in parallel), so each build keeps its own evaluation.
        // An entry is removed once every wrapper of its build has read it, or when the accessor is collected,
        // so evaluated rows are not kept after the build.
        private readonly ConditionalWeakTable<TestBuilderContextAccessor, BuildEvaluation> _evaluations = new();

        public Type[] ClassTypeArguments { get; } = classTypeArguments;

        public GenericClassRowFallback Fallback { get; } = fallback;

        /// <summary>The number of wrappers that read each build's evaluation.</summary>
        public int ConsumerCount { get; set; }

        public Task<Evaluation> GetEvaluationAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            if (dataGeneratorMetadata.TestBuilderContext is not { } buildKey)
            {
                return EvaluateAsync(dataGeneratorMetadata);
            }

            var consumerCount = ConsumerCount;
            var buildEvaluation = _evaluations.GetValue(buildKey, _ => new BuildEvaluation(consumerCount));
            var evaluation = buildEvaluation.GetOrStart(this, dataGeneratorMetadata);

            if (buildEvaluation.ReleaseConsumer())
            {
                _evaluations.Remove(buildKey);
            }

            return evaluation;
        }

        internal async Task<Evaluation> EvaluateAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            var rowsByIndex = new List<EvaluatedRow>?[dataSources.Length];
            var anyRowInfers = false;

            // The rows are read against a context of their own, so the cleanup their sources subscribe (a
            // dependency injection scope's disposal) lands on no test's context, and none of it is lost when
            // a row's first fetch returns the evaluated values without subscribing again. It runs at session
            // end, as it does for the rows reflection discovery evaluates to infer type arguments (#7205).
            var accessor = dataGeneratorMetadata.TestBuilderContext;
            var previousContext = accessor?.Current;
            TestBuilderContext? evaluationContext = null;
            if (accessor is not null && previousContext is not null)
            {
                evaluationContext = new TestBuilderContext
                {
                    TestMetadata = previousContext.TestMetadata,
                    DataSourceAttribute = previousContext.DataSourceAttribute,
                    ClassDataSourceAttribute = previousContext.ClassDataSourceAttribute,
                    InitializedAttributes = previousContext.InitializedAttributes,
                    ClassConstructor = previousContext.ClassConstructor
                };
                evaluationContext.ShareStateBagWith(previousContext);
                accessor.Current = evaluationContext;
            }

            try
            {
                foreach (var index in partitionedIndices)
                {
                    var rows = new List<EvaluatedRow>();
                    await foreach (var rowFactory in dataSources[index].GetDataRowsAsync(dataGeneratorMetadata).ConfigureAwait(false))
                    {
                        var row = await rowFactory().ConfigureAwait(false) is { } fetched ? EvaluateWrappedFactory(fetched) : null;
                        // Retain declared constructor-position and own-class null inference.
                        var infers = RowInfers(dataSources[index], row, ClassTypeArguments)
                            || (dataSources[index] is MethodDataSourceAttribute { ClassProvidingDataSource: null }
                                && MatchesOmittedOptionalConstructorRow(row, dataGeneratorMetadata.MembersToGenerate, ClassTypeArguments));
                        anyRowInfers |= infers;
                        rows.Add(new EvaluatedRow(new EvaluatedDataRow(row, rowFactory), infers));
                    }

                    rowsByIndex[index] = rows;
                }
            }
            finally
            {
                if (evaluationContext is not null)
                {
                    accessor!.Current = previousContext!;
                    TestSessionContext.DeferOnDisposeToSessionEnd(evaluationContext);
                }
            }

            return new Evaluation(rowsByIndex, anyRowInfers);
        }
    }

    // A declared reference-array row cannot tell us whether optional constructor values are present.
    // An explicit instantiation may supply their types, but every supplied value must still fit it.
    internal static bool MatchesOmittedOptionalConstructorRow(object?[]? row, IMemberMetadata[] parameters, Type[] classTypeArguments)
    {
        if (row is null)
        {
            return false;
        }

        var (values, _) = TestDataRowUnwrapper.UnwrapArray(row);
        if (values.Length == 0 || values.Length >= parameters.Length)
        {
            return false;
        }

        for (var i = values.Length; i < parameters.Length; i++)
        {
            if (parameters[i] is not ParameterMetadata { IsOptional: true })
            {
                return false;
            }
        }

        for (var i = 0; i < values.Length; i++)
        {
            if (parameters[i] is not ParameterMetadata parameter)
            {
                return false;
            }

            if (values[i] is { } value)
            {
                if (!MatchesConstructorParameterType(GetConstructorTypeInfo(parameter), value.GetType(), classTypeArguments, direction: 1))
                {
                    return false;
                }
            }
            else
            {
                var type = parameter.TypeInfo switch
                {
                    GenericParameter { IsMethodParameter: false } generic => classTypeArguments[generic.Position],
                    ConstructedGeneric constructed => constructed.GenericDefinition,
                    _ => parameter.Type
                };
                if (type.IsValueType && Nullable.GetUnderlyingType(type) is null
                    && type != typeof(Nullable<>))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Array descriptions emitted outside a generic class erase T[] to object. The existing lazy
    // constructor lookup preserves the original shape (including nested arrays inside generics).
    // Genuine object arguments also trigger recovery once; the cached description remains object.
    // Cache the recovered description per parameter, without closing arbitrary types under Native AOT.
    private static readonly ConditionalWeakTable<ParameterMetadata, TypeInfo> ConstructorTypes = new();

    private static TypeInfo GetConstructorTypeInfo(ParameterMetadata parameter)
        => (parameter.ReflectionInfoResolver is not null || parameter.ReflectionInfoFactory is not null) && HasErasedArray(parameter.TypeInfo)
            ? ConstructorTypes.GetValue(parameter, static p => DescribeConstructorType(p.ReflectionInfo.ParameterType))
            : parameter.TypeInfo;

    private static bool HasErasedArray(TypeInfo type) => type switch
    {
        ConcreteType { Type: var concrete } => concrete == typeof(object),
        ConstructedGeneric generic => generic.TypeArguments.Any(HasErasedArray),
        _ => false
    };

    private sealed record ConstructorArray(TypeInfo Element, int Rank, bool IsVector) : TypeInfo;

    private static TypeInfo DescribeConstructorType(Type type)
    {
        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            return new ConstructorArray(DescribeConstructorType(element), type.GetArrayRank(), type.Name.EndsWith("[]", StringComparison.Ordinal));
        }
        if (type.IsGenericParameter)
        {
            return new GenericParameter(type.GenericParameterPosition, type.DeclaringMethod is not null);
        }
        if (type.IsGenericType && type.ContainsGenericParameters)
        {
            return new ConstructedGeneric(type.GetGenericTypeDefinition(), type.GetGenericArguments().Select(DescribeConstructorType).ToArray());
        }
        return new ConcreteType(type);
    }

    // Compare generated type descriptions without MakeGenericType, which cannot close arbitrary types in AOT.
    // direction: 1 = expected accepts actual, -1 = actual accepts expected, 0 = invariant equality.
    private static bool MatchesConstructorParameterType(TypeInfo expected, Type actual, Type[] classTypeArguments, int direction)
    {
        if (expected is ConstructorArray array)
        {
            if (direction < 0 && !actual.IsArray)
            {
                // All arrays inherit Array's non-generic supertypes. Only vectors also
                // implement the generic collection interfaces, with their element type.
                if (actual.IsAssignableFrom(typeof(Array)))
                {
                    return true;
                }
                if (array.IsVector && actual.IsGenericType)
                {
                    var definition = actual.GetGenericTypeDefinition();
                    if (definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>)
                        || definition == typeof(IList<>) || definition == typeof(IReadOnlyCollection<>)
                        || definition == typeof(IReadOnlyList<>))
                    {
                        var argument = actual.GetGenericArguments()[0];
                        var argumentDirection = argument.IsValueType || IsValueType(array.Element) ? 0 : direction;
                        return MatchesConstructorParameterType(array.Element, argument, classTypeArguments, argumentDirection);
                    }
                }
                return false;
            }
            if (!actual.IsArray || actual.GetArrayRank() != array.Rank
                || actual.Name.EndsWith("[]", StringComparison.Ordinal) != array.IsVector)
            {
                return false;
            }
            var element = actual.GetElementType()!;
            // Array covariance also applies only to reference elements.
            var elementDirection = element.IsValueType || IsValueType(array.Element) ? 0 : direction;
            return MatchesConstructorParameterType(array.Element, element, classTypeArguments, elementDirection);
        }

        if (expected is not ConstructedGeneric constructed)
        {
            var expectedType = expected switch
            {
                GenericParameter { IsMethodParameter: false } generic => classTypeArguments[generic.Position],
                ConcreteType concrete => concrete.Type,
                _ => null
            };
            return expectedType is not null && (direction == 0 ? expectedType == actual
                : direction > 0 ? expectedType.IsAssignableFrom(actual) : actual.IsAssignableFrom(expectedType));
        }

        bool IsValueType(TypeInfo type) => type switch
        {
            GenericParameter { IsMethodParameter: false } generic => classTypeArguments[generic.Position].IsValueType,
            ConcreteType concrete => concrete.Type.IsValueType,
            ConstructedGeneric generic => generic.GenericDefinition.IsValueType,
            _ => false
        };

        // Nullable<T> boxes as T when it has a value. Only assignment may unwrap it;
        // generic argument equality must still distinguish T from Nullable<T>.
        if (direction > 0 && constructed.GenericDefinition == typeof(Nullable<>))
        {
            return MatchesConstructorParameterType(constructed.TypeArguments[0], actual, classTypeArguments, direction: 0);
        }

        if (MatchesConstructed(actual))
        {
            return true;
        }
        if (direction > 0)
        {
            for (var candidate = actual.BaseType; candidate is not null; candidate = candidate.BaseType)
            {
                if (MatchesConstructed(candidate))
                {
                    return true;
                }
            }
            foreach (var candidate in GetConstructorRowInterfaces(actual))
            {
                if (MatchesConstructed(candidate))
                {
                    return true;
                }
            }
        }
        else if (direction < 0)
        {
            // Compare the expected type's open supertypes after substituting its arguments.
            // This includes object accepting an interface, and inherited generic interfaces,
            // without constructing closed types that may be unavailable under Native AOT.
            if (actual == typeof(object))
            {
                return true;
            }
            if (constructed.GenericDefinition.BaseType is { } baseType
                && MatchesConstructorParameterType(Substitute(baseType), actual, classTypeArguments, direction))
            {
                return true;
            }
            foreach (var candidate in GetConstructorRowInterfaces(constructed.GenericDefinition))
            {
                if (MatchesConstructorParameterType(Substitute(candidate), actual, classTypeArguments, direction))
                {
                    return true;
                }
            }
        }
        return false;

        TypeInfo Substitute(Type type)
        {
            if (type.IsGenericParameter)
            {
                return constructed.TypeArguments[type.GenericParameterPosition];
            }
            if (type.IsArray)
            {
                return new ConstructorArray(Substitute(type.GetElementType()!), type.GetArrayRank(), type.Name.EndsWith("[]", StringComparison.Ordinal));
            }
            return type.IsGenericType && type.ContainsGenericParameters
                ? new ConstructedGeneric(type.GetGenericTypeDefinition(), type.GetGenericArguments().Select(Substitute).ToArray())
                : new ConcreteType(type);
        }

        bool MatchesConstructed(Type candidate)
        {
            if (!candidate.IsGenericType || candidate.GetGenericTypeDefinition() != constructed.GenericDefinition)
            {
                return false;
            }
            var actualArguments = candidate.GetGenericArguments();
            var parameters = constructed.GenericDefinition.GetGenericArguments();
            for (var i = 0; i < actualArguments.Length; i++)
            {
                var variance = parameters[i].GenericParameterAttributes & System.Reflection.GenericParameterAttributes.VarianceMask;
                var expectedArgumentIsValueType = IsValueType(constructed.TypeArguments[i]);
                // CLR variance applies only to reference-type arguments; boxing is not covariance.
                var argumentDirection = direction == 0 || variance == System.Reflection.GenericParameterAttributes.None
                    || actualArguments[i].IsValueType || expectedArgumentIsValueType ? 0
                    : variance == System.Reflection.GenericParameterAttributes.Covariant ? direction : -direction;
                if (!MatchesConstructorParameterType(constructed.TypeArguments[i], actualArguments[i], classTypeArguments, argumentDirection))
                {
                    return false;
                }
            }
            return true;
        }
    }

    private static readonly ConditionalWeakTable<Type, Type[]> ConstructorRowInterfaces = new();

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Only implemented interface identities and generic type arguments are compared for constructor assignability. No interface members are reflected or invoked; the constructor references the target interface.")]
    private static Type[] GetConstructorRowInterfaces(Type type)
        => ConstructorRowInterfaces.GetValue(type, static candidate => candidate.GetInterfaces());

    private static bool RowInfers(IDataSourceAttribute source, object?[]? row, Type[] classTypeArguments)
    {
        // A declared constructor mapping takes precedence over runtime leading-value inference.
        // Method sources next use their per-instantiation declared types, retaining own-class nulls;
        // only sources without either declaration infer directly from runtime values.
        if (DeclaredRowTypes.TryGetValue(source, out var argumentTypes))
        {
            return argumentTypes.SequenceEqual(classTypeArguments);
        }

        if (source is MethodDataSourceAttribute { InferredClassTypeArguments: { } declared } methodSource)
        {
            return row is { Length: > 0 } && declared.SequenceEqual(classTypeArguments)
                && (methodSource.ClassProvidingDataSource is null || HasNonNullValues(row));
        }

        return InfersTypeArguments(row, classTypeArguments);
    }

    /// <summary>
    /// One build's evaluation, started by the first wrapper that reads it, and the number of the build's
    /// wrappers that have not read it yet.
    /// </summary>
    internal sealed class BuildEvaluation(int consumerCount)
    {
        private readonly object _lock = new();
        private int _remainingConsumers = consumerCount;
        private Task<Evaluation>? _evaluation;

        public Task<Evaluation> GetOrStart(Coordinator coordinator, DataGeneratorMetadata dataGeneratorMetadata)
        {
            TaskCompletionSource<Evaluation> completion;

            lock (_lock)
            {
                if (_evaluation is not null)
                {
                    return _evaluation;
                }

                completion = new TaskCompletionSource<Evaluation>(TaskCreationOptions.RunContinuationsAsynchronously);
                _evaluation = completion.Task;
            }

            // Run the sources' code outside the lock
            _ = CompleteAsync(coordinator, dataGeneratorMetadata, completion);
            return completion.Task;
        }

        /// <summary>Records that a wrapper has read the evaluation, and returns whether it was the last one.</summary>
        public bool ReleaseConsumer() => Interlocked.Decrement(ref _remainingConsumers) == 0;

        private static async Task CompleteAsync(Coordinator coordinator, DataGeneratorMetadata dataGeneratorMetadata, TaskCompletionSource<Evaluation> completion)
        {
            try
            {
                completion.SetResult(await coordinator.EvaluateAsync(dataGeneratorMetadata).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }
    }

    /// <summary>
    /// A row of a partitioned source, evaluated once to read its values' types. <see cref="EvaluatedDataRow"/> holds the values
    /// evaluated from a wrapped factory (<see cref="EvaluateWrappedFactory"/>), so its first fetch does not invoke it again.
    /// </summary>
    internal readonly record struct EvaluatedRow(EvaluatedDataRow State, bool InfersTypeArguments);

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

    // Whether this source yielded every row of the inner source in a build: the AllRows fallback, or a source
    // that is not partitioned and is used because no partitioned row infers the instantiation. Recorded per
    // build, keyed on the build's context accessor, because builds of one instantiation can overlap and a
    // non-deterministic source can give them different rows. An entry is collected with its build's accessor
    private readonly ConditionalWeakTable<TestBuilderContextAccessor, StrongBox<bool>> _yieldsEveryRowByBuild = new();

    // The same, for an enumeration that has no build context accessor
    private volatile bool _yieldsEveryRowWithoutBuild;

    /// <inheritdoc />
    /// <remarks>
    /// Reads the value for an enumeration without a build context accessor. The engine reads
    /// <see cref="SkipIfEmptyFor"/> with the build's accessor instead.
    /// </remarks>
    public bool SkipIfEmpty
    {
        get => SkipIfEmptyFor(null);
        set => InnerDataSource.SkipIfEmpty = value;
    }

    /// <summary>
    /// Whether the build that enumerated this source with <paramref name="build"/> reports a skipped test when
    /// the source yielded no rows. A partition may be empty because its rows belong to other instantiations.
    /// Reflection mode creates no partition in that case, so this one reports a skipped test only when it
    /// yielded every row of the inner source in that build, as reflection's fallback to every class row does.
    /// </summary>
    internal bool SkipIfEmptyFor(TestBuilderContextAccessor? build)
    {
        var yieldsEveryRow = build is null
            ? _yieldsEveryRowWithoutBuild
            : _yieldsEveryRowByBuild.TryGetValue(build, out var recorded) && recorded.Value;

        return yieldsEveryRow && InnerDataSource.SkipIfEmpty;
    }

    private void RecordYieldsEveryRow(TestBuilderContextAccessor? build, bool yieldsEveryRow)
    {
        if (build is null)
        {
            _yieldsEveryRowWithoutBuild = yieldsEveryRow;
        }
        else
        {
            _yieldsEveryRowByBuild.GetValue(build, static _ => new StrongBox<bool>()).Value = yieldsEveryRow;
        }
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
        RecordYieldsEveryRow(
            dataGeneratorMetadata.TestBuilderContext,
            !evaluation.AnyRowInfers && _coordinator.Fallback == GenericClassRowFallback.AllRows);

        if (!evaluation.AnyRowInfers && _coordinator.Fallback == GenericClassRowFallback.NoClassRow)
        {
            if (_index == 0)
            {
                // No row supplies the constructor arguments. The first source yields one empty row for all
                // of them, and the instance factory fails for it once per method row, as in reflection mode.
                // Every source is wrapped under this fallback, so the source at index 0 is always this wrapper
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
                yield return evaluatedRow.State.FetchAsync;
            }
        }
    }
}

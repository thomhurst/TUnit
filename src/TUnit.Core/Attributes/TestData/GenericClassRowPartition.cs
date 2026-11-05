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
                            || (dataSources[index] is MethodDataSourceAttribute { ClassProvidingDataSource: null, InferredClassTypeArguments: null }
                                && MatchesConstructorRow(row, dataGeneratorMetadata.MembersToGenerate, ClassTypeArguments));
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

    // A declared reference-array row cannot tell us whether constructor values infer every type argument.
    // An explicit instantiation may supply their types, but every supplied value must still fit it.
    internal static bool MatchesConstructorRow(object?[]? row, IMemberMetadata[] parameters, Type[] classTypeArguments)
    {
        if (row is null)
        {
            return false;
        }

        var (values, _) = TestDataRowUnwrapper.UnwrapArray(row);
        var trailing = parameters.LastOrDefault() is ParameterMetadata last
            ? GetTrailingConstructorElement(last, classTypeArguments) : null;
        var regularCount = parameters.Length - (trailing is null ? 0 : 1);
        if (trailing is null && values.Length > parameters.Length)
        {
            return false;
        }

        for (var i = values.Length; i < regularCount; i++)
        {
            if (parameters[i] is not ParameterMetadata { IsOptional: true })
            {
                return false;
            }
        }

        for (var i = 0; i < values.Length; i++)
        {
            if (i >= regularCount && trailing is not null)
            {
                // A single supplied collection stays intact; loose values bind to its elements.
                if (values.Length == parameters.Length
                    && MatchesConstructorValue(GetConstructorTypeInfo((ParameterMetadata)parameters[regularCount]), values[i], classTypeArguments))
                {
                    continue;
                }
                if (values.Length == parameters.Length && values[i] is Array { Rank: 1 } suppliedArray
                    && MatchesTrailingArrayElements(suppliedArray, trailing, classTypeArguments))
                {
                    continue;
                }
                if (!MatchesConstructorValue(trailing, values[i], classTypeArguments))
                {
                    return false;
                }
            }
            else if (parameters[i] is not ParameterMetadata parameter
                || !MatchesConstructorValue(GetConstructorTypeInfo(parameter), values[i], classTypeArguments))
            {
                return false;
            }
        }

        return true;
    }

    private static TypeInfo? GetTrailingConstructorElement(ParameterMetadata parameter, Type[] classTypeArguments)
    {
        var description = GetConstructorTypeInfo(parameter);
        if (description is GenericParameter { IsMethodParameter: false } genericParameter)
        {
            description = new ConcreteType(classTypeArguments[genericParameter.Position]);
        }
        return description switch
        {
            ConstructorArray { Rank: 1 } array => array.Element,
            ConcreteType { Type: { IsArray: true } array } when array.GetArrayRank() == 1
                => new ConcreteType(array.GetElementType()!),
            ConstructedGeneric generic when IsCollectingInterface(generic.GenericDefinition) && parameter.IsParams
                => generic.TypeArguments[0],
            ConcreteType { Type: var type } when type.IsGenericType && IsCollectingInterface(type.GetGenericTypeDefinition()) && parameter.IsParams
                => new ConcreteType(type.GetGenericArguments()[0]),
            _ => null
        };
    }

    private static bool IsCollectingInterface(Type type) => type == typeof(IEnumerable<>)
        || type == typeof(ICollection<>) || type == typeof(IList<>)
        || type == typeof(IReadOnlyCollection<>) || type == typeof(IReadOnlyList<>);

    private static bool MatchesConstructorValue(TypeInfo description, object? value, Type[] classTypeArguments)
    {
        if (value is not null)
        {
            return MatchesConstructorParameterType(description, value.GetType(), classTypeArguments, direction: 1)
                || MayConvertConstructorValue(description, value, classTypeArguments);
        }

        // Both generated CastHelper and reflection constructor binding accept null for
        // value types as their default value; leave that conversion to the binder.
        return true;
    }

    private static bool MatchesTrailingArrayElements(Array values, TypeInfo element, Type[] classTypeArguments)
    {
        // Like CastHelper.ToTrailingArray, support rank-one arrays with any lower bound.
        var lowerBound = values.GetLowerBound(0);
        for (var i = 0; i < values.Length; i++)
        {
            if (!MatchesConstructorValue(element, values.GetValue(lowerBound + i), classTypeArguments))
            {
                return false;
            }
        }
        return true;
    }

    // TODO(#7615): consolidate structural matching with the binder and its parity coverage.
    // Conversion belongs to the binder. Do not execute user converters while partitioning;
    // retain candidates for registered conversions and built-in scalar CastHelper paths.
    private static bool MayConvertConstructorValue(TypeInfo description, object value, Type[] classTypeArguments)
    {
        // CastHelper checks conversions against the non-nullable target. Normalize the
        // description too, because the underlying type can remain structurally generic.
        if (description is ConstructedGeneric { GenericDefinition: var definition, TypeArguments: [var underlying] }
            && definition == typeof(Nullable<>))
        {
            description = underlying;
        }
        var target = description switch
        {
            GenericParameter { IsMethodParameter: false } generic => classTypeArguments[generic.Position],
            ConcreteType concrete => concrete.Type,
            _ => null
        };

        // CastHelper's non-AOT array fallback converts vector elements (or a single
        // IConvertible value). Inspect candidates without invoking user conversions.
        var arrayElement = description switch
        {
            ConstructorArray { Rank: 1, IsVector: true } array => array.Element,
            _ when target is { IsArray: true } && target.GetArrayRank() == 1
                && target.Name.EndsWith("[]", StringComparison.Ordinal) => new ConcreteType(target.GetElementType()!),
            _ => null
        };
        if (arrayElement is not null)
        {
            if (value is Array { Rank: 1 } array && array.GetLowerBound(0) == 0
                && MatchesTrailingArrayElements(array, arrayElement, classTypeArguments))
            {
                return true;
            }
            if (value is IConvertible && MatchesConstructorValue(arrayElement, value, classTypeArguments))
            {
                return true;
            }
        }

        // CastHelper unwraps the first item of non-array enumerables for scalar targets.
        // Keep the candidate without enumerating: user data may be lazy or have side effects.
        var enumerableTarget = target ?? (description as ConstructedGeneric)?.GenericDefinition;
        if (value is System.Collections.IEnumerable and not string && !value.GetType().IsArray
            && enumerableTarget is not null
            && !typeof(System.Collections.IEnumerable).IsAssignableFrom(enumerableTarget))
        {
            return true;
        }

        if (target is null)
        {
            var sourceType = value.GetType();
            if (value is IConvertible && !sourceType.IsPrimitive && !sourceType.IsEnum
                && sourceType != typeof(string) && sourceType != typeof(decimal) && sourceType != typeof(DateTime))
            {
                return true; // Custom ToType belongs to the binder, including structural targets.
            }
#if NET
            // An open self-parsing interface preserves its identity after substitution.
            // Inspect the definition without constructing the closed target or calling Parse.
            if (value is string && description is ConstructedGeneric constructed
                // Native AOT can omit interfaces on open definitions even when the closed
                // type supports parsing. Retain uncertain strings there for the binder.
                && (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
                    || GetConstructorRowInterfaces(constructed.GenericDefinition).Any(candidate =>
                    candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IParsable<>)
                    && candidate.GenericTypeArguments[0].IsGenericType
                    && candidate.GenericTypeArguments[0].GetGenericTypeDefinition() == constructed.GenericDefinition
                    && candidate.GenericTypeArguments[0].GetGenericArguments().SequenceEqual(constructed.GenericDefinition.GetGenericArguments()))))
            {
                return true;
            }
#endif
            if (description is ConstructedGeneric structural
                && Helpers.CastHelper.HasStructuralReflectionConversion(value.GetType(), structural.GenericDefinition,
                    (description, classTypeArguments, valueType: value.GetType()), static (candidate, state) => MatchesConstructorParameterType(
                        state.description, candidate, state.classTypeArguments, direction: 0),
                    static (result, input, state) => MatchesStructuralOperator(
                        (ConstructedGeneric)state.description, result, input, valueType: state.valueType, state.classTypeArguments)))
            {
                return true;
            }
            return Converters.AotConverterRegistry.HasConverter(value.GetType(), (description, classTypeArguments),
                static (registeredTarget, state) => MatchesConstructorParameterType(
                    state.description, registeredTarget, state.classTypeArguments, direction: 0));
        }

        target = Nullable.GetUnderlyingType(target) ?? target;
        if (Converters.AotConverterRegistry.TryGetConverter(value.GetType(), target, out _))
        {
            return true;
        }

        if (Helpers.CastHelper.HasReflectionConversion(value.GetType(), target))
        {
            return true;
        }

        // Parsing belongs to the binder, especially for user-defined IParsable implementations.
        // Compare cached interface identities only; do not invoke Parse or close generic types here.
        if (value is string && (target == typeof(Guid) || target == typeof(DateTime)
            || target == typeof(DateTimeOffset) || target == typeof(TimeSpan)
#if NET
            || (!target.IsPrimitive && target != typeof(decimal)
                // Closed custom types can also lose parsing interfaces under Native AOT.
                && ((!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
                        && Type.GetTypeCode(target) == TypeCode.Object)
                    || GetConstructorRowInterfaces(target).Any(candidate => candidate.IsGenericType
                        && candidate.GetGenericTypeDefinition() == typeof(IParsable<>)
                        && candidate.GenericTypeArguments[0] == target)))
#endif
            ))
        {
            return true;
        }

        // CastHelper's non-primitive fallback accepts IConvertible string targets too.
        // Keep candidates without invoking a potentially user-defined conversion here.
        if (target == typeof(string) && value is IConvertible)
        {
            return true;
        }

        if (target.IsEnum)
        {
            try
            {
                _ = Enum.ToObject(target, value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        var source = value.GetType();
        if ((source.IsPrimitive || source.IsEnum || source == typeof(string) || source == typeof(decimal))
            && (target.IsPrimitive || target == typeof(decimal)))
        {
            try
            {
                _ = Convert.ChangeType(value, target);
                return true;
            }
            catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
            {
                return false;
            }
        }

        // Custom IConvertible implementations are user code: only the binder may
        // call them. Built-in conversions above still reject known invalid values.
        // The reflection fallback also calls ChangeType for non-primitive targets,
        // including DateTime and custom ToType implementations. Preserve uncertain rows.
        if (value is not IConvertible)
        {
            return false;
        }

        // Framework scalar implementations cannot define custom ToType behavior.
        // Probe only these known values; custom IConvertible stays deferred to the binder.
        if (source.IsPrimitive || source.IsEnum || source == typeof(string)
            || source == typeof(decimal) || source == typeof(DateTime))
        {
            try
            {
                _ = Convert.ChangeType(value, target);
                return true;
            }
            catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
            {
                return false;
            }
        }
        return true;
    }

    private static bool MatchesStructuralOperator(ConstructedGeneric target, Type result, Type input,
        Type valueType, Type[] classTypeArguments)
    {
        return SameType(Substitute(result), target)
            && MatchesConstructorParameterType(Substitute(input), valueType, classTypeArguments, direction: 1);

        TypeInfo Substitute(Type type)
        {
            if (type.IsGenericParameter) return target.TypeArguments[type.GenericParameterPosition];
            if (type.IsArray)
                return new ConstructorArray(Substitute(type.GetElementType()!), type.GetArrayRank(), type.Name.EndsWith("[]", StringComparison.Ordinal));
            return type.IsGenericType && type.ContainsGenericParameters
                ? new ConstructedGeneric(type.GetGenericTypeDefinition(), type.GetGenericArguments().Select(Substitute).ToArray())
                : new ConcreteType(type);
        }

        bool SameType(TypeInfo left, TypeInfo right)
        {
            if (left is GenericParameter generic) left = new ConcreteType(classTypeArguments[generic.Position]);
            if (right is GenericParameter other) right = new ConcreteType(classTypeArguments[other.Position]);
            if (left is ConcreteType concrete)
                return MatchesConstructorParameterType(right, concrete.Type, classTypeArguments, direction: 0);
            if (right is ConcreteType closed)
                return MatchesConstructorParameterType(left, closed.Type, classTypeArguments, direction: 0);
            if (left is ConstructorArray leftArray && right is ConstructorArray rightArray)
                return leftArray.Rank == rightArray.Rank && leftArray.IsVector == rightArray.IsVector
                    && SameType(leftArray.Element, rightArray.Element);
            return left is ConstructedGeneric first && right is ConstructedGeneric second
                && first.GenericDefinition == second.GenericDefinition
                && first.TypeArguments.Length == second.TypeArguments.Length
                && first.TypeArguments.Zip(second.TypeArguments, SameType).All(equal => equal);
        }
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

    // Planned metadata replacement and matcher consolidation: https://github.com/thomhurst/TUnit/issues/7615
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
            // CLR vectors also implement invariant collection interfaces covariantly
            // for reference elements (for example string[] is an IList<object>).
            if (actual.IsArray && actual.Name.EndsWith("[]", StringComparison.Ordinal)
                && IsCollectingInterface(constructed.GenericDefinition))
            {
                var element = actual.GetElementType()!;
                var elementDirection = element.IsValueType || IsValueType(constructed.TypeArguments[0]) ? 0 : direction;
                if (MatchesConstructorParameterType(constructed.TypeArguments[0], element, classTypeArguments, elementDirection))
                {
                    return true;
                }
            }
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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Provides test data from a method, property, or field on the specified type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type containing the data source member.</typeparam>
/// <param name="methodNameProvidingDataSource">The name of the method, property, or field that provides the test data.</param>
/// <example>
/// <code>
/// [Test]
/// [MethodDataSource&lt;TestDataProvider&gt;(nameof(TestDataProvider.GetTestCases))]
/// public void MyTest(int input, string expected) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public class MethodDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
T>(string methodNameProvidingDataSource)
    : MethodDataSourceAttribute(typeof(T), methodNameProvidingDataSource);

/// <summary>
/// Provides test data from a method, property, or field in the test class or a specified type.
/// </summary>
/// <remarks>
/// <para>
/// The data source can be a method, property, or field that returns test data.
/// Supported return types include single values, <see cref="IEnumerable{T}"/>, <see cref="IAsyncEnumerable{T}"/>,
/// <see cref="Task{T}"/>, tuples, and arrays.
/// </para>
/// <para>
/// When no class type is specified, the data source is looked up in the test class itself.
/// Both static and instance members are supported.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using a method in the same class
/// [Test]
/// [MethodDataSource(nameof(GetTestData))]
/// public void MyTest(int value, string name) { }
///
/// public static IEnumerable&lt;(int, string)&gt; GetTestData()
/// {
///     yield return (1, "one");
///     yield return (2, "two");
/// }
///
/// // Using a method in another class
/// [Test]
/// [MethodDataSource(typeof(SharedData), nameof(SharedData.GetValues))]
/// public void MyTest(string value) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public class MethodDataSourceAttribute : Attribute, IDataSourceAttribute
{
    internal const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public
        | System.Reflection.BindingFlags.NonPublic
        | System.Reflection.BindingFlags.Static
        | System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.FlattenHierarchy;

    /// <summary>
    /// Gets the type containing the data source member, or <c>null</c> if the data source is in the test class itself.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public Type? ClassProvidingDataSource { get; }

    /// <summary>
    /// Gets the name of the method, property, or field that provides the test data.
    /// </summary>
    public string MethodNameProvidingDataSource { get; }

    /// <summary>
    /// Gets or sets an AOT-safe factory function that provides test data without reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is <strong>automatically populated by the TUnit source generator</strong> at compile time
    /// and is not intended to be set manually in attribute syntax. Because C# attributes only support
    /// literal values as arguments, a <see cref="Func{T,TResult}"/> cannot be assigned in an attribute
    /// declaration.
    /// </para>
    /// <para>
    /// When the source generator processes a <c>[MethodDataSource]</c> attribute, it emits code that
    /// sets this property to a generated delegate that invokes the named method directly — bypassing
    /// reflection and enabling compatibility with Native AOT and IL trimming.
    /// </para>
    /// </remarks>
    public Func<DataGeneratorMetadata, IAsyncEnumerable<Func<Task<object?[]?>>>>? Factory { get; set; }

    /// <summary>
    /// Gets or sets the arguments to pass to the data source method.
    /// Use this when the data source method requires parameters.
    /// </summary>
    public object?[] Arguments { get; set; } = [];

    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    /// <inheritdoc />
    public bool DeferEnumeration { get; set; }

    public MethodDataSourceAttribute(string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }

    public MethodDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type classProvidingDataSource,
        string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        ClassProvidingDataSource = classProvidingDataSource ?? throw new ArgumentNullException(nameof(classProvidingDataSource), "No class type was provided");
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }

    /// <summary>
    /// Creates an <see cref="InstanceMethodDataSourceAttribute"/> copy of this attribute, preserving all
    /// user-settable state. Used by reflection-mode discovery to mirror the conversion the source generator
    /// performs at compile time for data sources that target instance members.
    /// <see cref="Factory"/> is intentionally not copied: it is only populated by the source generator,
    /// so it is always null on attributes discovered via reflection.
    /// </summary>
    internal InstanceMethodDataSourceAttribute ToInstanceVariant()
    {
        var converted = ClassProvidingDataSource is { } classProvidingDataSource
            ? new InstanceMethodDataSourceAttribute(classProvidingDataSource, MethodNameProvidingDataSource)
            : new InstanceMethodDataSourceAttribute(MethodNameProvidingDataSource);

        converted.Arguments = Arguments;
        converted.SkipIfEmpty = SkipIfEmpty;
        converted.DeferEnumeration = DeferEnumeration;

        return converted;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (Factory != null)
        {
            var factoryYieldedAny = false;
            await foreach (var func in Factory(dataGeneratorMetadata))
            {
                factoryYieldedAny = true;
                yield return func;
            }

            // An empty source runs the test once with no arguments, like NoDataSource (#2862), unless
            // SkipIfEmpty is set. Same rule as the reflection path below, so both modes agree (#7092).
            if (!factoryYieldedAny && !SkipIfEmpty)
            {
                yield return static () => Task.FromResult<object?[]?>([]);
            }

            yield break;
        }

        // A parameterless test method has no members to generate. That is valid, so don't throw:
        // the source-generated Factory above never needs them either.
        var membersToGenerate = dataGeneratorMetadata.MembersToGenerate;
        var item1 = membersToGenerate.Length > 0 ? membersToGenerate[0] : null;

        var targetType = ClassProvidingDataSource
            ?? (item1 as PropertyMetadata)?.ClassMetadata.Type
            ?? TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);

        // If we have a test class instance and no explicit class was provided,
        // use the instance's actual type (which will be the constructed generic type)
        // Skip PlaceholderInstance as it's used during discovery when the actual instance isn't created yet
        if (ClassProvidingDataSource == null
            && dataGeneratorMetadata.TestClassInstance != null
            && dataGeneratorMetadata.TestClassInstance is not PlaceholderInstance)
        {
            targetType = dataGeneratorMetadata.TestClassInstance.GetType();
        }

        // If the target type is abstract or interface, we can't create an instance of it.
        // Fall back to the test class type which should be concrete.
        // BUT: Don't override if ClassProvidingDataSource was explicitly provided, even if it's a static class
        // (static classes are abstract in IL but contain static members we can invoke)
        if (ClassProvidingDataSource == null && targetType != null && (targetType.IsAbstract || targetType.IsInterface))
        {
            var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
            if (testClassType != null && !testClassType.IsAbstract && !testClassType.IsInterface)
            {
                targetType = testClassType;
            }
        }
        
        if (targetType == null)
        {
            throw new InvalidOperationException($"Could not determine target type for method '{MethodNameProvidingDataSource}'. This may occur during static property initialization without a test context.");
        }

        // Try to find a method first.
        var methodInfo = ResolveDataSourceMethod(targetType)
            ?? targetType.GetMethod(MethodNameProvidingDataSource, BindingFlags);

        object? methodResult;

        if (methodInfo != null)
        {
            // Determine if it's an instance method
            object? instance = null;
            if (!methodInfo.IsStatic)
            {
                instance = await GetOrCreateInstanceAsync(dataGeneratorMetadata, targetType);
            }

            methodResult = methodInfo.Invoke(instance, BuildInvokeArgs(methodInfo, Arguments));
        }
        else
        {
            // Try to find a property or field
            var propertyInfo = targetType.GetProperty(MethodNameProvidingDataSource, BindingFlags);
            var fieldInfo = targetType.GetField(MethodNameProvidingDataSource, BindingFlags);

            if (propertyInfo != null)
            {
                // Determine if it's an instance property
                object? instance = null;
                if (propertyInfo.GetMethod?.IsStatic != true)
                {
                    instance = await GetOrCreateInstanceAsync(dataGeneratorMetadata, targetType);
                }

                methodResult = propertyInfo.GetValue(instance);
            }
            else if (fieldInfo != null)
            {
                // Determine if it's an instance field
                object? instance = null;
                if (!fieldInfo.IsStatic)
                {
                    instance = await GetOrCreateInstanceAsync(dataGeneratorMetadata, targetType);
                }

                methodResult = fieldInfo.GetValue(instance);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Method, property, or field '{MethodNameProvidingDataSource}' not found in class '{targetType.Name}' with the specified arguments.");
            }
        }

        // Handle different return types
        if (methodResult == null)
        {
            yield break;
        }

        var hasAnyItems = false;
        await foreach (var row in GetRowsFromRuntimeValueAsync(methodResult, dataGeneratorMetadata))
        {
            hasAnyItems = true;
            yield return row;
        }

        // If the source was empty, yield one empty result like NoDataSource does
        // unless SkipIfEmpty is true, in which case we yield nothing (test will be skipped)
        if (!hasAnyItems && !SkipIfEmpty)
        {
            yield return () => Task.FromResult<object?[]?>([]);
        }
    }

    /// <summary>
    /// Turns a data source's value into rows by its runtime type: an <see cref="IAsyncEnumerable{T}"/>, a
    /// <see cref="Task{TResult}"/> or <see cref="ValueTask{TResult}"/> (awaited, then treated like its result), an
    /// <see cref="System.Collections.IEnumerable"/> other than a string or tuple (one row per item), or a single row.
    /// Used by the reflection fallback above and by the source-generated factory for a field, or a private or protected
    /// property, whose declared type does not say which of these it holds, so both read such a member the same way.
    /// An empty source yields nothing; the caller decides whether that becomes an empty row.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    internal static async IAsyncEnumerable<Func<Task<object?[]?>>> GetRowsFromRuntimeValueAsync(object methodResult, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // ValueTask<T> is neither a Task nor an IEnumerable, so normalise it to Task<T> and let
        // the Task branch below await it, as the source-generated Factory does.
        if (TryConvertValueTaskToTask(methodResult) is { } valueTaskAsTask)
        {
            methodResult = valueTaskAsTask;
        }

        // Compute paramTypes once to avoid repeated LINQ allocations
        // Use MembersToGenerate which contains the correct parameters for this data generator type
        // (constructor params for class-level, method params for method-level)
        var paramTypes = GetMemberTypes(dataGeneratorMetadata.MembersToGenerate);

        // If it's IAsyncEnumerable, handle it specially
        if (IsAsyncEnumerable(methodResult.GetType()))
        {
            await foreach (var item in ConvertToAsyncEnumerable(methodResult))
            {
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
                };
            }
        }
        // If it's Task<IEnumerable>
        else if (methodResult is Task task)
        {
            await task.ConfigureAwait(false);
            var taskResult = GetTaskResult(task);

            if (taskResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(taskResult))
            {
                foreach (var item in enumerable)
                {
                    yield return async () =>
                    {
                        return await Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
                    };
                }
            }
            else
            {
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(taskResult.ToObjectArrayWithTypes(paramTypes));
                };
            }
        }
        // Regular IEnumerable - but check if it's a tuple first
        // Tuples implement IEnumerable but should be treated as single values
        else if (methodResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(methodResult))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(item.ToObjectArrayWithTypes(paramTypes));
            }
        }
        else
        {
            yield return async () =>
            {
                return await Task.FromResult<object?[]?>(methodResult.ToObjectArrayWithTypes(paramTypes));
            };
        }
    }

    /// <summary>
    /// The declared type of one row of a data source member of type <paramref name="memberType"/>, read the way
    /// <see cref="GetRowsFromRuntimeValueAsync"/> reads its value: a <c>Task</c> or <c>ValueTask</c> is unwrapped; an
    /// <c>IAsyncEnumerable&lt;X&gt;</c>, an array of <c>X</c> or an <c>IEnumerable&lt;X&gt;</c> other than a string
    /// yields rows of <c>X</c>; any other value, such as a string or a tuple, is a single row. A <c>Func&lt;X&gt;</c>
    /// row is unwrapped. Null for <c>object[]</c> rows and non-generic sequences, whose element types are unknown.
    /// Reflection discovery uses it to infer a generic class's type arguments from a member it cannot read yet
    /// (#7208). Keep it in step with <see cref="GetRowsFromRuntimeValueAsync"/>; MethodDataSourceDeclaredRowTypeTests
    /// checks the two against each other.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection mode requires dynamic access to data source members")]
    internal static Type? GetDeclaredRowType(Type memberType)
    {
        var awaited = false;
        if (memberType.IsGenericType
            && (memberType.GetGenericTypeDefinition() == typeof(Task<>) || memberType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            memberType = memberType.GetGenericArguments()[0];
            awaited = true;
        }

        Type? rowType;
        if (memberType == typeof(string))
        {
            rowType = memberType;
        }
        else if (!awaited && GetSequenceElementType(memberType, typeof(IAsyncEnumerable<>)) is { } asyncElementType)
        {
            // An awaited IAsyncEnumerable is not enumerated by invocation, so only a direct one yields rows
            rowType = asyncElementType;
        }
        else if (memberType.IsArray)
        {
            rowType = memberType.GetElementType();
        }
        else if (GetSequenceElementType(memberType, typeof(IEnumerable<>)) is { } elementType)
        {
            rowType = elementType;
        }
        else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(memberType))
        {
            return null;
        }
        else
        {
            rowType = memberType;
        }

        if (rowType is { IsGenericType: true } && rowType.GetGenericTypeDefinition() == typeof(Func<>))
        {
            rowType = rowType.GetGenericArguments()[0];
        }

        return rowType == null || rowType == typeof(object[]) ? null : rowType;
    }

    /// <summary>
    /// <c>X</c> when <paramref name="type"/> is or implements <paramref name="sequenceDefinition"/> of <c>X</c>
    /// (<c>IEnumerable&lt;&gt;</c> or <c>IAsyncEnumerable&lt;&gt;</c>), otherwise null.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection mode requires dynamic access to data source members")]
    private static Type? GetSequenceElementType(Type type, Type sequenceDefinition)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == sequenceDefinition)
        {
            return type.GetGenericArguments()[0];
        }

        foreach (var implemented in type.GetInterfaces())
        {
            if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == sequenceDefinition)
            {
                return implemented.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// The declared type of the member this source reads on <paramref name="targetType"/>: the return type of the
    /// method overload <see cref="GetDataRowsAsync"/> invokes, else the property or field type. Null when no such
    /// member exists. Reflection discovery uses it to infer a generic class's type arguments before reading the
    /// member, so it resolves the member exactly as invocation does.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    internal Type? GetDeclaredMemberType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type targetType)
    {
        var methodInfo = ResolveDataSourceMethod(targetType)
            ?? targetType.GetMethod(MethodNameProvidingDataSource, BindingFlags);

        return methodInfo?.ReturnType
            ?? targetType.GetProperty(MethodNameProvidingDataSource, BindingFlags)?.PropertyType
            ?? targetType.GetField(MethodNameProvidingDataSource, BindingFlags)?.FieldType;
    }

    // Resolves the data-source method overload that best matches the supplied Arguments.
    //
    // When every argument is non-null we know its runtime type, so we delegate to the runtime
    // binder via Type.GetMethod(name, flags, binder, types, modifiers). The binder applies normal
    // overload-resolution rules (most-specific match wins), which both honours the derived-type /
    // interface widening this fix intends AND disambiguates competing overloads such as
    // GetData(object) vs GetData(string) — where a plain IsAssignableFrom scan would match both and
    // throw on SingleOrDefault.
    //
    // When any argument is null it has no runtime type, so it cannot be expressed in the binder's
    // Type[]. The overloads are then scored the way the source generator scores them (ScoreArguments):
    // a null needs a reference or Nullable<T> parameter, other arguments may use numeric widening,
    // missing trailing parameters must be optional or a CancellationToken, and exact-count overloads and
    // exact argument types rank higher. A single best overload is used. Otherwise we fall back to the
    // caller's name-only lookup.
    //
    // With zero arguments the empty Type[] flows through the same binder path and selects the
    // parameterless overload directly — including when the name is shared with other-arity
    // overloads, which the caller's name-only GetMethod(name, flags) would reject as ambiguous
    // with an AmbiguousMatchException.
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    private MethodInfo? ResolveDataSourceMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type targetType)
    {
        var arguments = Arguments;

        // For zero arguments this produces an empty Type[], which the binder GetMethod overload
        // resolves to the parameterless overload — even when other-arity overloads share the name.
        // (The plain name-only GetMethod(name, flags) cannot: it returns null on an ambiguous name.)
        var argumentTypes = new Type[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            var argumentType = arguments[i]?.GetType();
            if (argumentType is null)
            {
                // A null argument has no runtime type the binder can match on.
                return ResolveDataSourceMethodWithNullArgument(targetType, arguments);
            }

            argumentTypes[i] = argumentType;
        }

        // Let the runtime binder pick the best overload for these exact argument types.
        // Ambiguous matches surface as AmbiguousMatchException (genuinely ambiguous code),
        // never as the spurious SingleOrDefault throw the old scan produced.
        // Generic method definitions are skipped: the Arguments cannot supply their type arguments, so
        // IEnumerable<int> Rows() is used rather than an ambiguous match with IEnumerable<T> Rows<T>(),
        // as the source generator does.
#if NET
        return targetType.GetMethod(MethodNameProvidingDataSource, genericParameterCount: 0, BindingFlags, binder: null, argumentTypes, modifiers: null);
#else
        List<MethodBase>? candidates = null;
        foreach (var method in targetType.GetMethods(BindingFlags))
        {
            if (method.Name == MethodNameProvidingDataSource && !method.IsGenericMethodDefinition)
            {
                (candidates ??= []).Add(method);
            }
        }

        return candidates is null
            ? null
            : (MethodInfo?)Type.DefaultBinder.SelectMethod(BindingFlags, candidates.ToArray(), argumentTypes, modifiers: null);
#endif
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Method data sources require runtime discovery. AOT users should use Factory property.")]
    private MethodInfo? ResolveDataSourceMethodWithNullArgument([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type targetType, object?[] arguments)
    {
        var bestScore = -1;
        var best = new List<MethodInfo>();
        foreach (var method in targetType.GetMethods(BindingFlags))
        {
            if (method.Name != MethodNameProvidingDataSource || method.IsGenericMethodDefinition)
            {
                continue;
            }

            var score = ScoreArguments(method.GetParameters(), arguments);
            if (score < 0 || score < bestScore)
            {
                continue;
            }

            if (score > bestScore)
            {
                best.Clear();
                bestScore = score;
            }

            best.Add(method);
        }

        // On a tie, the overload with the more specific parameter types wins, as with the binder and the source
        // generator: Rows(string?, long) beats Rows(string?, object) for { null, 20 }.
        foreach (var candidate in best)
        {
            var mostSpecific = true;
            foreach (var other in best)
            {
                if (!ReferenceEquals(candidate, other) && !IsMoreSpecific(candidate.GetParameters(), other.GetParameters(), arguments.Length))
                {
                    mostSpecific = false;
                    break;
                }
            }

            if (mostSpecific)
            {
                return candidate;
            }
        }

        // Still ambiguous, or nothing accepts the arguments: the caller's name-only lookup reports it
        return null;
    }

    private static bool IsMoreSpecific(ParameterInfo[] candidate, ParameterInfo[] other, int argumentCount)
    {
        // Only called for overloads that ScoreArguments accepted, and it rejects any overload with fewer
        // parameters than arguments, so both have at least argumentCount parameters. Check it anyway, as the
        // generator's IsMoreSpecificDataSourceOverload does, so a broken invariant cannot index past the end.
        if (argumentCount > candidate.Length || argumentCount > other.Length)
        {
            return false;
        }

        var anyDifferent = false;
        for (var i = 0; i < argumentCount; i++)
        {
            var candidateType = candidate[i].ParameterType;
            var otherType = other[i].ParameterType;
            if (candidateType == otherType)
            {
                continue;
            }

            var otherUnderlying = Nullable.GetUnderlyingType(otherType) ?? otherType;
            if (!otherType.IsAssignableFrom(candidateType)
                && candidateType != otherUnderlying
                && !IsNumericWidening(candidateType, otherUnderlying))
            {
                return false;
            }

            anyDifferent = true;
        }

        return anyDifferent;
    }

    /// <summary>
    /// Scores how well a method accepts <paramref name="arguments"/>, as the source generator's
    /// <c>ScoreDataSourceOverload</c> does: -1 when it cannot be called with them, otherwise higher for exact
    /// argument types. An overload with one parameter per argument outranks one that needs missing optional or
    /// <see cref="CancellationToken"/> parameters filled in by <see cref="BuildInvokeArgs"/>.
    /// </summary>
    private static int ScoreArguments(ParameterInfo[] parameters, object?[] arguments)
    {
        if (arguments.Length > parameters.Length)
        {
            return -1;
        }

        // Each argument adds at most 2, so this base puts every exact-count overload above every other one
        var score = arguments.Length == parameters.Length ? 2 * arguments.Length + 1 : 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (i >= arguments.Length)
            {
                if (!parameters[i].HasDefaultValue && parameterType != typeof(CancellationToken))
                {
                    return -1;
                }

                continue;
            }

            if (arguments[i] is not { } argument)
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                {
                    return -1;
                }

                continue;
            }

            var argumentType = argument.GetType();
            var underlyingParameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
            if (argumentType == parameterType)
            {
                score += 2;
            }
            else if (parameterType.IsInstanceOfType(argument)
                     || argumentType == underlyingParameterType
                     || IsNumericWidening(argumentType, parameterType))
            {
                score += 1;
            }
            else
            {
                return -1;
            }
        }

        return score;
    }

    // The implicit numeric conversions C# allows, except to decimal, which MethodInfo.Invoke cannot apply.
    // Invoke cannot widen into Nullable<T> either (an int argument for a long? parameter throws), so callers
    // pass the parameter type itself, not its underlying type.
    private static bool IsNumericWidening(Type from, Type to)
    {
        var target = Type.GetTypeCode(to);
        return Type.GetTypeCode(from) switch
        {
            TypeCode.SByte => target is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double,
            TypeCode.Byte => target is TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double,
            TypeCode.Int16 => target is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double,
            TypeCode.UInt16 => target is TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double,
            TypeCode.Char => target is TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double,
            TypeCode.Int32 => target is TypeCode.Int64 or TypeCode.Single or TypeCode.Double,
            TypeCode.UInt32 => target is TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double,
            TypeCode.Int64 or TypeCode.UInt64 => target is TypeCode.Single or TypeCode.Double,
            TypeCode.Single => target is TypeCode.Double,
            _ => false
        } && !to.IsEnum && !from.IsEnum;
    }

    // MethodInfo.Invoke does not auto-fill optional parameters the way a C# call site does.
    private static object?[] BuildInvokeArgs(MethodInfo methodInfo, object?[] suppliedArguments)
    {
        var parameters = methodInfo.GetParameters();
        if (parameters.Length <= suppliedArguments.Length)
        {
            // Exact match passes through; surplus supplied args are left to Invoke to surface as a mismatch.
            return suppliedArguments;
        }

        var args = new object?[parameters.Length];
        Array.Copy(suppliedArguments, args, suppliedArguments.Length);
        for (var i = suppliedArguments.Length; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.HasDefaultValue)
            {
                args[i] = Type.Missing;
            }
            else if (p.ParameterType == typeof(CancellationToken))
            {
                args[i] = CancellationToken.None;
            }
            else
            {
                // Required param missing — fall back so Invoke surfaces the original mismatch.
                return suppliedArguments;
            }
        }
        return args;
    }

    private static Type[]? GetMemberTypes(IMemberMetadata[]? members)
    {
        if (members == null || members.Length == 0)
        {
            return null;
        }

        var types = new Type[members.Length];
        for (var i = 0; i < members.Length; i++)
        {
            types[i] = members[i] switch
            {
                ParameterMetadata param => param.Type,
                PropertyMetadata prop => prop.Type,
                _ => throw new InvalidOperationException(
                    $"Unexpected member metadata type {members[i].GetType().Name} in GetMemberTypes")
            };
        }
        return types;
    }

    private static readonly ConcurrentDictionary<Type, bool> IsAsyncEnumerableCache = new();

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The 'type' parameter is annotated with DynamicallyAccessedMemberTypes.Interfaces, so its interfaces are preserved; the closure-free factory only forwards that same Type instance.")]
    private static bool IsAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        // Cache per-type: GetInterfaces() allocates an array on every call and the
        // result is invariant for a given Type. GetOrAdd with a static (closure-free)
        // factory ensures concurrent callers don't each run the interface scan.
        return IsAsyncEnumerableCache.GetOrAdd(type, static t =>
        {
            foreach (var i in t.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    return true;
                }
            }

            return false;
        });
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic code usage is documented. AOT-safe path available via Factory property")]
    private static async IAsyncEnumerable<object?> ConvertToAsyncEnumerable(object asyncEnumerable, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var type = asyncEnumerable.GetType();
        
        // Find the IAsyncEnumerable<T> interface
        var asyncEnumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
        
        if (asyncEnumerableInterface is null)
        {
            throw new InvalidOperationException($"Type {type.Name} does not implement IAsyncEnumerable<T>");
        }
        
        // Get the GetAsyncEnumerator method from the interface
        var enumeratorMethod = asyncEnumerableInterface.GetMethod("GetAsyncEnumerator");
        
        if (enumeratorMethod is null)
        {
            throw new InvalidOperationException($"Could not find GetAsyncEnumerator method on interface {asyncEnumerableInterface.Name}");
        }
        
        var enumerator = enumeratorMethod.Invoke(asyncEnumerable, [cancellationToken]);

        // The enumerator might not have MoveNextAsync directly on its type,
        // we need to look for it on the IAsyncEnumerator<T> interface
        var enumeratorType = enumerator!.GetType();
        
        // Find MoveNextAsync - first try the type directly, then check interfaces
        var moveNextMethod = enumeratorType.GetMethod("MoveNextAsync");
        if (moveNextMethod is null)
        {
            // Look for it on the IAsyncEnumerator<T> interface
            var asyncEnumeratorInterface = enumeratorType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IAsyncEnumerator<>));
            
            if (asyncEnumeratorInterface != null)
            {
                moveNextMethod = asyncEnumeratorInterface.GetMethod("MoveNextAsync");
            }
        }
        
        // Similarly for Current property
        var currentProperty = enumeratorType.GetProperty("Current");
        if (currentProperty is null)
        {
            // Look for it on the IAsyncEnumerator<T> interface
            var asyncEnumeratorInterface = enumeratorType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IAsyncEnumerator<>));
            
            if (asyncEnumeratorInterface != null)
            {
                currentProperty = asyncEnumeratorInterface.GetProperty("Current");
            }
        }

        while (true)
        {
            var moveNextTask = (ValueTask<bool>)moveNextMethod!.Invoke(enumerator, null)!;
            if (!await moveNextTask.ConfigureAwait(false))
            {
                break;
            }

            yield return currentProperty!.GetValue(enumerator);
        }

        // Dispose the enumerator
        var disposeMethod = enumerator.GetType().GetMethod("DisposeAsync");
        if (disposeMethod != null)
        {
            var disposeTask = (ValueTask)disposeMethod.Invoke(enumerator, null)!;
            await disposeTask.ConfigureAwait(false);
        }
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo?> ValueTaskAsTaskMethodCache = new();

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "ValueTask<T>.AsTask is a public member of a framework type. AOT-safe path available via Factory property.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ValueTask<T>.AsTask is a public member of a framework type. AOT-safe path available via Factory property.")]
    private static Task? TryConvertValueTaskToTask(object methodResult)
    {
        var asTaskMethod = ValueTaskAsTaskMethodCache.GetOrAdd(methodResult.GetType(), static type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>)
                ? type.GetMethod(nameof(ValueTask<object>.AsTask), Type.EmptyTypes)
                : null);

        return asTaskMethod?.Invoke(methodResult, null) as Task;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic code usage is documented. AOT-safe path available via Factory property")]
    private static object? GetTaskResult(Task task)
    {
        var taskType = task.GetType();

        if (taskType.IsGenericType)
        {
            var resultProperty = taskType.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return null;
    }

    /// <summary>
    /// Gets an existing test class instance or creates a new one.
    /// Uses InstanceFactory if available (which can perform property injection),
    /// otherwise falls back to Activator.CreateInstance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Reflection usage is documented. AOT-safe path available via Factory property")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic code usage is documented. AOT-safe path available via Factory property")]
    private static async Task<object?> GetOrCreateInstanceAsync(DataGeneratorMetadata metadata, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType)
    {
        // First check if we have a valid test class instance
        var testClassInstance = metadata.TestClassInstance;
        if (testClassInstance is PlaceholderInstance)
        {
            testClassInstance = null;
        }

        if (testClassInstance != null && targetType.IsInstanceOfType(testClassInstance))
        {
            return testClassInstance;
        }

        // Try to use the InstanceFactory if available (which can perform property injection)
        if (metadata.InstanceFactory != null)
        {
            return await metadata.InstanceFactory(targetType);
        }

        // Fall back to creating a bare instance
        return Activator.CreateInstance(targetType);
    }
}

// These cross-type overloads must lose to the source-generated same-type IsEqualTo / IsNotEqualTo
// when the expected argument is the same type as the source (e.g. Guid vs Guid), where both are
// applicable. Two layered mechanisms achieve that:
//
//  1. The trailing `params CrossTypeOverloadMarker[]` makes each overload applicable only in
//     EXPANDED form, so a same-type call binds to the normal-form same-type overload via the C#
//     "normal beats expanded" tie-break (§12.6.4.2). This is honored by EVERY Roslyn version,
//     including the .NET 8 SDK's compiler, which is the only mechanism that fixes #6296.
//  2. [OverloadResolutionPriority(-1)] is kept as a redundant signal for completeness on modern
//     compilers, but it is NOT load-bearing — it is honored only by Roslyn 4.12+ (.NET 9 SDK and
//     later), so consumers pinned to the .NET 8 SDK via global.json never see its effect.
//
// Why CrossTypeOverloadMarker and not `params object[]`: an `object[]` marker is applicable in
// expanded form to ANY trailing argument, so a call like
// `Assert.That(productCode).IsEqualTo("x", StringComparer.OrdinalIgnoreCase)` would silently bind
// here and DISCARD the comparer — a false-pass footgun for a test framework (PR #6313 review).
// CrossTypeOverloadMarker has no accessible constructor, so callers can never supply an element:
// the overload stays applicable only with ZERO trailing args (the tie-break case), and any stray
// trailing argument is a compile error again, exactly as it was before #6296.
//
// Conversion direction (#7073): the tie-break above only applies when both overloads have the same
// parameter types. When the expected value merely CONVERTS implicitly to the source type
// (`Assert.That(3m).IsEqualTo(3)`, `Assert.That((decimal?)x).IsEqualTo(3m)`), identity on TOther is
// a better conversion than int -> decimal, so only ORP keeps the same-type overload ahead — and
// C# 12 ignores ORP. Rather than depend on it, these overloads compare in the SOURCE type whenever
// TOther -> TValue is implicit (exactly the case C# 13+ binds to the same-type overload), and only
// otherwise convert the actual value to TOther. Built-in implicit conversions (numeric widening,
// nullable wrapping, reference/boxing) count alongside user-defined operators, so e.g.
// `Assert.That(3).IsEqualTo(3.0)` works on every language version too. The value-dependent implicit
// constant conversion (`Assert.That(0U).IsEqualTo(0)`, int constant -> uint) also counts when the value
// is in range, because C# 13+ binds those calls to the same-type overload as well.
//
// History: #5765 / #6276 / #6280 tried to fix this purely via ORP + a LangVersion bump in
// TUnit.Assertions.props. That bump is a no-op for the real failure because ORP honoring tracks
// the build SDK's Roslyn version, not LangVersion — see #6296. The `params` tie-break is what
// makes this compiler-version-independent. The unused `_` parameter is never read.
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Adds <c>IsEqualTo</c> overloads accepting an expected value of a different type when one of
/// the two types converts implicitly to the other. Defined as a partial of
/// the source-generated <c>EqualsAssertionExtensions</c> so it shares the containing type. The
/// trailing <c>params</c> array on each overload keeps it from displacing the same-type overload
/// for same-type calls (e.g. enum or value-type sources) — see the file header for the why.
///
/// Enables ergonomic comparison of wrapper Value Objects against their wrapped primitive,
/// e.g. <c>await Assert.That(productCode).IsEqualTo("Example")</c> when
/// <c>ProductCode</c> defines <c>public static implicit operator string(ProductCode pc)</c>,
/// and of numbers of different types, e.g. <c>await Assert.That(3).IsEqualTo(3.0)</c>.
/// </summary>
public static partial class EqualsAssertionExtensions
{
    /// <summary>
    /// Asserts equality between the source value and an expected value of a different type.
    /// When <typeparamref name="TOther"/> converts implicitly to <typeparamref name="TValue"/>
    /// (user-defined operator, numeric widening, nullable or reference conversion), the values are
    /// compared as <typeparamref name="TValue"/>, as the same-type overload would. Otherwise the
    /// source value is converted implicitly to <typeparamref name="TOther"/> and compared to
    /// <paramref name="expected"/> using <see cref="EqualsAssertion{T}"/>.
    /// </summary>
    [OverloadResolutionPriority(-1)]
    [RequiresUnreferencedCode("Looks up implicit conversion operators via reflection. Trimming may remove user-defined operators.")]
    public static EqualsAssertion<TOther> IsEqualTo<TValue, TOther>(
        this IAssertionSource<TValue> source,
        TOther? expected,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null,
        // Expanded-form-only marker so a same-type call loses to the same-type overload; never read.
        // Element type has no accessible ctor, so stray trailing args stay a compile error. See file header.
        params CrossTypeOverloadMarker[] _)
    {
        source.Context.ExpressionBuilder.Append($".IsEqualTo({expectedExpression})");

        if ((ImplicitConversionCache.TryGetConverter<TOther, TValue>(ConversionNullHandling.InvokeOperator)
             ?? ImplicitConversionCache.TryGetConstantConverter<TOther, TValue>(expected)) is { } toSourceType)
        {
            // Convert the expected value here, as C# 13+ does at the call site of the same-type overload.
            var expectedAsSource = toSourceType(expected);
            var sourceTyped = source.Context.Map(CrossTypeComparison.CreateSourceTypeMapper<TValue, TOther>(toSourceType));
            return new SourceTypeEqualsAssertion<TValue, TOther>(sourceTyped, source.Context.Evaluation, expected, expectedAsSource);
        }

        var converter = ImplicitConversionCache.GetConverter<TValue, TOther>();
        var mapped = source.Context.Map(converter);
        return new EqualsAssertion<TOther>(mapped, expected);
    }
}

/// <summary>
/// Adds <c>IsNotEqualTo</c> overloads accepting an unexpected value of a different type when
/// one of the two types converts implicitly to the other. See remarks on
/// <see cref="EqualsAssertionExtensions"/> for why this is a partial extension class.
/// </summary>
public static partial class NotEqualsAssertionExtensions
{
    /// <summary>
    /// Asserts inequality between the source value and an unexpected value of a different type.
    /// When <typeparamref name="TOther"/> converts implicitly to <typeparamref name="TValue"/>, the
    /// values are compared as <typeparamref name="TValue"/>, as the same-type overload would.
    /// Otherwise the source value is converted implicitly to <typeparamref name="TOther"/> and
    /// compared to <paramref name="notExpected"/> using <see cref="NotEqualsAssertion{T}"/>.
    /// </summary>
    [OverloadResolutionPriority(-1)]
    [RequiresUnreferencedCode("Looks up implicit conversion operators via reflection. Trimming may remove user-defined operators.")]
    public static NotEqualsAssertion<TOther> IsNotEqualTo<TValue, TOther>(
        this IAssertionSource<TValue> source,
        TOther? notExpected,
        [CallerArgumentExpression(nameof(notExpected))] string? notExpectedExpression = null,
        // Expanded-form-only marker so a same-type call loses to the same-type overload; never read.
        // Element type has no accessible ctor, so stray trailing args stay a compile error. See file header.
        params CrossTypeOverloadMarker[] _)
    {
        source.Context.ExpressionBuilder.Append($".IsNotEqualTo({notExpectedExpression})");

        if ((ImplicitConversionCache.TryGetConverter<TOther, TValue>(ConversionNullHandling.InvokeOperator)
             ?? ImplicitConversionCache.TryGetConstantConverter<TOther, TValue>(notExpected)) is { } toSourceType)
        {
            var notExpectedAsSource = toSourceType(notExpected);
            var sourceTyped = source.Context.Map(CrossTypeComparison.CreateSourceTypeMapper<TValue, TOther>(toSourceType));
            return new SourceTypeNotEqualsAssertion<TValue, TOther>(sourceTyped, source.Context.Evaluation, notExpected!, notExpectedAsSource!);
        }

        var converter = ImplicitConversionCache.GetConverter<TValue, TOther>();
        var mapped = source.Context.Map(converter);
        return new NotEqualsAssertion<TOther>(mapped, notExpected!);
    }
}

/// <summary>
/// Marker element type for the trailing <c>params</c> on the cross-type
/// <c>IsEqualTo</c> / <c>IsNotEqualTo</c> overloads. Its only purpose is to make those overloads
/// applicable solely in <em>expanded</em> form so a same-type call loses to the same-type overload
/// (see <see cref="EqualsAssertionExtensions"/>). It has no accessible constructor, so callers can
/// never pass an element — the overloads bind only with zero trailing arguments, and any stray
/// trailing argument remains a compile error rather than being silently discarded. Never instantiated.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CrossTypeOverloadMarker
{
    private CrossTypeOverloadMarker() { }
}

/// <summary>
/// Source-type comparison for the cross-type overloads when <c>TOther</c> converts implicitly to
/// <c>TValue</c> (see the file header). <see cref="SourceTypeEqualsAssertion{TValue, TOther}"/> and
/// <see cref="SourceTypeNotEqualsAssertion{TValue, TOther}"/> delegate the check to the same-type
/// <see cref="EqualsAssertion{T}"/> / <see cref="NotEqualsAssertion{T}"/> over the source value, so the
/// result, the failure message and options such as <c>IgnoringType</c> match the same-type overload.
/// The returned assertion is still typed on <c>TOther</c>, so the mapper only supplies the value that
/// chained assertions (and <c>await</c>) see: the actual value itself when it already is a <c>TOther</c>
/// (preserving reference identity), otherwise its exact <c>TOther</c> equivalent. It is never replaced
/// by the expected value. When there is no exact equivalent (e.g. <c>3.5m</c> for <c>int</c>), the
/// mapper raises <see cref="CrossTypeValueNotRepresentableException"/>. The source-type assertions do
/// not read the mapped value, so that only affects chained assertions.
/// </summary>
internal static class CrossTypeComparison
{
    [RequiresUnreferencedCode("Reflects over user-defined operators on TValue and TOther.")]
    public static Func<TValue?, TOther?> CreateSourceTypeMapper<TValue, TOther>(Func<TOther?, TValue?> toSourceType)
    {
        return actual => ImplicitConversionCache.TryConvertExactly(actual, toSourceType, out var converted)
            ? converted
            : throw new CrossTypeValueNotRepresentableException(actual, typeof(TValue), typeof(TOther));
    }

    /// <summary>
    /// Reads the source value directly, so a failed mapping to <c>TOther</c> never affects the result.
    /// An exception from evaluating the source itself is passed on and reported as usual.
    /// </summary>
    public static async Task<EvaluationMetadata<TValue>> GetSourceMetadataAsync<TValue, TOther>(
        EvaluationContext<TValue> source,
        EvaluationMetadata<TOther> mapped)
    {
        // Already evaluated (the mapped context evaluates it first), so this is the cached value.
        var (actual, exception) = await source.GetAsync().ConfigureAwait(false);
        return new EvaluationMetadata<TValue>(actual, exception, mapped.StartTime, mapped.EndTime);
    }

    public static string Format(object? value) => value?.ToString() ?? "null";
}

/// <summary>
/// Raised while mapping the actual value to <c>TOther</c> when it has no exact <c>TOther</c>
/// equivalent, e.g. <c>3.5m</c> for <c>int</c>, or <see langword="null"/> for <c>decimal</c>.
/// </summary>
internal sealed class CrossTypeValueNotRepresentableException(object? actual, Type sourceType, Type otherType)
    : Exception($"The value {CrossTypeComparison.Format(actual)} of type '{sourceType}' has no equivalent '{otherType}' value.");

internal sealed class SourceTypeEqualsAssertion<TValue, TOther>(
    AssertionContext<TOther> context,
    EvaluationContext<TValue> source,
    TOther? expected,
    TValue? expectedAsSource)
    : EqualsAssertion<TOther>(context, expected)
{
    private EqualsAssertion<TValue>? _sameType;

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TOther> metadata)
    {
        var sourceMetadata = await CrossTypeComparison.GetSourceMetadataAsync(source, metadata).ConfigureAwait(false);
        return await SameType().InternalCheckAsync(sourceMetadata).ConfigureAwait(false);
    }

    protected override string GetExpectation() => SameType().InternalGetExpectation();

    // The same-type assertion with this assertion's IgnoringType options (re-applied on each use, as
    // they can be added after construction). Its own expression builder keeps those calls out of the message.
    private EqualsAssertion<TValue> SameType()
    {
        _sameType ??= new EqualsAssertion<TValue>(new AssertionContext<TValue>(source, new StringBuilder()), expectedAsSource);
        foreach (var type in IgnoredTypes)
        {
            _sameType.IgnoringType(type);
        }

        return _sameType;
    }
}

internal sealed class SourceTypeNotEqualsAssertion<TValue, TOther>(
    AssertionContext<TOther> context,
    EvaluationContext<TValue> source,
    TOther notExpected,
    TValue notExpectedAsSource)
    : NotEqualsAssertion<TOther>(context, notExpected)
{
    private NotEqualsAssertion<TValue>? _sameType;

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TOther> metadata)
    {
        var sourceMetadata = await CrossTypeComparison.GetSourceMetadataAsync(source, metadata).ConfigureAwait(false);
        return await SameType().InternalCheckAsync(sourceMetadata).ConfigureAwait(false);
    }

    protected override string GetExpectation() => SameType().InternalGetExpectation();

    // The same-type assertion with this assertion's IgnoringType options (re-applied on each use, as
    // they can be added after construction). Its own expression builder keeps those calls out of the message.
    private NotEqualsAssertion<TValue> SameType()
    {
        _sameType ??= new NotEqualsAssertion<TValue>(new AssertionContext<TValue>(source, new StringBuilder()), notExpectedAsSource);
        foreach (var type in IgnoredTypes)
        {
            _sameType.IgnoringType(type);
        }

        return _sameType;
    }
}

/// <summary>
/// How a converter built from a user-defined operator treats a <see langword="null"/> input.
/// </summary>
internal enum ConversionNullHandling
{
    /// <summary>
    /// Map <see langword="null"/> to <see langword="default"/> without calling the operator. Used to
    /// convert the actual value, so a null wrapper compares as null instead of throwing in an
    /// operator such as <c>implicit operator string(ProductCode pc) =&gt; pc.Value</c>.
    /// </summary>
    ReturnDefault,

    /// <summary>
    /// Call the operator with <see langword="null"/>, as C# does. Used to convert the expected value to
    /// the source type, where the same-type overload would run the operator at the call site (#7086).
    /// </summary>
    InvokeOperator,
}

/// <summary>
/// Caches implicit conversions from <c>TFrom</c> to <c>TTo</c>: user-defined operators plus the
/// built-in reference, boxing, nullable and numeric-widening conversions. Lookup is performed once
/// per type pair and the resulting delegate is reused.
/// </summary>
internal static class ImplicitConversionCache
{
    // A null value means "no implicit conversion"; GetOrAdd caches that negative result too.
    private static readonly ConcurrentDictionary<(Type From, Type To, ConversionNullHandling NullHandling), Delegate?> Cache = new();

    /// <summary>
    /// Returns the implicit converter from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>,
    /// or a converter that throws a diagnostic when no implicit conversion exists.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    public static Func<TFrom?, TTo?> GetConverter<TFrom, TTo>()
    {
        return TryGetConverter<TFrom, TTo>() ?? (_ => throw new InvalidOperationException(
            $"No implicit conversion operator from '{typeof(TFrom)}' to '{typeof(TTo)}' was found. " +
            $"IsEqualTo / IsNotEqualTo with a different argument type requires an implicit conversion between the two types, " +
            $"e.g. '{typeof(TFrom).Name}' defining 'public static implicit operator {typeof(TTo).Name}({typeof(TFrom).Name} value)'."));
    }

    /// <summary>
    /// Returns the implicit converter from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>,
    /// or <see langword="null"/> when no implicit conversion exists. <paramref name="nullHandling"/>
    /// controls whether a user-defined operator sees <see langword="null"/> inputs.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    public static Func<TFrom?, TTo?>? TryGetConverter<TFrom, TTo>(ConversionNullHandling nullHandling = ConversionNullHandling.ReturnDefault)
    {
        var key = (typeof(TFrom), typeof(TTo), nullHandling);
        return (Func<TFrom?, TTo?>?)Cache.GetOrAdd(key, static k => BuildConverter<TFrom, TTo>(k.NullHandling));
    }

    /// <summary>
    /// Returns a converter for the C# implicit constant expression conversion (§10.2.11) when it would
    /// apply to <paramref name="value"/>: an <c>int</c> converts to <c>sbyte</c>, <c>byte</c>, <c>short</c>,
    /// <c>ushort</c>, <c>uint</c>, <c>ulong</c>, <c>nint</c> or <c>nuint</c> (or their nullable forms), and a
    /// <c>long</c> to <c>ulong</c>, when the value is in range. A numeric zero (any numeric type except
    /// <c>char</c>, as Roslyn accepts) also converts to any enum or nullable enum (§10.2.4). C# 13+ uses these
    /// to bind <c>Assert.That(0U).IsEqualTo(0)</c> and <c>Assert.That(MyEnum.None).IsEqualTo(0)</c> to the
    /// same-type overload, while C# 12 binds the cross-type one.
    /// Whether the argument was a constant is not known at run time, so any qualifying value does.
    /// Returns <see langword="null"/> when the conversion does not apply or the value does not qualify.
    /// </summary>
    public static Func<TFrom?, TTo?>? TryGetConstantConverter<TFrom, TTo>(TFrom? value)
    {
        return value is not null && ConstantConversion<TFrom, TTo>.TryConvert(value, out _)
            ? ConstantConversion<TFrom, TTo>.Converter
            : null;
    }

    private static class ConstantConversion<TFrom, TTo>
    {
        // The non-nullable target type when a constant TFrom can convert implicitly to TTo, otherwise null.
        private static readonly Type? Target = GetTarget();

        public static readonly Func<TFrom?, TTo?> Converter = static value =>
            value is not null && TryConvert(value, out var converted)
                ? (TTo?)converted
                : throw new OverflowException($"The value {CrossTypeComparison.Format(value)} has no implicit constant conversion to '{typeof(TTo)}'.");

        public static bool TryConvert(object value, out object? result)
        {
            result = null;
            if (Target is null)
            {
                return false;
            }

            if (Target.IsEnum)
            {
                // Only a zero converts implicitly to an enum. -0.0 counts too, as it does for the compiler.
                if (!TryConvertNumeric(value, typeof(double), out var asDouble) || (double)asDouble! != 0)
                {
                    return false;
                }

                result = Enum.ToObject(Target, 0);
                return true;
            }

            return TryConvertNumeric(value, Target, out result);
        }

        private static Type? GetTarget()
        {
            var target = Nullable.GetUnderlyingType(typeof(TTo)) ?? typeof(TTo);

            if (target.IsEnum)
            {
                return typeof(TFrom) != typeof(char) && IsNumeric(typeof(TFrom)) ? target : null;
            }

            if (typeof(TFrom) == typeof(int))
            {
                return IsNativeInteger(target) || Type.GetTypeCode(target) is TypeCode.SByte or TypeCode.Byte
                    or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64
                    ? target
                    : null;
            }

            return typeof(TFrom) == typeof(long) && target == typeof(ulong) ? target : null;
        }
    }

    /// <summary>
    /// Finds the <typeparamref name="TTo"/> value that <paramref name="toFrom"/> converts back to exactly
    /// <paramref name="value"/>, if there is one: <c>4m</c> maps to <c>4</c>, but <c>3.5m</c> has no
    /// <c>int</c> equivalent and is never truncated.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    public static bool TryConvertExactly<TFrom, TTo>(TFrom? value, Func<TTo?, TFrom?> toFrom, out TTo? result)
    {
        result = default;

        if (value is TTo direct)
        {
            // Reference, boxing or identity conversion: keep the actual instance.
            result = direct;
            return true;
        }

        if (value is null)
        {
            // C# passes null to a user-defined operator, which may map it to a non-null TTo (a sentinel such
            // as Wrapper.None). Prefer that equivalent, as `(TTo)actual` would give, then null itself.
            return (TryGetConverter<TFrom, TTo>(ConversionNullHandling.InvokeOperator) is { } forward
                    && TryRoundTrip(value, v => forward(v), toFrom, out result))
                   || TryRoundTrip(value, static _ => null, toFrom, out result);
        }

        object? candidate;
        try
        {
            if (TryConvertNumeric(
                         // An enum compares through its underlying number (MyEnum.None -> 0).
                         value is Enum e ? Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture) : value,
                         Nullable.GetUnderlyingType(typeof(TTo)) ?? typeof(TTo),
                         out var numeric))
            {
                // Also covers explicit numeric conversions (decimal -> int); the round trip rejects lossy ones.
                candidate = numeric;
            }
            else if (TryGetConverter<TFrom, TTo>() is { } converter)
            {
                candidate = converter(value);
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
            // A user-defined operator rejected the value: there is no exact equivalent.
            return false;
        }

        return TryRoundTrip(value, _ => candidate, toFrom, out result);
    }

    /// <summary>
    /// Accepts the <typeparamref name="TTo"/> value that <paramref name="candidate"/> produces for
    /// <paramref name="value"/> when <paramref name="toFrom"/> converts it back to exactly <paramref name="value"/>.
    /// </summary>
    private static bool TryRoundTrip<TFrom, TTo>(TFrom? value, Func<TFrom?, object?> candidate, Func<TTo?, TFrom?> toFrom, out TTo? result)
    {
        result = default;
        try
        {
            var converted = candidate(value);
            if (converted is null && default(TTo) is not null)
            {
                return false;
            }

            var typed = (TTo?)converted;
            if (!EqualityComparer<TFrom>.Default.Equals(toFrom(typed)!, value!))
            {
                return false;
            }

            result = typed;
            return true;
        }
        catch (Exception)
        {
            // A user-defined operator rejected the value in either direction: there is no exact equivalent.
            return false;
        }
    }

    /// <summary>
    /// Builds the C# implicit conversion from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>, if
    /// there is one. Values are handled boxed, where <c>T?</c> and <c>T</c> look the same (a boxed
    /// <c>int?</c> is a boxed <c>int</c> or null), so nullable wrapping and lifting are applied once here
    /// for every kind of conversion rather than per kind.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    private static Func<TFrom?, TTo?>? BuildConverter<TFrom, TTo>(ConversionNullHandling nullHandling)
    {
        var fromType = typeof(TFrom);
        var toType = typeof(TTo);
        var fromUnderlying = Nullable.GetUnderlyingType(fromType);
        var toUnderlying = Nullable.GetUnderlyingType(toType);

        // A user-defined operator with exactly this signature. C# calls it even with null.
        if (FindImplicitOperator(fromType, toType) is { } exact)
        {
            return nullHandling == ConversionNullHandling.InvokeOperator
                ? value => (TTo?)InvokeOperator(exact, value)
                : value => value is null ? default : (TTo?)InvokeOperator(exact, value);
        }

        // Reference, boxing and nullable-wrapping conversions (string -> object, decimal -> decimal?),
        // including boxing T? to an interface that T implements (int? -> IComparable).
        if (toType.IsAssignableFrom(fromType)
            || (fromUnderlying is not null && !toType.IsValueType && toType.IsAssignableFrom(fromUnderlying)))
        {
            return static value => (TTo?)(object?)value;
        }

        // T? -> U is explicit. T? -> U? is the lifted form of T -> U, and T -> U? is T -> U followed by wrapping.
        if (fromUnderlying is not null && toUnderlying is null)
        {
            return null;
        }

        var source = fromUnderlying ?? fromType;
        var target = toUnderlying ?? toType;

        if (FindImplicitOperator(source, target) is { } op)
        {
            // A lifted conversion maps null to null; only a reference-type source passes null to the operator.
            var invokeOnNull = nullHandling == ConversionNullHandling.InvokeOperator && fromUnderlying is null;
            return value => value is null && !invokeOnNull ? default : (TTo?)InvokeOperator(op, value);
        }

        if (IsImplicitNumericConversion(source, target))
        {
            return value => value is not null && TryConvertNumeric(value, target, out var converted) ? (TTo?)converted : default;
        }

        return null;
    }

    private static object? InvokeOperator(MethodInfo op, object? value)
    {
        // MethodInfo.Invoke boxes value-type arguments on every call. A compiled expression avoids the box
        // but requires [RequiresDynamicCode], which breaks Native AOT compatibility, so we accept the boxing.
        try
        {
            return op.Invoke(null, [value]);
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            // Surface the operator's own exception, as a direct conversion in user code would.
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    private static bool IsNumeric(Type type)
        => IsNativeInteger(type) || ((type.IsPrimitive || type == typeof(decimal)) && Type.GetTypeCode(type) is
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
            or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Char or TypeCode.Single or TypeCode.Double or TypeCode.Decimal);

    // nint / nuint have TypeCode.Object, so they are matched by type.
    private static bool IsNativeInteger(Type type) => type == typeof(nint) || type == typeof(nuint);

    /// <summary>The C# implicit numeric conversions (C# spec §10.2.3, including nint / nuint) between non-nullable types.</summary>
    private static bool IsImplicitNumericConversion(Type from, Type to)
    {
        if (!IsNumeric(from) || !IsNumeric(to))
        {
            return false;
        }

        if (from == typeof(nint))
        {
            return Type.GetTypeCode(to) is TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
        }

        if (from == typeof(nuint))
        {
            return Type.GetTypeCode(to) is TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
        }

        if (to == typeof(nint))
        {
            return Type.GetTypeCode(from) is TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.Char;
        }

        if (to == typeof(nuint))
        {
            return Type.GetTypeCode(from) is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.Char;
        }

        var target = Type.GetTypeCode(to);
        return Type.GetTypeCode(from) switch
        {
            TypeCode.SByte => target is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Byte => target is TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int16 => target is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt16 => target is TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int32 => target is TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.UInt32 => target is TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Int64 or TypeCode.UInt64 => target is TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Char => target is TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal,
            TypeCode.Single => target is TypeCode.Double,
            _ => false,
        };
    }

    /// <summary>
    /// Converts a boxed number to <paramref name="target"/>, a non-nullable numeric type, without
    /// reflection or dynamic code. Lossy conversions round or fail; callers that need exactness
    /// check the round trip.
    /// </summary>
    private static bool TryConvertNumeric(object value, Type target, out object? result)
    {
        result = null;
        if (!IsNumeric(value.GetType()) || !IsNumeric(target))
        {
            return false;
        }

        try
        {
            // IConvertible converts char only to and from integral types, so route char through int
            // in both directions: 'A' -> 65 -> 65.0, and 65.0 -> 65 -> 'A'.
            // nint / nuint are not IConvertible either, so route them through long / ulong.
            value = value switch
            {
                char c => (int)c,
                nint n => (long)n,
                nuint n => (ulong)n,
                float or double or decimal when target == typeof(char) => Convert.ToInt32(value, CultureInfo.InvariantCulture),
                _ => value,
            };

            result = target == typeof(nint) ? checked((nint)Convert.ToInt64(value, CultureInfo.InvariantCulture))
                : target == typeof(nuint) ? checked((nuint)Convert.ToUInt64(value, CultureInfo.InvariantCulture))
                : Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is OverflowException or InvalidCastException)
        {
            return false;
        }
    }

    /// <summary>
    /// Looks for <c>public static implicit operator <paramref name="toType"/>(<paramref name="fromType"/>)</c>.
    /// C# requires it to be declared on the source or target type (on the underlying type, for an operator
    /// with a nullable signature) or one of their base classes, so both hierarchies are searched. An operator
    /// taking a base class of <paramref name="fromType"/> also applies; candidates from both hierarchies are
    /// ranked together, the exact parameter type winning, then the most derived one, as C# picks the most
    /// specific source type (§10.5.4).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "See IL2072.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "User-defined operators are looked up reflectively; the warning is surfaced via RequiresUnreferencedCode on the public entry points, and if they are trimmed, IsEqualTo throws InvalidOperationException at call time.")]
    private static MethodInfo? FindImplicitOperator(Type fromType, Type toType)
    {
        MethodInfo? best = null;
        Type? bestParameter = null;

        var sourceDeclaring = Nullable.GetUnderlyingType(fromType) ?? fromType;
        var targetDeclaring = Nullable.GetUnderlyingType(toType) ?? toType;

        if (CollectImplicitOperators(sourceDeclaring, fromType, toType, ref best, ref bestParameter) || targetDeclaring == sourceDeclaring)
        {
            return best;
        }

        CollectImplicitOperators(targetDeclaring, fromType, toType, ref best, ref bestParameter);
        return best;
    }

    /// <summary>
    /// Updates <paramref name="best"/> with any more specific applicable operator declared on
    /// <paramref name="definedOn"/> or its base classes. Returns <see langword="true"/> once an operator
    /// taking exactly <paramref name="fromType"/> is found, as nothing can be more specific.
    /// </summary>
    private static bool CollectImplicitOperators(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type definedOn,
        Type fromType,
        Type toType,
        ref MethodInfo? best,
        ref Type? bestParameter)
    {
        foreach (var method in definedOn.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (method.Name != "op_Implicit" || method.ReturnType != toType)
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                continue;
            }

            var parameterType = parameters[0].ParameterType;
            if (parameterType == fromType)
            {
                best = method;
                bestParameter = parameterType;
                return true;
            }

            if (fromType.IsSubclassOf(parameterType)
                && (bestParameter is null || parameterType.IsSubclassOf(bestParameter)))
            {
                best = method;
                bestParameter = parameterType;
            }
        }

        return false;
    }
}

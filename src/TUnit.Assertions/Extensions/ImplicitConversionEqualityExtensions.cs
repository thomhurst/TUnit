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
// `Assert.That(3).IsEqualTo(3.0)` works on every language version too.
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

        if (ImplicitConversionCache.TryGetConverter<TOther, TValue>(ConversionNullHandling.InvokeOperator) is { } toSourceType)
        {
            var sourceTyped = source.Context.Map(CrossTypeComparison.CreateSourceTypeMapper<TValue, TOther>(toSourceType));
            return new SourceTypeEqualsAssertion<TValue, TOther>(sourceTyped, source.Context.Evaluation, toSourceType, expected);
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

        if (ImplicitConversionCache.TryGetConverter<TOther, TValue>(ConversionNullHandling.InvokeOperator) is { } toSourceType)
        {
            var sourceTyped = source.Context.Map(CrossTypeComparison.CreateSourceTypeMapper<TValue, TOther>(toSourceType));
            return new SourceTypeNotEqualsAssertion<TValue, TOther>(sourceTyped, source.Context.Evaluation, toSourceType, notExpected!);
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
/// <c>TValue</c> (see the file header). The equality result is computed as <c>TValue</c> by
/// <see cref="SourceTypeEqualsAssertion{TValue, TOther}"/> and
/// <see cref="SourceTypeNotEqualsAssertion{TValue, TOther}"/>, which read the source value directly.
/// The returned assertion is still typed on <c>TOther</c>, so the mapper only supplies the value that
/// chained assertions (and <c>await</c>) see: the actual value itself when it already is a <c>TOther</c>
/// (preserving reference identity), otherwise its exact <c>TOther</c> equivalent. It is never replaced
/// by the expected value. When there is no exact equivalent (e.g. <c>3.5m</c> for <c>int</c>), the
/// mapper raises <see cref="CrossTypeValueNotRepresentableException"/>, which the source-type
/// assertions ignore because their result does not depend on the mapped value.
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
    /// Compares the source value with the expected value as <typeparamref name="TValue"/>, as the
    /// same-type overload would. A conversion that throws (e.g. a user-defined operator rejecting
    /// <see langword="null"/>) is reported as the failure's exception.
    /// </summary>
    public static async Task<(bool Equal, TValue? Actual, Exception? Exception)> CompareAsSourceTypeAsync<TValue, TOther>(
        EvaluationContext<TValue> source,
        Func<TOther?, TValue?> toSourceType,
        TOther? expected)
    {
        // Already evaluated (the mapped context evaluates it first), so this is the cached value.
        var (actual, _) = await source.GetAsync().ConfigureAwait(false);

        TValue? expectedAsSource;
        try
        {
            expectedAsSource = toSourceType(expected);
        }
        catch (Exception e)
        {
            return (false, actual, e);
        }

        return (EqualityComparer<TValue>.Default.Equals(actual!, expectedAsSource!), actual, null);
    }

    public static string Format(object? value) => value?.ToString() ?? "null";
}

/// <summary>
/// Raised while mapping the actual value to <c>TOther</c> when it has no exact <c>TOther</c>
/// equivalent, e.g. <c>3.5m</c> for <c>int</c>, or <see langword="null"/> for <c>decimal</c>.
/// </summary>
internal sealed class CrossTypeValueNotRepresentableException(object? actual, Type sourceType, Type otherType)
    : Exception($"The value {CrossTypeComparison.Format(actual)} of type '{sourceType}' has no equivalent '{otherType}' value.")
{
    public object? Actual { get; } = actual;
}

internal sealed class SourceTypeEqualsAssertion<TValue, TOther>(
    AssertionContext<TOther> context,
    EvaluationContext<TValue> source,
    Func<TOther?, TValue?> toSourceType,
    TOther? expected)
    : EqualsAssertion<TOther>(context, expected)
{
    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TOther> metadata)
    {
        // An exception from evaluating the source itself is reported as usual.
        if (metadata.Exception is not null and not CrossTypeValueNotRepresentableException)
        {
            return await base.CheckAsync(metadata).ConfigureAwait(false);
        }

        var (equal, actual, exception) = await CrossTypeComparison.CompareAsSourceTypeAsync(source, toSourceType, Expected).ConfigureAwait(false);
        if (exception is not null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}", exception);
        }

        if (equal)
        {
            return AssertionResult.Passed;
        }

        // Prefer the richer TOther failure message (collection previews, member diffs) when the
        // actual value has a TOther equivalent, but never let it turn a source-type mismatch into a pass.
        if (metadata.Exception is null)
        {
            var typedResult = await base.CheckAsync(metadata).ConfigureAwait(false);
            if (!typedResult.IsPassed)
            {
                return typedResult;
            }
        }

        return AssertionResult.Failed($"received {CrossTypeComparison.Format(actual)}");
    }
}

internal sealed class SourceTypeNotEqualsAssertion<TValue, TOther>(
    AssertionContext<TOther> context,
    EvaluationContext<TValue> source,
    Func<TOther?, TValue?> toSourceType,
    TOther notExpected)
    : NotEqualsAssertion<TOther>(context, notExpected)
{
    private readonly TOther _notExpected = notExpected;

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TOther> metadata)
    {
        if (metadata.Exception is not null and not CrossTypeValueNotRepresentableException)
        {
            return await base.CheckAsync(metadata).ConfigureAwait(false);
        }

        var (equal, actual, exception) = await CrossTypeComparison.CompareAsSourceTypeAsync(source, toSourceType, _notExpected).ConfigureAwait(false);
        if (exception is not null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}", exception);
        }

        return equal
            ? AssertionResult.Failed($"received {CrossTypeComparison.Format(actual)}")
            : AssertionResult.Passed;
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
    /// Finds the <typeparamref name="TTo"/> value that <paramref name="toFrom"/> converts back to exactly
    /// <paramref name="value"/>, if there is one: <c>4m</c> maps to <c>4</c>, but <c>3.5m</c> has no
    /// <c>int</c> equivalent and is never truncated.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    public static bool TryConvertExactly<TFrom, TTo>(TFrom? value, Func<TTo?, TFrom?> toFrom, out TTo? result)
    {
        result = default;

        object? candidate;
        if (value is TTo direct)
        {
            // Reference, boxing or identity conversion: keep the actual instance.
            candidate = direct;
        }
        else if (value is null && default(TTo) is null)
        {
            candidate = null;
        }
        else if (value is not null && TryConvertNumeric(value, Nullable.GetUnderlyingType(typeof(TTo)) ?? typeof(TTo), out var numeric))
        {
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

        if (candidate is null && default(TTo) is not null)
        {
            return false;
        }

        var typed = (TTo?)candidate;
        TFrom? roundTripped;
        try
        {
            roundTripped = toFrom(typed);
        }
        catch (Exception)
        {
            // e.g. a user-defined operator that rejects null: there is no exact equivalent.
            return false;
        }

        if (!EqualityComparer<TFrom>.Default.Equals(roundTripped!, value!))
        {
            return false;
        }

        result = typed;
        return true;
    }

    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "User-defined operators are public static methods on TFrom or TTo; if trimmed, IsEqualTo throws InvalidOperationException at call time.")]
    private static Func<TFrom?, TTo?>? BuildConverter<TFrom, TTo>(ConversionNullHandling nullHandling)
    {
        var fromType = typeof(TFrom);
        var toType = typeof(TTo);

        // C# allows `public static implicit operator TTo(TFrom)` to be declared on
        // either participant. Try TFrom first (the common Value Object pattern),
        // then fall back to TTo for cases where the conversion lives on the target.
        var op = FindImplicitOperator(fromType, fromType, toType)
                 ?? FindImplicitOperator(toType, fromType, toType);

        if (op is not null)
        {
            // MethodInfo.Invoke boxes value-type arguments on every call. An
            // Expression.Lambda<Func<TFrom,TTo>>(...).Compile() avoids the box but
            // requires [RequiresDynamicCode] which breaks Native AOT compatibility,
            // so we accept the boxing here.
            if (nullHandling == ConversionNullHandling.InvokeOperator)
            {
                return value => (TTo?)InvokeOperator(op, value);
            }

            return value =>
            {
                if (value is null)
                {
                    return default;
                }
                return (TTo?)InvokeOperator(op, value);
            };
        }

        // Built-in implicit reference, boxing and nullable-wrapping conversions
        // (string -> object, int -> IComparable, decimal -> decimal?).
        if (toType.IsAssignableFrom(fromType))
        {
            return static value => (TTo?)(object?)value;
        }

        // Built-in implicit numeric conversions and their lifted forms (int -> double,
        // int -> long?, int? -> decimal?). T? -> U is an explicit conversion, so it is excluded.
        var fromUnderlying = Nullable.GetUnderlyingType(fromType);
        var toUnderlying = Nullable.GetUnderlyingType(toType);
        if (fromUnderlying is not null && toUnderlying is null)
        {
            return null;
        }

        var numericTarget = toUnderlying ?? toType;
        if (IsImplicitNumericConversion(fromUnderlying ?? fromType, numericTarget))
        {
            return value =>
            {
                if (value is null)
                {
                    return default;
                }

                TryConvertNumeric(value, numericTarget, out var converted);
                return (TTo?)converted;
            };
        }

        return null;
    }

    private static object? InvokeOperator(MethodInfo op, object? value)
    {
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
        => (type.IsPrimitive || type == typeof(decimal)) && Type.GetTypeCode(type) is
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
            or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Char or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    /// <summary>The C# implicit numeric conversions (C# spec §10.2.3).</summary>
    private static bool IsImplicitNumericConversion(Type from, Type to)
    {
        if (!IsNumeric(from) || !IsNumeric(to))
        {
            return false;
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
            // IConvertible on char supports only integral targets, so route char through int.
            result = Convert.ChangeType(value is char c ? (int)c : value, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is OverflowException or InvalidCastException)
        {
            return false;
        }
    }

    /// <summary>
    /// Looks for <c>public static implicit operator <paramref name="toType"/>(<paramref name="fromType"/>)</c>
    /// declared on <paramref name="definedOn"/>. C# requires <paramref name="definedOn"/> to be either
    /// <paramref name="fromType"/> or <paramref name="toType"/>; we try both call sites.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "User-defined operators are looked up reflectively; warning is surfaced via RequiresUnreferencedCode on public entry points.")]
    private static MethodInfo? FindImplicitOperator(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type definedOn,
        Type fromType,
        Type toType)
    {
        foreach (var method in definedOn.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "op_Implicit" || method.ReturnType != toType)
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == fromType)
            {
                return method;
            }
        }

        return null;
    }
}

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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

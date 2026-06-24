// These cross-type overloads must lose to the source-generated same-type IsEqualTo / IsNotEqualTo
// when the expected argument is the same type as the source (e.g. Guid vs Guid), where both are
// applicable. Two layered mechanisms achieve that:
//
//  1. The trailing `params object[]` makes each overload applicable only in EXPANDED form, so a
//     same-type call binds to the normal-form same-type overload via the C# "normal beats
//     expanded" tie-break (§12.6.4.2). This is honored by EVERY Roslyn version, including the
//     .NET 8 SDK's compiler, which is the only mechanism that fixes #6296.
//  2. [OverloadResolutionPriority(-1)] is kept as a redundant signal for completeness on modern
//     compilers, but it is NOT load-bearing — it is honored only by Roslyn 4.12+ (.NET 9 SDK and
//     later), so consumers pinned to the .NET 8 SDK via global.json never see its effect.
//
// History: #5765 / #6276 / #6280 tried to fix this purely via ORP + a LangVersion bump in
// TUnit.Assertions.props. That bump is a no-op for the real failure because ORP honoring tracks
// the build SDK's Roslyn version, not LangVersion — see #6296. The `params` tie-break is what
// makes this compiler-version-independent. The unused `_` parameter is never read.
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Adds <c>IsEqualTo</c> overloads accepting an expected value of a different type when the
/// source type defines an implicit conversion operator to that type. Defined as a partial of
/// the source-generated <c>EqualsAssertionExtensions</c> so it shares the containing type. When
/// both this and the same-type overload are applicable (e.g. enum or value-type sources), the
/// trailing <c>params</c> array makes this overload lose to the same-type one on every compiler;
/// see the file header for why <see cref="OverloadResolutionPriorityAttribute"/> alone is not
/// sufficient (issue #6296).
///
/// Enables ergonomic comparison of wrapper Value Objects against their wrapped primitive,
/// e.g. <c>await Assert.That(productCode).IsEqualTo("Example")</c> when
/// <c>ProductCode</c> defines <c>public static implicit operator string(ProductCode pc)</c>.
/// </summary>
public static partial class EqualsAssertionExtensions
{
    /// <summary>
    /// Asserts equality between the source value and an expected value of a different type,
    /// provided that <typeparamref name="TValue"/> defines an implicit conversion operator
    /// to <typeparamref name="TOther"/>. The source value is converted via that operator
    /// and compared to <paramref name="expected"/> using <see cref="EqualsAssertion{T}"/>.
    /// </summary>
    [OverloadResolutionPriority(-1)]
    [RequiresUnreferencedCode("Looks up implicit conversion operators via reflection. Trimming may remove user-defined operators.")]
    public static EqualsAssertion<TOther> IsEqualTo<TValue, TOther>(
        this IAssertionSource<TValue> source,
        TOther? expected,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null,
        // Forces expanded-form applicability so a same-type call loses to the same-type
        // IsEqualTo<TValue> overload on every compiler. Never read. See file header.
        params object[] _)
    {
        var converter = ImplicitConversionCache.GetConverter<TValue, TOther>();
        source.Context.ExpressionBuilder.Append($".IsEqualTo({expectedExpression})");
        var mapped = source.Context.Map(converter);
        return new EqualsAssertion<TOther>(mapped, expected);
    }
}

/// <summary>
/// Adds <c>IsNotEqualTo</c> overloads accepting an unexpected value of a different type when
/// the source type defines an implicit conversion operator to that type. See remarks on
/// <see cref="EqualsAssertionExtensions"/> for why this is a partial extension class.
/// </summary>
public static partial class NotEqualsAssertionExtensions
{
    /// <summary>
    /// Asserts inequality between the source value and an unexpected value of a different
    /// type, provided that <typeparamref name="TValue"/> defines an implicit conversion
    /// operator to <typeparamref name="TOther"/>. The source value is converted via that
    /// operator and compared to <paramref name="notExpected"/> using
    /// <see cref="NotEqualsAssertion{T}"/>.
    /// </summary>
    [OverloadResolutionPriority(-1)]
    [RequiresUnreferencedCode("Looks up implicit conversion operators via reflection. Trimming may remove user-defined operators.")]
    public static NotEqualsAssertion<TOther> IsNotEqualTo<TValue, TOther>(
        this IAssertionSource<TValue> source,
        TOther? notExpected,
        [CallerArgumentExpression(nameof(notExpected))] string? notExpectedExpression = null,
        // Forces expanded-form applicability so a same-type call loses to the same-type
        // IsNotEqualTo<TValue> overload on every compiler. Never read. See file header.
        params object[] _)
    {
        var converter = ImplicitConversionCache.GetConverter<TValue, TOther>();
        source.Context.ExpressionBuilder.Append($".IsNotEqualTo({notExpectedExpression})");
        var mapped = source.Context.Map(converter);
        return new NotEqualsAssertion<TOther>(mapped, notExpected!);
    }
}

/// <summary>
/// Caches user-defined implicit conversion operators from <c>TFrom</c> to <c>TTo</c>.
/// Reflection lookup is performed once per type pair and the resulting delegate is reused.
/// </summary>
internal static class ImplicitConversionCache
{
    private static readonly ConcurrentDictionary<(Type From, Type To), Delegate> Cache = new();

    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    public static Func<TFrom?, TTo?> GetConverter<TFrom, TTo>()
    {
        var key = (typeof(TFrom), typeof(TTo));
        return (Func<TFrom?, TTo?>)Cache.GetOrAdd(key, static _ => BuildConverter<TFrom, TTo>());
    }

    [RequiresUnreferencedCode("Reflects over user-defined operators on TFrom and TTo.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "User-defined operators are public static methods on TFrom or TTo; if trimmed, IsEqualTo throws InvalidOperationException at call time.")]
    private static Func<TFrom?, TTo?> BuildConverter<TFrom, TTo>()
    {
        var fromType = typeof(TFrom);
        var toType = typeof(TTo);

        // C# allows `public static implicit operator TTo(TFrom)` to be declared on
        // either participant. Try TFrom first (the common Value Object pattern),
        // then fall back to TTo for cases where the conversion lives on the target.
        var op = FindImplicitOperator(fromType, fromType, toType)
                 ?? FindImplicitOperator(toType, fromType, toType);

        if (op is null)
        {
            return _ => throw new InvalidOperationException(
                $"No implicit conversion operator from '{fromType}' to '{toType}' was found. " +
                $"IsEqualTo / IsNotEqualTo with a different argument type requires '{fromType.Name}' " +
                $"to define 'public static implicit operator {toType.Name}({fromType.Name} value)'.");
        }

        // MethodInfo.Invoke boxes value-type arguments on every call. An
        // Expression.Lambda<Func<TFrom,TTo>>(...).Compile() avoids the box but
        // requires [RequiresDynamicCode] which breaks Native AOT compatibility,
        // so we accept the boxing here.
        return value =>
        {
            if (value is null)
            {
                return default;
            }
            return (TTo?)op.Invoke(null, [value]);
        };
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

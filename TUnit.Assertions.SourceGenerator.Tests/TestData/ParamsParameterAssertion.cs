using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: [GenerateAssertion] methods whose source signature contains a
/// <c>params</c> array. The generator must emit any auto-supplied
/// <c>[CallerArgumentExpression]</c> diagnostic parameter BEFORE the
/// <c>params</c> parameter; otherwise the generated extension method violates
/// CS0231 ("A params parameter must be the last parameter in a parameter list").
/// </summary>
public static partial class ParamsParameterAssertionExtensions
{
    // Canonical CS0231 bug shape: one required source parameter followed by params.
    [GenerateAssertion]
    public static bool ContainsAny(this string value, string label, params string[] candidates)
        => candidates.Length > 0 && value.Length >= label.Length;

    // Multiple required source parameters preceding params: each must contribute
    // its own [CallerArgumentExpression] in the diagnostic block before params.
    [GenerateAssertion]
    public static bool IsBetweenExcluding(this int value, int min, int max, params int[] excluded)
        => value >= min && value <= max && excluded.Length >= 0;

    // Optional defaulted source parameter before params. C# allows
    // (T x = default, params T[] y); the generator must preserve the default.
    [GenerateAssertion]
    public static bool MeetsLength(this string value, int minLength = 1, params string[] suffixes)
        => value.Length >= minLength && suffixes.Length >= 0;

    // Generic-typed required parameter alongside a generic-typed params array.
    [GenerateAssertion]
    public static bool IsOneOfWithDefault<T>(this T value, T fallback, params T[] alternatives)
        => alternatives.Length >= 0 || fallback != null;

    // InlineMethodBody must follow the same emit order on the inline path.
    [GenerateAssertion(InlineMethodBody = true)]
    public static bool StartsWithAny(this string value, string prefix, params string[] suffixes)
        => suffixes.Length >= 0 && value.StartsWith(prefix);

    // Regression: params-only (no preceding source parameter) must remain
    // byte-identical to the pre-fix shape used by IsIn/IsNotIn in production.
    [GenerateAssertion]
    public static bool ContainsExactly(this string value, params string[] required)
        => required.Length >= 0 && value.Length >= 0;
}

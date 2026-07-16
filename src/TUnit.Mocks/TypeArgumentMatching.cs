using System.Collections.Immutable;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks;

/// <summary>
/// Shared matching logic for a generic method's type arguments. Setups and verifications record the
/// type arguments they were configured with (or <see cref="AnyType"/>/<see cref="AnyValueType"/>
/// wildcards); recorded calls record the concrete closed types. A non-generic member carries a
/// default <see cref="ImmutableArray{T}"/> and always matches.
/// </summary>
internal static class TypeArgumentMatching
{
    /// <summary>True if <paramref name="type"/> is a recognized "match any type argument" wildcard.</summary>
    public static bool IsAnyMarker(Type type)
        => type == typeof(AnyType) || type == typeof(AnyValueType);

    /// <summary>
    /// Determines whether a setup configured with <paramref name="setupTypeArgs"/> matches a call
    /// made with <paramref name="callTypeArgs"/>. A default setup array (non-generic method,
    /// or generic method whose setup did not specify type args) matches any call. A default
    /// call array (the dispatch path did not supply type arguments — e.g. partial/wrap virtual
    /// methods) is not discriminated, so it matches any setup. Otherwise the arity must match and
    /// each slot must be equal or a wildcard marker.
    /// </summary>
    public static bool Matches(ImmutableArray<Type> setupTypeArgs, ImmutableArray<Type> callTypeArgs)
    {
        if (setupTypeArgs.IsDefault || callTypeArgs.IsDefault)
        {
            return true;
        }

        if (callTypeArgs.Length != setupTypeArgs.Length)
        {
            return false;
        }

        for (var i = 0; i < setupTypeArgs.Length; i++)
        {
            var slot = setupTypeArgs[i];
            if (!IsAnyMarker(slot) && slot != callTypeArgs[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Formats type arguments as <c>&lt;T1, T2&gt;</c> for failure messages; empty string for a
    /// non-generic member (default or empty array).
    /// </summary>
    public static string FormatForDiagnostics(ImmutableArray<Type> typeArguments)
        => typeArguments.IsDefaultOrEmpty
            ? ""
            : "<" + string.Join(", ", typeArguments.Select(FriendlyTypeName)) + ">";

    /// <summary>Short type name for diagnostics, without the CLR arity suffix (e.g. <c>List</c>, not <c>List`1</c>).</summary>
    private static string FriendlyTypeName(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`');
        return tick < 0 ? name : name.Substring(0, tick);
    }
}

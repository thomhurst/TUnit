using TUnit.Mocks.Arguments;

namespace TUnit.Mocks;

/// <summary>
/// Shared matching logic for a generic method's type arguments. Setups and verifications record the
/// type arguments they were configured with (or <see cref="AnyType"/>/<see cref="AnyValueType"/>
/// wildcards); recorded calls record the concrete closed types. A non-generic member carries
/// <see langword="null"/> and always matches.
/// </summary>
internal static class TypeArgumentMatching
{
    /// <summary>True if <paramref name="type"/> is a recognized "match any type argument" wildcard.</summary>
    public static bool IsAnyMarker(Type type)
        => type == typeof(AnyType) || type == typeof(AnyValueType);

    /// <summary>
    /// Determines whether a setup configured with <paramref name="setupTypeArgs"/> matches a call
    /// made with <paramref name="callTypeArgs"/>. A <see langword="null"/> setup (non-generic method,
    /// or generic method whose setup did not specify type args) matches any call. A <see langword="null"/>
    /// call (the dispatch path did not supply type arguments — e.g. partial/wrap virtual methods) is
    /// not discriminated, so it matches any setup. Otherwise the arity must match and each slot must
    /// be equal or a wildcard marker.
    /// </summary>
    public static bool Matches(Type[]? setupTypeArgs, Type[]? callTypeArgs)
    {
        if (setupTypeArgs is null || callTypeArgs is null)
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
}

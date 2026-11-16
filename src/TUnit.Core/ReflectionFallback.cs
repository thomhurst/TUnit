#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace TUnit.Core;

/// <summary>
/// Whether source-generated mode may reflect over a type the source generator emitted no metadata for.
/// On by default. A host without run-time member metadata turns it off with the
/// <c>TUnit.ReflectionFallback</c> AppContext switch (MSBuild: <c>TUnitReflectionFallback=false</c>),
/// after which only generated metadata is used. Reflection mode ignores the switch.
/// </summary>
internal static class ReflectionFallback
{
    private const string SwitchName = "TUnit.ReflectionFallback";

#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(SwitchName)]
#endif
    internal static bool IsSupported { get; } = !AppContext.TryGetSwitch(SwitchName, out var isEnabled) || isEnabled;
}

using System.Reflection;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<Assembly>( nameof(Assembly.IsDynamic))]
[CreateAssertion<Assembly>( nameof(Assembly.IsDynamic), CustomName = "IsNotDynamic", NegateLogic = true)]

[CreateAssertion<Assembly>( nameof(Assembly.IsFullyTrusted))]
[CreateAssertion<Assembly>( nameof(Assembly.IsFullyTrusted), CustomName = "IsNotFullyTrusted", NegateLogic = true)]

#if !NET
[CreateAssertion<Assembly>( nameof(Assembly.GlobalAssemblyCache))]
[CreateAssertion<Assembly>( nameof(Assembly.GlobalAssemblyCache), CustomName = "IsNotInGAC", NegateLogic = true)]
#endif

#if NET5_0_OR_GREATER
[CreateAssertion<Assembly>( nameof(Assembly.IsCollectible))]
[CreateAssertion<Assembly>( nameof(Assembly.IsCollectible), CustomName = "IsNotCollectible", NegateLogic = true)]
#endif

// Custom helper methods
[CreateAssertion<Assembly>( typeof(AssemblyAssertionExtensions), nameof(IsDebugBuild))]
[CreateAssertion<Assembly>( typeof(AssemblyAssertionExtensions), nameof(IsDebugBuild), CustomName = "IsReleaseBuild", NegateLogic = true)]

[CreateAssertion<Assembly>( typeof(AssemblyAssertionExtensions), nameof(IsSigned))]
[CreateAssertion<Assembly>( typeof(AssemblyAssertionExtensions), nameof(IsSigned), CustomName = "IsNotSigned", NegateLogic = true)]
public static partial class AssemblyAssertionExtensions
{
    internal static bool IsDebugBuild(Assembly assembly)
    {
        var debuggableAttribute = assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>();
        return debuggableAttribute?.IsJITTrackingEnabled ?? false;
    }
    
    internal static bool IsSigned(Assembly assembly)
    {
        return assembly.GetName().GetPublicKey()?.Length > 0;
    }
}
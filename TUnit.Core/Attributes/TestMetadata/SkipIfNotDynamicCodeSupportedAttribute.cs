using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class SkipIfNotDynamicCodeSupportedAttribute : SkipAttribute
{
    public SkipIfNotDynamicCodeSupportedAttribute(string reason) : base(reason)
    {
    }

    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
#if NET
        // Skip if dynamic code is NOT supported (i.e., in AOT mode)
        return Task.FromResult(!RuntimeFeature.IsDynamicCodeSupported);
#else
        // For older frameworks, assume dynamic code is supported
        return Task.FromResult(false);
#endif
    }
}
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
        return Task.FromResult(!RuntimeFeature.IsDynamicCodeSupported);
#else
        return Task.FromResult(false);
#endif
    }
}
using System.Runtime.CompilerServices;

namespace TUnit.TestProject;

public class DynamicCodeOnlyAttribute() : SkipAttribute("This test is only supported when dynamic code is available")
{
    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
#if NET
        return Task.FromResult(!RuntimeFeature.IsDynamicCodeSupported);
#else
        return Task.FromResult(false);
#endif
    }
}
using System.Runtime.InteropServices;

namespace TUnit.Engine.Tests.Attributes;

public class SkipNetFrameworkAttribute(string reason) : SkipAttribute(reason)
{
    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        return Task.FromResult(RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"));
    }
}
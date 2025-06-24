using System.Runtime.InteropServices;

namespace TUnit.TestProject.Attributes;

public class SkipNetFrameworkAttribute(string reason) : SkipAttribute(reason)
{
    public override Task<bool> ShouldSkip(TestContext context)
    {
        return Task.FromResult(RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"));
    }
}
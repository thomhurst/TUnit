using System.Runtime.InteropServices;

namespace TUnit.TestProject.Attributes;

public class SkipMacOSAttribute(string reason) : SkipAttribute(reason)
{
    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
    }
}
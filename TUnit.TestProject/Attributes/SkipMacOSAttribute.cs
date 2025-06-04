using System.Runtime.InteropServices;

namespace TUnit.TestProject.Attributes;

[Obsolete("Use `[ExcludeOnAttribute(OS.MacOS)]` instead.")]
public class SkipMacOSAttribute(string reason) : SkipAttribute(reason)
{
    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
    }
}
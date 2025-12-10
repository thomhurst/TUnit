using System.Runtime.InteropServices;
using TUnit.Core;

namespace TUnit.Engine.Tests;

public class SkipOnWindowsAttribute : SkipAttribute
{
    public SkipOnWindowsAttribute(string reason) : base(reason)
    {
    }

    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        return Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }
}

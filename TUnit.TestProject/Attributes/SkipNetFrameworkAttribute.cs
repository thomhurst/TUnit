using System.Runtime.InteropServices;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Attributes;

public class SkipNetFrameworkAttribute(string reason) : SkipAttribute(reason)
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        return Task.FromResult(RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"));
    }
}

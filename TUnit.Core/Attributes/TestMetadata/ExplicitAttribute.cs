using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ExplicitAttribute : TUnitAttribute
{
    public string For { get; }

    public ExplicitAttribute([CallerFilePath] string callerFile = "", [CallerMemberName] string callerMemberName = "")
    {
        For = $"{callerFile} {callerMemberName}".Trim();
    }
}
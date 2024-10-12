using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ExplicitAttribute(
    [CallerFilePath] string callerFile = "",
    [CallerMemberName] string callerMemberName = "")
    : TUnitAttribute
{
    public string For { get; } = $"{callerFile} {callerMemberName}".Trim();
}
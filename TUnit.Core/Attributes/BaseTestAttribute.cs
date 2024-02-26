using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public abstract class BaseTestAttribute(
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0)
    : TUnitAttribute
{
    public readonly string File = file;
    public readonly int Line = line;
}
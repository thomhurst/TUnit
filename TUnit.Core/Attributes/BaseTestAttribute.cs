namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public abstract class BaseTestAttribute : TUnitAttribute
{
    public readonly string File;
    public readonly int Line;

    protected BaseTestAttribute(string file, int line)
    {
        File = file;
        Line = line;
    }
}
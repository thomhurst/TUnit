namespace TUnit.Core;

// Any new test type attributes should inherit from this
// This ensures we have a location of the test provided by the compiler
// Using [CallerLineNumber] [CallerFilePath]
[AttributeUsage(AttributeTargets.Method)]
public abstract class BaseTestAttribute : TUnitAttribute
{
    public readonly string File;
    public readonly int Line;

    internal BaseTestAttribute(string file, int line)
    {
        File = file;
        Line = line;
    }
}

namespace TUnit.Core;

/// <summary>
/// Base class for test method attributes. Automatically captures the source file path and line number
/// where the test is defined, using compiler services.
/// </summary>
/// <remarks>
/// Inherit from this class to create custom test type attributes. The <see cref="File"/> and <see cref="Line"/>
/// properties are populated automatically by the compiler via <c>[CallerFilePath]</c> and <c>[CallerLineNumber]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public abstract class BaseTestAttribute : TUnitAttribute
{
    /// <summary>
    /// Gets the source file path where the test is defined.
    /// </summary>
    public readonly string File;

    /// <summary>
    /// Gets the line number in the source file where the test is defined.
    /// </summary>
    public readonly int Line;

    internal BaseTestAttribute(string file, int line)
    {
        File = file;
        Line = line;
    }
}

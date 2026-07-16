namespace TUnit.Core;

/// <summary>
/// Base class for hook attributes (<see cref="BeforeAttribute"/>, <see cref="AfterAttribute"/>,
/// <see cref="BeforeEveryAttribute"/>, <see cref="AfterEveryAttribute"/>).
/// </summary>
/// <remarks>
/// This class is not intended to be used directly. Use the derived attributes instead.
/// </remarks>
public class HookAttribute : TUnitAttribute
{
    /// <summary>
    /// Gets the scope at which this hook runs (Test, Class, Assembly, TestSession, or TestDiscovery).
    /// </summary>
    public HookType HookType { get; }

    /// <summary>
    /// Gets the source file path where this hook is defined.
    /// </summary>
    public string File { get; }

    /// <summary>
    /// Gets the line number in the source file where this hook is defined.
    /// </summary>
    public int Line { get; }

    internal HookAttribute(HookType hookType, string file, int line)
    {
        if (!Enum.IsDefined(typeof(HookType), hookType))
        {
            throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null);
        }

        HookType = hookType;
        File = file;
        Line = line;
    }

    /// <summary>
    /// Gets or sets the execution order of this hook relative to other hooks at the same scope.
    /// Lower values execute first. Default is 0.
    /// </summary>
    public int Order { get; init; }
}

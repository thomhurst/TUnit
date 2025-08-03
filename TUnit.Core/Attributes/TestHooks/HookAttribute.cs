namespace TUnit.Core;

public class HookAttribute : TUnitAttribute
{
    public HookType HookType { get; }
    public string File { get; }
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

    public int Order { get; init; }
}

namespace TUnit.Core;

public class HookAttribute : TUnitAttribute
{
    public HookType HookType
    {
        get;
    }

    internal HookAttribute(HookType hookType, string file, int line)
    {
        if (!Enum.IsDefined(typeof(HookType), hookType))
        {
            throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null);
        }
        
        HookType = hookType;
    }   
    
    public int Order { get; init; }
}
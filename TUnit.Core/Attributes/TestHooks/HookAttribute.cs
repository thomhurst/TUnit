namespace TUnit.Core;

public class HookAttribute : TUnitAttribute
{
    internal HookAttribute(HookType hookType)
    {
        if (!Enum.IsDefined(typeof(HookType), hookType))
        {
            throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null);
        }
    }   
    
    public int Order { get; init; }
}
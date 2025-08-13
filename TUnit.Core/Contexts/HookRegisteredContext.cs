using TUnit.Core.Hooks;

namespace TUnit.Core;

/// <summary>
/// Context for hook registration phase
/// </summary>
public class HookRegisteredContext
{
    public StaticHookMethod HookMethod { get; }
    public string HookName => HookMethod.Name;
    
    public HookRegisteredContext(StaticHookMethod hookMethod)
    {
        HookMethod = hookMethod;
    }
}
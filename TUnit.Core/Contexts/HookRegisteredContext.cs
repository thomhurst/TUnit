using TUnit.Core.Hooks;

namespace TUnit.Core;

/// <summary>
/// Context for hook registration phase
/// </summary>
public class HookRegisteredContext
{
    private readonly object _hookMethod;
    private readonly string _hookName;
    private TimeSpan? _timeout;

    public StaticHookMethod? StaticHookMethod => _hookMethod as StaticHookMethod;
    public InstanceHookMethod? InstanceHookMethod => _hookMethod as InstanceHookMethod;
    public string HookName => _hookName;
    
    /// <summary>
    /// Gets or sets the timeout for this hook
    /// </summary>
    public TimeSpan? Timeout
    {
        get => _timeout;
        set => _timeout = value;
    }
    
    public HookRegisteredContext(StaticHookMethod hookMethod)
    {
        _hookMethod = hookMethod;
        _hookName = hookMethod.Name;
    }
    
    public HookRegisteredContext(InstanceHookMethod hookMethod)
    {
        _hookMethod = hookMethod;
        _hookName = hookMethod.Name;
    }
}
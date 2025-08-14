using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for hook registration phase
/// </summary>
public class HookRegisteredContext
{
    private TimeSpan? _timeout;
    private IHookExecutor? _hookExecutor;

    public HookMethod HookMethod { get; }
    public string HookName => HookMethod.Name;
    
    /// <summary>
    /// Gets or sets the timeout for this hook
    /// </summary>
    public TimeSpan? Timeout
    {
        get => _timeout;
        set => _timeout = value;
    }
    
    /// <summary>
    /// Gets or sets the hook executor for this hook
    /// </summary>
    public IHookExecutor? HookExecutor
    {
        get => _hookExecutor;
        set => _hookExecutor = value;
    }
    
    public HookRegisteredContext(HookMethod hookMethod)
    {
        HookMethod = hookMethod;
    }
    
    /// <summary>
    /// Sets the hook executor for this hook
    /// </summary>
    public void SetHookExecutor(IHookExecutor hookExecutor)
    {
        _hookExecutor = hookExecutor;
    }
}
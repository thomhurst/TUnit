using TUnit.Core.Hooks;

namespace TUnit.Core;

/// <summary>
/// Context for hook registration phase
/// </summary>
public class HookRegisteredContext
{
    private TimeSpan? _timeout;

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
    /// Gets or sets the skip reason for this hook
    /// </summary>
    public string? SkipReason { get; set; }
    
    public HookRegisteredContext(HookMethod hookMethod)
    {
        HookMethod = hookMethod;
    }
}
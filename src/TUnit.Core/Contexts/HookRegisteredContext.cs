using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for hook registration phase
/// </summary>
public class HookRegisteredContext
{
    public HookMethod HookMethod { get; }
    public string HookName => HookMethod.Name;

    /// <summary>
    /// Gets or sets the timeout for this hook
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the hook executor that will be used to invoke this hook.
    /// Set by <see cref="IHookRegisteredEventReceiver"/> implementations (e.g. <c>CultureAttribute</c>,
    /// <c>STAThreadExecutorAttribute</c>) to wrap hook invocation in custom execution logic.
    /// If left <c>null</c>, the hook's default executor is used.
    /// </summary>
    public IHookExecutor? HookExecutor { get; set; }

    public HookRegisteredContext(HookMethod hookMethod)
    {
        HookMethod = hookMethod;
    }
}
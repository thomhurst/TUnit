using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record HookMethod
{
    public required MethodMetadata MethodInfo { get; init; }

    [field: AllowNull, MaybeNull]
    public string Name => field ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.Parameters.Select(x => x.Name))})";

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public abstract Type ClassType { get; }
    public virtual Assembly? Assembly => ClassType?.Assembly;

    [field: AllowNull, MaybeNull]
    public IEnumerable<Attribute> Attributes => field ??= MethodInfo.GetCustomAttributes();

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => Attributes.OfType<TAttribute>().FirstOrDefault();

    /// <summary>
    /// Gets the timeout for this hook method. This will be set during hook registration
    /// by the event receiver infrastructure, falling back to the default 5-minute timeout.
    /// </summary>
    public TimeSpan? Timeout { get; internal set; } = Defaults.HookTimeout;

    private IHookExecutor _hookExecutor = DefaultExecutor.Instance;

    public required IHookExecutor HookExecutor
    {
        get => _hookExecutor;
        init => _hookExecutor = value;
    }

    /// <summary>
    /// Overrides the hook executor after construction. Called by
    /// <c>EventReceiverOrchestrator</c> when an <see cref="Interfaces.IHookRegisteredEventReceiver"/>
    /// (e.g. <c>CultureAttribute</c>) sets a custom executor on the registration context.
    /// </summary>
    internal void SetHookExecutor(IHookExecutor executor) => _hookExecutor = executor;

    public required int Order { get; init; }
    
    public required int RegistrationIndex { get; init; }
}

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Settings;

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
    /// Gets the timeout for this hook method. When <c>null</c>, the engine falls back to
    /// <see cref="Settings.TUnitSettings.Timeouts"/>.<see cref="Settings.TimeoutSettings.DefaultHookTimeout"/>
    /// at execution time, so discovery-hook configuration is respected.
    /// Set explicitly by the <c>[Timeout]</c> attribute or event receiver infrastructure.
    /// </summary>
    public TimeSpan? Timeout { get; internal set; }

    private IHookExecutor _hookExecutor = DefaultExecutor.Instance;
    private bool _hookExecutorIsExplicit;

    public required IHookExecutor HookExecutor
    {
        get => _hookExecutor;
        init
        {
            _hookExecutor = value;
            // An init-time value other than DefaultExecutor means an explicit [HookExecutor<T>]
            // attribute was present at discovery time (source-gen or reflection).
            _hookExecutorIsExplicit = !ReferenceEquals(value, DefaultExecutor.Instance);
        }
    }

    internal void SetHookExecutor(IHookExecutor executor) => _hookExecutor = executor;

    // Explicit [HookExecutor<T>] on the hook method itself always wins.
    // Otherwise, prefer the per-test CustomHookExecutor (set via OnTestRegistered with
    // ScopedAttributeFilter, so it reflects the most-specific scoped attribute for this
    // test — e.g. method-level [Culture] over class-level). Fall back to _hookExecutor
    // which may have been set via OnHookRegistered from class/assembly-level attributes.
    internal IHookExecutor ResolveEffectiveExecutor(TestContext? testContext) =>
        _hookExecutorIsExplicit
            ? _hookExecutor
            : testContext?.CustomHookExecutor is { } custom
                ? custom
                : _hookExecutor;

    public required int Order { get; init; }
    
    public required int RegistrationIndex { get; init; }
}

using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Builds executable hook delegates from Sources collections.
/// Responsible for converting hook metadata into Func delegates ready for execution.
/// </summary>
internal interface IHookDelegateBuilder
{
    /// <summary>
    /// Eagerly initializes all global hook delegates at startup.
    /// </summary>
    ValueTask InitializeAsync();

    ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeTestHooksAsync(Type testClassType);
    ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterTestHooksAsync(Type testClassType);
    ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeEveryTestHooksAsync(Type testClassType);
    ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterEveryTestHooksAsync(Type testClassType);

    /// <summary>
    /// Synchronous fast-path accessors: return <c>true</c> when the per-type Before/After(Test) hook
    /// list is already materialized. Used by <see cref="HookExecutor"/> to skip async state-machine
    /// allocation on the steady-state cache-hit path. The BeforeEvery/AfterEvery lists are always
    /// available synchronously via the existing ValueTask accessors (populated during initialization).
    /// </summary>
    bool TryGetCachedBeforeTestHooks(Type testClassType, out IReadOnlyList<NamedHookDelegate<TestContext>> hooks);
    bool TryGetCachedAfterTestHooks(Type testClassType, out IReadOnlyList<NamedHookDelegate<TestContext>> hooks);
    IReadOnlyList<NamedHookDelegate<TestContext>> GetCachedBeforeEveryTestHooks();
    IReadOnlyList<NamedHookDelegate<TestContext>> GetCachedAfterEveryTestHooks();

    ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeClassHooksAsync(Type testClassType);
    ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterClassHooksAsync(Type testClassType);
    ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeEveryClassHooksAsync();
    ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterEveryClassHooksAsync();

    ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeAssemblyHooksAsync(Assembly assembly);
    ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterAssemblyHooksAsync(Assembly assembly);
    ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeEveryAssemblyHooksAsync();
    ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterEveryAssemblyHooksAsync();

    ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> CollectBeforeTestSessionHooksAsync();
    ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> CollectAfterTestSessionHooksAsync();

    ValueTask<IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>> CollectBeforeTestDiscoveryHooksAsync();
    ValueTask<IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>> CollectAfterTestDiscoveryHooksAsync();
}

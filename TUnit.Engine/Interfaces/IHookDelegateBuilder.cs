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

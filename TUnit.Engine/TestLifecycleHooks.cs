using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Lifecycle hooks ready for execution
/// </summary>
public sealed class TestLifecycleHooks
{
    /// <summary>
    /// Hooks to run before test class instantiation
    /// </summary>
    public required Func<HookContext, Task>[] BeforeClass { get; init; }

    /// <summary>
    /// Hooks to run after test class instantiation
    /// </summary>
    public required Func<object, HookContext, Task>[] AfterClass { get; init; }

    /// <summary>
    /// Hooks to run before test execution
    /// </summary>
    public required Func<object, HookContext, Task>[] BeforeTest { get; init; }

    /// <summary>
    /// Hooks to run after test execution
    /// </summary>
    public required Func<object, HookContext, Task>[] AfterTest { get; init; }
}

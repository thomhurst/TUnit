using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Marks a method as a global teardown hook that runs after every test, class, assembly, or test session,
/// regardless of which class declares it.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="AfterAttribute"/> which is scoped to the declaring class,
/// <c>[AfterEvery]</c> applies globally. For example, <c>[AfterEvery(HookType.Test)]</c> runs after
/// every test in the entire test suite, not just tests in the declaring class.
/// </para>
/// <para>
/// The method must be <c>static</c> and declared in a class. It will be invoked for all tests matching the specified scope.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GlobalHooks
/// {
///     [AfterEvery(HookType.Test)]
///     public static void AfterEachTest(TestContext context)
///     {
///         // Runs after every test in the entire suite
///     }
///
///     [AfterEvery(HookType.Class)]
///     public static void AfterEachClass(ClassHookContext context)
///     {
///         // Runs after every test class
///     }
/// }
/// </code>
/// </example>
/// <param name="hookType">The scope at which this hook runs.</param>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterEveryAttribute(HookType hookType, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : HookAttribute(hookType, file, line);

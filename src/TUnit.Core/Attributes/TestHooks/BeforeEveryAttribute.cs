using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Marks a method as a global setup hook that runs before every test, class, assembly, or test session,
/// regardless of which class declares it.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="BeforeAttribute"/> which is scoped to the declaring class,
/// <c>[BeforeEvery]</c> applies globally. For example, <c>[BeforeEvery(HookType.Test)]</c> runs before
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
///     [BeforeEvery(HookType.Test)]
///     public static void BeforeEachTest(TestContext context)
///     {
///         // Runs before every test in the entire suite
///     }
///
///     [BeforeEvery(HookType.Class)]
///     public static void BeforeEachClass(ClassHookContext context)
///     {
///         // Runs before every test class
///     }
/// }
/// </code>
/// </example>
/// <param name="hookType">The scope at which this hook runs.</param>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeEveryAttribute(HookType hookType, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : HookAttribute(hookType, file, line);

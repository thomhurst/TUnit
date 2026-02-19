using System.Runtime.CompilerServices;

#pragma warning disable CS9113

namespace TUnit.Core;

/// <summary>
/// Marks a method as a teardown hook that runs after a specific scope (test, class, assembly, or test session).
/// The hook is scoped to the class that declares it.
/// </summary>
/// <remarks>
/// <para>
/// Use <c>[After(HookType.Test)]</c> to run the method after each test in the declaring class.
/// Use <c>[After(HookType.Class)]</c> to run the method once after all tests in the class complete.
/// Use <c>[After(HookType.Assembly)]</c> to run the method once after all tests in the assembly complete.
/// Use <c>[After(HookType.TestSession)]</c> to run the method once per test session teardown.
/// </para>
/// <para>
/// For hooks that apply globally to every test regardless of class, use <see cref="AfterEveryAttribute"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyTests
/// {
///     [After(HookType.Test)]
///     public void CleanUp()
///     {
///         // Runs after each test in this class
///     }
///
///     [After(HookType.Class)]
///     public static void ClassCleanUp()
///     {
///         // Runs once after all tests in this class complete
///     }
/// }
/// </code>
/// </example>
/// <param name="hookType">The scope at which this hook runs.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterAttribute(HookType hookType, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : HookAttribute(hookType, file, line);

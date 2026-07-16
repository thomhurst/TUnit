using System.Runtime.CompilerServices;

#pragma warning disable CS9113

namespace TUnit.Core;

/// <summary>
/// Marks a method as a setup hook that runs before a specific scope (test, class, assembly, or test session).
/// The hook is scoped to the class that declares it.
/// </summary>
/// <remarks>
/// <para>
/// Use <c>[Before(HookType.Test)]</c> to run the method before each test in the declaring class.
/// Use <c>[Before(HookType.Class)]</c> to run the method once before all tests in the class.
/// Use <c>[Before(HookType.Assembly)]</c> to run the method once before all tests in the assembly.
/// Use <c>[Before(HookType.TestSession)]</c> to run the method once per test session.
/// </para>
/// <para>
/// For hooks that apply globally to every test regardless of class, use <see cref="BeforeEveryAttribute"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyTests
/// {
///     [Before(HookType.Test)]
///     public void SetUp()
///     {
///         // Runs before each test in this class
///     }
///
///     [Before(HookType.Class)]
///     public static void ClassSetUp()
///     {
///         // Runs once before all tests in this class
///     }
/// }
/// </code>
/// </example>
/// <param name="hookType">The scope at which this hook runs.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeAttribute(HookType hookType, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : HookAttribute(hookType, file, line);

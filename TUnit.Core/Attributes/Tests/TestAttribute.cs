using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Marks a method as a test method in the TUnit testing framework.
/// </summary>
/// <remarks>
/// Methods marked with this attribute will be discovered and executed as tests during test runs.
/// The attribute automatically captures the file path and line number where the test is defined.
/// </remarks>
/// <example>
/// <code>
/// public class ExampleTests
/// {
///     [Test]
///     public void ExampleTest()
///     {
///         // Test code here
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute(
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0)
    : BaseTestAttribute(file, line);

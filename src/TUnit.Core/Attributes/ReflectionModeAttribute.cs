namespace TUnit.Core;

/// <summary>
/// Attribute that forces the test assembly to use reflection mode for test discovery and execution.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when source generation cannot be used for test discovery, such as when
/// working with dynamically generated types (e.g., Razor components in bUnit tests).
/// </para>
///
/// <para>
/// This attribute should be applied at the assembly level and affects all tests in the assembly.
/// Command-line options (--reflection) can still override this setting.
/// </para>
///
/// <para>
/// <strong>Performance Note:</strong> Reflection mode is slower than source-generated mode.
/// Only use this attribute when source generation is incompatible with your test scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Add to your test project (e.g., in AssemblyInfo.cs or at the top of any .cs file)
/// using TUnit.Core;
///
/// [assembly: ReflectionMode]
///
/// // All tests in this assembly will now use reflection mode
/// public class MyBunitTests
/// {
///     [Test]
///     public void TestRazorComponent()
///     {
///         // Test Razor components that are source-generated at compile time
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class ReflectionModeAttribute : Attribute
{
}

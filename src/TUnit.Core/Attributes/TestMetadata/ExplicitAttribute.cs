using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Marks a test method or class to only run when explicitly selected.
/// Tests marked with this attribute will not run as part of normal test execution
/// and must be targeted directly by name or filter.
/// </summary>
/// <remarks>
/// This is useful for long-running tests, tests that require specific environments,
/// or tests that should only be run manually by a developer.
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [Explicit]
/// public void LongRunningPerformanceTest()
/// {
///     // Only runs when explicitly selected
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ExplicitAttribute(
    [CallerFilePath] string callerFile = "",
    [CallerMemberName] string callerMemberName = "")
    : TUnitAttribute
{
    /// <summary>
    /// Gets a string identifying where this attribute was applied (file path and member name).
    /// </summary>
    public string For { get; } = $"{callerFile} {callerMemberName}".Trim();
}

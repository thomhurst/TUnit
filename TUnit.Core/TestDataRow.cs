namespace TUnit.Core;

/// <summary>
/// Internal interface for accessing TestDataRow properties without reflection.
/// This enables AOT compatibility by avoiding dynamic property access.
/// </summary>
internal interface ITestDataRow
{
    object? GetData();
    string? DisplayName { get; }
    string? Skip { get; }
    string[]? Categories { get; }
}

/// <summary>
/// Wraps test data with optional metadata for customizing test execution.
/// Use this when returning data from method/class data sources to specify
/// per-row display names, skip reasons, or categories.
/// </summary>
/// <typeparam name="T">The type of the test data.</typeparam>
/// <param name="Data">The actual test data to be passed to the test method.</param>
/// <param name="DisplayName">
/// Optional custom display name for the test case.
/// Supports parameter substitution using $paramName or $arg1, $arg2, etc.
/// </param>
/// <param name="Skip">
/// Optional skip reason. When set, the test case will be skipped with this message.
/// </param>
/// <param name="Categories">
/// Optional categories to apply to this specific test case.
/// </param>
/// <example>
/// <code>
/// public static IEnumerable&lt;TestDataRow&lt;(string Username, string Password)&gt;&gt; GetLoginData()
/// {
///     yield return new(("admin", "secret123"), DisplayName: "Admin login");
///     yield return new(("guest", "guest"), DisplayName: "Guest login");
///     yield return new(("", ""), DisplayName: "Empty credentials", Skip: "Not implemented yet");
/// }
/// </code>
/// </example>
public record TestDataRow<T>(
    T Data,
    string? DisplayName = null,
    string? Skip = null,
    string[]? Categories = null
) : ITestDataRow
{
    object? ITestDataRow.GetData() => Data;
}

using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Internal interface for accessing TestDataRow properties without reflection.
/// This enables AOT compatibility by avoiding dynamic property access.
/// </summary>
internal interface ITestDataRow
{
    object? GetData();
    string? DisplayName { get; }
    string? DataExpression { get; }
    string? Skip { get; }
    string[]? Categories { get; }
}

/// <summary>
/// Wraps test data with optional metadata for customizing test execution.
/// Use this when returning data from method/class data sources to specify
/// per-row display names, skip reasons, or categories.
/// </summary>
/// <typeparam name="T">The type of the test data.</typeparam>
/// <example>
/// <code>
/// public static IEnumerable&lt;TestDataRow&lt;(string Username, string Password)&gt;&gt; GetLoginData()
/// {
///     yield return new(("admin", "secret123")); // DisplayName includes method name + expression
///     yield return new(("guest", "guest"), DisplayName: "Guest login"); // DisplayName fully overridden
///     yield return new(("", ""), DisplayName: "Empty credentials", Skip: "Not implemented yet");
/// }
/// </code>
/// </example>
public record TestDataRow<T> : ITestDataRow
{
    /// <summary>
    /// The actual test data to be passed to the test method.
    /// </summary>
    public T Data { get; init; }

    /// <summary>
    /// Optional custom display name for the test case. When set, replaces the entire display name.
    /// Supports parameter substitution using $paramName or $arg1, $arg2, etc.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Auto-captured expression text of the Data argument via CallerArgumentExpression.
    /// Used as the argument representation in the default TestName(expression) format
    /// when DisplayName is not explicitly set.
    /// </summary>
    public string? DataExpression { get; init; }

    /// <summary>
    /// Optional skip reason. When set, the test case will be skipped with this message.
    /// </summary>
    public string? Skip { get; init; }

    /// <summary>
    /// Optional categories to apply to this specific test case.
    /// </summary>
    public string[]? Categories { get; init; }

    /// <summary>
    /// Creates a new TestDataRow wrapping the specified data.
    /// </summary>
    /// <param name="Data">The test data.</param>
    /// <param name="DisplayName">Optional custom display name. When set, replaces the entire display name.</param>
    /// <param name="Skip">Optional skip reason.</param>
    /// <param name="Categories">Optional categories.</param>
    /// <param name="DataExpression">Auto-captured expression text. Leave unset to let the compiler fill it in.</param>
    public TestDataRow(
        T Data,
        string? DisplayName = null,
        string? Skip = null,
        string[]? Categories = null,
        [CallerArgumentExpression(nameof(Data))] string? DataExpression = null)
    {
        this.Data = Data;
        this.DisplayName = DisplayName;
        this.Skip = Skip;
        this.Categories = Categories;
        this.DataExpression = DataExpression;
    }

    object? ITestDataRow.GetData() => Data;
}

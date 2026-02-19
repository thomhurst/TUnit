using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Provides a way to supply inline data for parameterized tests.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ArgumentsAttribute</c> allows you to specify test data directly within your test definition,
/// rather than having to create a separate data source class.
/// </para>
/// <para>
/// Each attribute instance represents a single test case that will be executed.
/// </para>
/// <para>
/// Multiple <c>ArgumentsAttribute</c> instances can be applied to a single test method to create
/// multiple test cases with different input values.
/// </para>
/// <example>
/// <code>
/// [Test]
/// [Arguments(1, 2, 3)]
/// [Arguments(10, 20, 30)]
/// public void TestAddition(int a, int b, int expected)
/// {
///     Assert.That(a + b).IsEqualTo(expected);
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ArgumentsAttribute : Attribute, IDataSourceAttribute, ITestRegisteredEventReceiver
{
    /// <summary>
    /// Gets the array of argument values to pass to the test method.
    /// </summary>
    public object?[] Values { get; }

    /// <summary>
    /// Gets or sets a reason to skip this specific test case.
    /// When set, the test case will be skipped with the given reason.
    /// </summary>
    public string? Skip { get; set; }

    /// <summary>
    /// Gets or sets a custom display name for this test case.
    /// Supports parameter substitution using $paramName or $arg1, $arg2, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// [Arguments("admin", "secret", DisplayName = "Login as $arg1")]
    /// </code>
    /// </example>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets categories to apply to this specific test case.
    /// </summary>
    /// <example>
    /// <code>
    /// [Arguments("value", Categories = ["smoke", "integration"])]
    /// </code>
    /// </example>
    public string[]? Categories { get; set; }

    /// <inheritdoc />
    public bool SkipIfEmpty { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentsAttribute"/> class with the specified test argument values.
    /// </summary>
    /// <param name="values">The argument values to pass to the test method. Pass <c>null</c> for a single null argument.</param>
    public ArgumentsAttribute(params object?[]? values)
    {
        if (values == null)
        {
            Values = [null];
        }
        else if (values.Length == 0)
        {
            Values = [];
        }
        else
        {
            Values = values;
        }
    }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>(Values);
        await Task.CompletedTask;
    }

public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (!string.IsNullOrEmpty(Skip))
        {
            context.TestContext.SkipReason = Skip;
            context.TestContext.Metadata.TestDetails.ClassInstance = SkippedTestInstance.Instance;
        }

        if (!string.IsNullOrEmpty(DisplayName))
        {
            context.TestContext.SetDataSourceDisplayName(DisplayName!);
        }

        if (Categories is { Length: > 0 })
        {
            foreach (var category in Categories)
            {
                if (!string.IsNullOrWhiteSpace(category) && !context.TestDetails.Categories.Contains(category))
                {
                    context.TestDetails.Categories.Add(category);
                }
            }
        }

        return default;
    }

    public int Order => 0;
}

/// <summary>
/// Provides a strongly-typed inline value for a parameterized test with a single parameter.
/// </summary>
/// <typeparam name="T">The type of the test parameter.</typeparam>
/// <param name="value">The value to pass to the test method.</param>
/// <example>
/// <code>
/// [Test]
/// [Arguments&lt;string&gt;("hello")]
/// [Arguments&lt;string&gt;("world")]
/// public void TestWithString(string input) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ArgumentsAttribute<T>(T value) : TypedDataSourceAttribute<T>, ITestRegisteredEventReceiver
{
    /// <summary>
    /// Gets or sets a reason to skip this specific test case.
    /// When set, the test case will be skipped with the given reason.
    /// </summary>
    public string? Skip { get; set; }

    /// <summary>
    /// Gets or sets a custom display name for this test case.
    /// Supports parameter substitution using $paramName or $arg1, $arg2, etc.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets categories to apply to this specific test case.
    /// </summary>
    public string[]? Categories { get; set; }

    /// <inheritdoc />
    public override bool SkipIfEmpty { get; set; }

    public override async IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult(value);
        await default(ValueTask);
    }

public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (!string.IsNullOrEmpty(Skip))
        {
            context.TestContext.SkipReason = Skip;
            context.TestContext.Metadata.TestDetails.ClassInstance = SkippedTestInstance.Instance;
        }

        if (!string.IsNullOrEmpty(DisplayName))
        {
            context.TestContext.SetDataSourceDisplayName(DisplayName!);
        }

        if (Categories is { Length: > 0 })
        {
            foreach (var category in Categories)
            {
                if (!string.IsNullOrWhiteSpace(category) && !context.TestDetails.Categories.Contains(category))
                {
                    context.TestDetails.Categories.Add(category);
                }
            }
        }

        return default;
    }

    public int Order => 0;
}

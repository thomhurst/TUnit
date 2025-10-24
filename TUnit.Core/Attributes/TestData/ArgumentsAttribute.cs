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
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ArgumentsAttribute : Attribute, IDataSourceAttribute, ITestRegisteredEventReceiver
{
    public object?[] Values { get; }

    public string? Skip { get; set; }

    public ArgumentsAttribute(params object?[]? values)
    {
        if (values == null || values.Length == 0)
        {
            Values = [null];
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
            context.TestContext.TestDetails.ClassInstance = SkippedTestInstance.Instance;
        }

        return default;
    }

    public int Order => 0;
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ArgumentsAttribute<T>(T value) : TypedDataSourceAttribute<T>, ITestRegisteredEventReceiver
{
    public string? Skip { get; set; }

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
            context.TestContext.TestDetails.ClassInstance = SkippedTestInstance.Instance;
        }

        return default;
    }

    public int Order => 0;
}

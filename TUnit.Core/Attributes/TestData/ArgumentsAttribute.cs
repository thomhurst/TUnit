using TUnit.Core.Interfaces;

namespace TUnit.Core;

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
        if(!string.IsNullOrEmpty(Skip))
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
        if(!string.IsNullOrEmpty(Skip))
        {
            context.TestContext.SkipReason = Skip;
            context.TestContext.TestDetails.ClassInstance = SkippedTestInstance.Instance;
        }

        return default;
    }

    public int Order => 0;
}

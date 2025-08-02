namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ArgumentsAttribute : TestDataAttribute
{
    public object?[] Values { get; }

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

    public override async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Task.FromResult<object?[]?>(Values);
        await Task.CompletedTask;
    }
}

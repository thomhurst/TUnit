using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : TypedDataSourceAttribute<T>
{
    protected abstract IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public sealed override IAsyncEnumerable<Func<Task<T>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2> : TypedDataSourceAttribute<(T1, T2)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public sealed override IAsyncEnumerable<Func<Task<(T1, T2)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T3> : TypedDataSourceAttribute<(T1, T2, T3)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public sealed override IAsyncEnumerable<Func<Task<(T1, T2, T3)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T4> : TypedDataSourceAttribute<(T1, T2, T3, T4)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override IAsyncEnumerable<Func<Task<(T1, T2, T3, T4)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class AsyncDataSourceGeneratorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T4,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    T5> : TypedDataSourceAttribute<(T1, T2, T3, T4, T5)>
{
    protected abstract IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata);

    public override IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GenerateDataSourcesAsync(dataGeneratorMetadata);
    }
}

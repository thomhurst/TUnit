using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

// Example typed async data source
public class IntDataSource : AsyncDataSourceGeneratorAttribute<int>
{
    protected override async IAsyncEnumerable<Func<Task<int>>> GenerateDataSourcesAsync(DataGeneratorMetadata metadata)
    {
        yield return () => Task.FromResult(1);
        yield return () => Task.FromResult(2);
        yield return () => Task.FromResult(3);
        await Task.CompletedTask;
    }
}

// Example typed data source
public class StringDataSource : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "hello";
        yield return () => "world";
    }
}

// Example multi-parameter typed data source
public class TupleDataSource : AsyncDataSourceGeneratorAttribute<int, string>
{
    protected override async IAsyncEnumerable<Func<Task<(int, string)>>> GenerateDataSourcesAsync(DataGeneratorMetadata metadata)
    {
        yield return () => Task.FromResult((1, "one"));
        yield return () => Task.FromResult((2, "two"));
        yield return () => Task.FromResult((3, "three"));
        await Task.CompletedTask;
    }
}

// Test generic method with typed data source
public class GenericTypedDataSourceTests
{
    [Test]
    [IntDataSource]
    public async Task GenericMethodWithIntData<T>(T value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value).IsTypeOf<int>();
    }
    
    [Test]
    [StringDataSource]
    public async Task GenericMethodWithStringData<T>(T value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value).IsTypeOf<string>();
    }
    
    [Test]
    [TupleDataSource]
    public async Task GenericMethodWithTupleData<T1, T2>(T1 num, T2 text)
    {
        await Assert.That(num).IsNotNull();
        await Assert.That(num).IsTypeOf<int>();
        await Assert.That(text).IsNotNull();
        await Assert.That(text).IsTypeOf<string>();
    }
}

// Test generic class with typed data source  
public class GenericClassTypedDataSourceTests<T>
{
    [Test]
    [IntDataSource]
    public async Task TestWithIntData(T value)
    {
        await Assert.That(value).IsNotNull();
        // T should be inferred as int from IntDataSource
    }
}
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Example typed async data source for testing
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
public class GenericTupleDataSource : AsyncDataSourceGeneratorAttribute<int, string>
{
    protected override async IAsyncEnumerable<Func<Task<(int, string)>>> GenerateDataSourcesAsync(DataGeneratorMetadata metadata)
    {
        yield return () => Task.FromResult((1, "one"));
        yield return () => Task.FromResult((2, "two"));
        yield return () => Task.FromResult((3, "three"));
        await Task.CompletedTask;
    }
}

/// <summary>
/// Tests for generic methods with typed data sources
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericTypedDataSourceTests
{
    [Test]
    [IntDataSource]
    public async Task GenericMethodWithIntData<T>(T value)
    {
        // T should be inferred as int from IntDataSource
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Task.CompletedTask;
    }
    
    [Test]
    [StringDataSource]
    public async Task GenericMethodWithStringData<T>(T value)
    {
        // T should be inferred as string from StringDataSource
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Task.CompletedTask;
    }
    
    [Test]
    [GenericTupleDataSource]
    public async Task GenericMethodWithTupleData<T1, T2>(T1 num, T2 text)
    {
        // T1 should be int, T2 should be string from GenericTupleDataSource
        await Assert.That(typeof(T1)).IsEqualTo(typeof(int));
        await Assert.That(typeof(T2)).IsEqualTo(typeof(string));
        await Task.CompletedTask;
    }
}

/// <summary>
/// Tests for generic classes with typed data sources
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericClassTypedDataSourceTests<T>
{
    [Test]
    [IntDataSource]
    public async Task TestWithIntData(T value)
    {
        // T should be inferred as int from IntDataSource
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Task.CompletedTask;
    }
}
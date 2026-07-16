using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

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

public class StringDataSource : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "hello";
        yield return () => "world";
    }
}

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

[EngineTest(ExpectedResult.Pass)]
public class GenericTypedDataSourceTests
{
    [Test]
    [IntDataSource]
    public async Task GenericMethodWithIntData<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Task.CompletedTask;
    }
    
    [Test]
    [StringDataSource]
    public async Task GenericMethodWithStringData<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Task.CompletedTask;
    }
    
    [Test]
    [GenericTupleDataSource]
    public async Task GenericMethodWithTupleData<T1, T2>(T1 num, T2 text)
    {
        await Assert.That(typeof(T1)).IsEqualTo(typeof(int));
        await Assert.That(typeof(T2)).IsEqualTo(typeof(string));
        await Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericClassTypedDataSourceTests<T>
{
    [Test]
    [IntDataSource]
    public async Task TestWithIntData(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Task.CompletedTask;
    }
}
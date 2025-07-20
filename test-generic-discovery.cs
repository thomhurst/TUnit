using System;
using TUnit.Core;

namespace TestGenericDiscovery;

// Simple generic test to verify source generation
public class SimpleGenericTests
{
    [Test]
    [Arguments(42)]
    public void GenericTest<T>(T value)
    {
        Console.WriteLine($"Generic test with value: {value} of type {typeof(T)}");
    }
    
    [Test]
    [Arguments("hello")]
    public void GenericTestWithConstraint<T>(T value) where T : class
    {
        Console.WriteLine($"Generic test with constraint, value: {value} of type {typeof(T)}");
    }
}

// Custom data source that provides a specific type
public class IntDataSource : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => 100;
    }
}

public class GenericTestsWithDataSource
{
    [Test]
    [IntDataSource]
    public void GenericTestWithTypedDataSource<T>(T value)
    {
        Console.WriteLine($"Generic test with typed data source: {value} of type {typeof(T)}");
    }
}
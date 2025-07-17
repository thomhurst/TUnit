using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Tests for InstanceMethodDataSource with generic type inference
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericInstanceMethodDataSourceTests
{
    // Test with typed InstanceMethodDataSource<T> for generic type inference
    [Test]
    [InstanceMethodDataSource<int>(nameof(GetIntData))]
    [GenerateGenericTest(typeof(int))]
    public async Task GenericMethodWithTypedInstanceMethodDataSource<T>(T value)
    {
        // T should be inferred as int from InstanceMethodDataSource<int>
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Task.CompletedTask;
    }
    
    // Test with multiple typed InstanceMethodDataSource for different types
    [Test]
    [InstanceMethodDataSource<string>(nameof(GetStringData))]
    [GenerateGenericTest(typeof(string))]
    public async Task GenericMethodWithStringInstanceMethodDataSource<T>(T value)
    {
        // T should be inferred as string from InstanceMethodDataSource<string>
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Task.CompletedTask;
    }
    
    // Instance method that would provide int data
    public IEnumerable<int> GetIntData()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
    
    // Instance method that would provide string data
    public IEnumerable<string> GetStringData()
    {
        yield return "hello";
        yield return "world";
        yield return "test";
    }
}

/// <summary>
/// Tests for generic classes with InstanceMethodDataSource
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class GenericClassInstanceMethodDataSourceTests<T> where T : class
{
    [Test]
    [InstanceMethodDataSource<string>(nameof(GetStringData))]
    public async Task TestWithInstanceMethodDataSource(T value)
    {
        // T should be inferred as string from InstanceMethodDataSource<string>
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Task.CompletedTask;
    }
    
    public IEnumerable<string> GetStringData()
    {
        yield return "instance";
        yield return "method";
        yield return "data";
    }
}
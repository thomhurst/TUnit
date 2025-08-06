using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.AOT.Tests;

/// <summary>
/// Simple AOT compatibility tests focused on compilation validation
/// </summary>
public class SimpleAotTests
{
    [Test]
    public void BasicTest_ShouldCompileAndRun()
    {
        // Basic test to verify AOT compilation succeeds
        var value = 42;
        Console.WriteLine($"Test value: {value}");
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public void ParameterizedTest_ShouldWork(int value)
    {
        // Test parameterized tests work in AOT
        Console.WriteLine($"Parameterized test with value: {value}");
    }

    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public void MethodDataSourceTest_ShouldWork(string data)
    {
        // Test method data sources work in AOT (using source-generated invocation)
        Console.WriteLine($"Method data source test with: {data}");
    }

    public static IEnumerable<string> GetTestData()
    {
        yield return "test1";
        yield return "test2";
        yield return "test3";
    }

    [Test]
    [ClassDataSource<SimpleDataClass>]
    public void ClassDataSourceTest_ShouldWork(SimpleDataClass data)
    {
        // Test class data sources work in AOT (using source-generated factories)
        Console.WriteLine($"Class data source test with: {data}");
    }

    public class SimpleDataClass : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "data1" };
            yield return new object[] { "data2" };
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    [Test]
    public async Task AsyncTest_ShouldWork()
    {
        // Test async tests work in AOT
        await Task.Delay(10);
        Console.WriteLine("Async test completed");
    }

    [Test]
    [MatrixDataSource]
    public void MatrixTest_ShouldWork(
        [Matrix(1, 2, 3)] int number, 
        [Matrix("a", "b")] string letter)
    {
        // Test matrix data sources work in AOT
        Console.WriteLine($"Matrix test: {number}, {letter}");
    }

    [Test]
    public void GenericTest_ShouldWork()
    {
        // Test generic type handling works in AOT
        var list = new List<string> { "test" };
        Console.WriteLine($"Generic test - list count: {list.Count}");
    }

    [Test]
    public void TupleTest_ShouldWork()
    {
        // Test tuple processing works in AOT
        var tuple = (Name: "Test", Value: 42);
        Console.WriteLine($"Tuple test: {tuple.Name} = {tuple.Value}");
    }

    [Test]
    public void ObjectTest_ShouldWork()
    {
        // Test object creation and property access
        var obj = new TestObject { Name = "Test", Value = 42 };
        Console.WriteLine($"Object test: {obj.Name} = {obj.Value}");
    }

    public class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}

/// <summary>
/// AOT compatibility tests for nested property injection
/// </summary>
public class NestedPropertyInjectionAotTests
{
    [CustomDataSource<CustomService>]
    public required CustomService? Service { get; set; }

    [Test]
    public async Task NestedPropertyInjection_ShouldWorkInAot()
    {
        // Test that nested property injection works correctly in AOT
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service!.IsInitialized).IsTrue();
        await Assert.That(Service.GetMessage()).IsEqualTo("Custom service initialized");

        // Test nested service
        await Assert.That(Service.NestedService).IsNotNull();
        await Assert.That(Service.NestedService!.IsInitialized).IsTrue();
        await Assert.That(Service.NestedService.GetData()).IsEqualTo("Nested service initialized");

        // Test deeply nested service
        await Assert.That(Service.NestedService.DeeplyNestedService).IsNotNull();
        await Assert.That(Service.NestedService.DeeplyNestedService!.IsInitialized).IsTrue();
        await Assert.That(Service.NestedService.DeeplyNestedService.GetDeepData()).IsEqualTo("Deeply nested service initialized");
    }
}

// Custom data source attribute for AOT testing
public class CustomDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : AsyncDataSourceGeneratorAttribute<T>
{
    protected override async IAsyncEnumerable<Func<Task<T>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            // Simple creation - framework should handle init properties and nested injection
            return Task.FromResult((T)Activator.CreateInstance(typeof(T))!);
        };
        await Task.CompletedTask;
    }
}

public class CustomService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    // Nested property with its own data source
    [CustomDataSource<NestedService>]
    public required NestedService? NestedService { get; set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetMessage()
    {
        return IsInitialized ? "Custom service initialized" : "Not initialized";
    }
}

public class NestedService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    // Deeply nested property with its own data source
    [CustomDataSource<DeeplyNestedService>]
    public required DeeplyNestedService? DeeplyNestedService { get; set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetData()
    {
        return IsInitialized ? "Nested service initialized" : "Nested not initialized";
    }
}

public class DeeplyNestedService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(1);
        IsInitialized = true;
    }

    public string GetDeepData()
    {
        return IsInitialized ? "Deeply nested service initialized" : "Deeply nested not initialized";
    }
}

/// <summary>
/// Hook tests for AOT
/// </summary>
public class SimpleHookTests
{
    private static bool _setupExecuted;

    [Before(HookType.Class)]
    public static async Task Setup()
    {
        await Task.Delay(1);
        _setupExecuted = true;
        Console.WriteLine("Setup hook executed");
    }

    [Test]
    public void HookTest_ShouldWork()
    {
        Console.WriteLine($"Setup was executed: {_setupExecuted}");
    }
}

/// <summary>
/// Assembly-level hook test
/// </summary>
public static class SimpleGlobalSetup
{
    [Before(HookType.Assembly)]
    public static async Task GlobalSetup()
    {
        await Task.Delay(1);
        Console.WriteLine("Global setup executed in AOT mode");
    }
}
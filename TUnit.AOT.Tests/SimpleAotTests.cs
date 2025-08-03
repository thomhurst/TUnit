using TUnit.Core;
using TUnit.Assertions;

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
    public void ClassDataSourceTest_ShouldWork(string data)
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
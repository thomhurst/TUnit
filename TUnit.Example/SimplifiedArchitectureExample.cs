using System;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Example;

/// <summary>
/// Example test class demonstrating the simplified architecture
/// </summary>
[TestClass]
public class SimplifiedArchitectureExample
{
    private string _testData;

    [Before(Test)]
    public void Setup()
    {
        _testData = "Initialized";
        Console.WriteLine("Setup executed");
    }

    [Test]
    public void SimpleTest()
    {
        Assert.That(_testData).IsEqualTo("Initialized");
        Console.WriteLine("Simple test executed");
    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(5, 10, 15)]
    public void ParameterizedTest(int a, int b, int expected)
    {
        var result = a + b;
        Assert.That(result).IsEqualTo(expected);
        Console.WriteLine($"Parameterized test: {a} + {b} = {result}");
    }

    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task DataDrivenTest(string input, string expected)
    {
        await Task.Delay(10); // Simulate async work
        var result = input.ToUpper();
        Assert.That(result).IsEqualTo(expected);
    }

    public static IEnumerable<(string, string)> GetTestData()
    {
        yield return ("hello", "HELLO");
        yield return ("world", "WORLD");
        yield return ("tunit", "TUNIT");
    }

    [Test]
    [Skip("Demonstrating skip functionality")]
    public void SkippedTest()
    {
        // This test will be skipped
    }

    [Test]
    [Timeout(1000)]
    public async Task TestWithTimeout()
    {
        await Task.Delay(100);
        Assert.That(true).IsTrue();
    }

    [After(Test)]
    public void Cleanup()
    {
        Console.WriteLine("Cleanup executed");
    }
}

/// <summary>
/// Example showing test dependencies
/// </summary>
[TestClass]
public class DependencyExample
{
    [Test]
    public void FirstTest()
    {
        Console.WriteLine("First test must run before second");
    }

    [Test]
    [DependsOn(typeof(DependencyExample), nameof(FirstTest))]
    public void SecondTest()
    {
        Console.WriteLine("Second test runs after first");
    }
}

/// <summary>
/// Example showing parallel execution control
/// </summary>
[TestClass]
[NotInParallel]
public class SerialExecutionExample
{
    private static int _counter;

    [Test]
    public void SerialTest1()
    {
        var value = ++_counter;
        Console.WriteLine($"Serial test 1: counter = {value}");
        Assert.That(value).IsEqualTo(1);
    }

    [Test]
    public void SerialTest2()
    {
        var value = ++_counter;
        Console.WriteLine($"Serial test 2: counter = {value}");
        Assert.That(value).IsEqualTo(2);
    }
}
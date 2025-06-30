using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Assertions;

namespace TUnit.Examples;

/// <summary>
/// Examples demonstrating reflection-free test patterns in TUnit
/// </summary>
public class ReflectionFreeExamples
{
    /// <summary>
    /// Basic test - no reflection needed, delegates are pre-compiled
    /// </summary>
    [Test]
    public async Task BasicAsyncTest()
    {
        await Task.Delay(10);
        await Assert.That(1 + 1).IsEqualTo(2);
    }

    /// <summary>
    /// Synchronous test with compile-time delegate generation
    /// </summary>
    [Test]
    public void SynchronousTest()
    {
        var result = PerformCalculation(5, 3);
        Assert.That(result).IsEqualTo(8);
    }

    /// <summary>
    /// Parameterized test with static data - fully AOT compatible
    /// </summary>
    [Test]
    [Arguments(1, 1, 2)]
    [Arguments(2, 3, 5)]
    [Arguments(5, 8, 13)]
    public void ParameterizedTest(int a, int b, int expected)
    {
        var result = a + b;
        Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    /// Test with compile-time resolved data source
    /// </summary>
    [Test]
    [MethodDataSource(typeof(TestDataProviders), nameof(TestDataProviders.GetMathTestCases))]
    public async Task DataDrivenTest(int input, int expected)
    {
        var result = input * 2;
        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    /// Test with async data source - AOT friendly with pre-compiled factory
    /// </summary>
    [Test]
    [MethodDataSource(typeof(TestDataProviders), nameof(TestDataProviders.GetAsyncTestData))]
    public async Task AsyncDataSourceTest(string input, bool expectedValid)
    {
        var isValid = IsValidInput(input);
        await Assert.That(isValid).IsEqualTo(expectedValid);
    }

    /// <summary>
    /// Test with property data source - no reflection at runtime
    /// </summary>
    [Test]
    [PropertyDataSource(typeof(TestDataProviders), nameof(TestDataProviders.StaticTestData))]
    public void PropertyDataSourceTest(TestCase testCase)
    {
        var result = ProcessTestCase(testCase);
        Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Test with complex argument types - fully type-safe at compile time
    /// </summary>
    [Test]
    [Arguments(new[] { 1, 2, 3 }, 6)]
    [Arguments(new[] { 5, 10, 15 }, 30)]
    public void ComplexArgumentTest(int[] numbers, int expectedSum)
    {
        var sum = 0;
        foreach (var num in numbers)
        {
            sum += num;
        }
        Assert.That(sum).IsEqualTo(expectedSum);
    }

    /// <summary>
    /// Test with class-level data source for multiple test methods
    /// </summary>
    [ClassDataSource<UserTestData>]
    public class UserTests(string username, int userId)
    {
        [Test]
        public void ValidateUsername()
        {
            Assert.That(username).IsNotNull()
                .And.IsNotEmpty()
                .And.HasLengthGreaterThan(3);
        }

        [Test]
        public async Task ValidateUserId()
        {
            await Assert.That(userId).IsGreaterThan(0);
        }
    }

    /// <summary>
    /// Test with custom timeout - no reflection needed for attribute processing
    /// </summary>
    [Test]
    [Timeout(5000)]
    public async Task TimeoutTest()
    {
        await Task.Delay(100);
        await Assert.That(true).IsTrue();
    }

    /// <summary>
    /// Test with categories for filtering - compile-time metadata
    /// </summary>
    [Test]
    [Category("Unit")]
    [Category("Fast")]
    public void CategorizedTest()
    {
        var result = QuickCalculation();
        Assert.That(result).IsGreaterThan(0);
    }

    /// <summary>
    /// Test with retry logic - handled through pre-compiled metadata
    /// </summary>
    [Test]
    [Retry(3)]
    public void RetryableTest()
    {
        var random = new Random();
        var value = random.Next(0, 10);
        
        // This might fail sometimes, but retry logic will handle it
        Assert.That(value).IsGreaterThan(5);
    }

    /// <summary>
    /// Test with dependencies - resolved at compile time
    /// </summary>
    [Test]
    [DependsOn(nameof(BasicAsyncTest))]
    public async Task DependentTest()
    {
        // This test runs after BasicAsyncTest completes
        await Task.Delay(10);
        await Assert.That(GetDependentValue()).IsEqualTo(42);
    }

    #region Helper Methods

    private int PerformCalculation(int a, int b) => a + b;
    
    private bool IsValidInput(string input) => !string.IsNullOrWhiteSpace(input);
    
    private object ProcessTestCase(TestCase testCase) => new { testCase.Name, testCase.Value };
    
    private int QuickCalculation() => 42;
    
    private int GetDependentValue() => 42;

    #endregion
}

/// <summary>
/// Data providers for reflection-free tests
/// All methods are resolved at compile time and stored as factories
/// </summary>
public static class TestDataProviders
{
    /// <summary>
    /// Static data source method - pre-compiled into factory
    /// </summary>
    public static IEnumerable<object[]> GetMathTestCases()
    {
        yield return new object[] { 1, 2 };
        yield return new object[] { 5, 10 };
        yield return new object[] { 10, 20 };
    }

    /// <summary>
    /// Async data source - AOT compatible through factory pattern
    /// </summary>
    public static async Task<IEnumerable<object[]>> GetAsyncTestData()
    {
        await Task.Delay(1); // Simulate async operation
        
        return new[]
        {
            new object[] { "valid", true },
            new object[] { "", false },
            new object[] { "test", true },
            new object[] { null!, false }
        };
    }

    /// <summary>
    /// Property data source - accessed without reflection at runtime
    /// </summary>
    public static IEnumerable<TestCase> StaticTestData { get; } = new[]
    {
        new TestCase { Name = "Test1", Value = 100 },
        new TestCase { Name = "Test2", Value = 200 },
        new TestCase { Name = "Test3", Value = 300 }
    };
}

/// <summary>
/// Test data class for property data source
/// </summary>
public class TestCase
{
    public required string Name { get; init; }
    public required int Value { get; init; }
}

/// <summary>
/// Class data source implementation
/// </summary>
public class UserTestData : IClassDataSource
{
    public IEnumerable<object[]> GetData()
    {
        yield return new object[] { "alice", 1 };
        yield return new object[] { "bob", 2 };
        yield return new object[] { "charlie", 3 };
    }
}

/// <summary>
/// Example showing generic test class handling in AOT mode
/// Generic parameters must be resolved at compile time
/// </summary>
public class GenericTestExample<T> where T : IComparable<T>
{
    private readonly T _value;

    public GenericTestExample(T value)
    {
        _value = value;
    }

    [Test]
    public void GenericTest()
    {
        Assert.That(_value).IsNotNull();
    }
}

/// <summary>
/// Concrete instantiations for AOT compilation
/// </summary>
[InheritsTests(typeof(GenericTestExample<int>))]
public class IntGenericTests : GenericTestExample<int>
{
    public IntGenericTests() : base(42) { }
}

[InheritsTests(typeof(GenericTestExample<string>))]
public class StringGenericTests : GenericTestExample<string>
{
    public StringGenericTests() : base("test") { }
}
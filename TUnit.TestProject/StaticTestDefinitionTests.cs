using TUnit.Core;

namespace TUnit.TestProject;

public class StaticTestDefinitionTests
{
    // Test 1: Simple MethodDataSource returning single values
    [Test]
    [MethodDataSource(nameof(GetSimpleData))]
    public void TestWithSimpleMethodDataSource(int value)
    {
        Console.WriteLine($"Testing with value: {value}");
        Assert.That(value).IsGreaterThan(0);
    }

    public static IEnumerable<int> GetSimpleData()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }

    // Test 2: MethodDataSource returning tuples
    [Test]
    [MethodDataSource(nameof(GetTupleData))]
    public void TestWithTupleMethodDataSource(int a, string b)
    {
        Console.WriteLine($"Testing with a={a}, b={b}");
        Assert.That(a).IsGreaterThan(0);
        Assert.That(b).IsNotNull();
    }

    public static IEnumerable<(int, string)> GetTupleData()
    {
        yield return (1, "one");
        yield return (2, "two");
        yield return (3, "three");
    }

    // Test 3: MethodDataSource returning object arrays
    [Test]
    [MethodDataSource(nameof(GetObjectArrayData))]
    public void TestWithObjectArrayMethodDataSource(int value, string text, bool flag)
    {
        Console.WriteLine($"Testing with value={value}, text={text}, flag={flag}");
        Assert.That(value).IsPositive();
        Assert.That(text).IsNotEmpty();
    }

    public static IEnumerable<object[]> GetObjectArrayData()
    {
        yield return new object[] { 10, "test1", true };
        yield return new object[] { 20, "test2", false };
        yield return new object[] { 30, "test3", true };
    }

    // Test 4: MethodDataSource on property with simple values
    [Test]
    [MethodDataSource(nameof(GetPropertyData))]
    public void TestWithPropertyMethodDataSource(double value)
    {
        Console.WriteLine($"Testing with value: {value}");
        Assert.That(value).IsGreaterThan(0);
    }

    public static IEnumerable<double> GetPropertyData()
    {
        return new[] { 1.5, 2.5, 3.5 };
    }

    // Test 5: Instance method data source (non-static)
    [Test]
    [MethodDataSource(nameof(GetInstanceData))]
    public void TestWithInstanceMethodDataSource(string value)
    {
        Console.WriteLine($"Testing with value: {value}");
        Assert.That(value).Contains("instance");
    }

    public IEnumerable<string> GetInstanceData()
    {
        yield return "instance1";
        yield return "instance2";
        yield return "instance3";
    }

    // Test 6: MethodDataSource on property
    public class TestWithPropertyInjection
    {
        [MethodDataSource(nameof(GetPropertyValues))]
        public required int Value { get; set; }

        public static IEnumerable<int> GetPropertyValues()
        {
            return new[] { 100, 200, 300 };
        }

        [Test]
        public void TestPropertyInjectedValue()
        {
            Console.WriteLine($"Testing with injected value: {Value}");
            Assert.That(Value).IsGreaterThan(50);
        }
    }

    // Test 7: Multiple properties with data sources
    public class TestWithMultiplePropertyInjection
    {
        [MethodDataSource(nameof(GetNameValues))]
        public required string Name { get; set; }

        [MethodDataSource(nameof(GetAgeValues))]
        public required int Age { get; set; }

        public static IEnumerable<string> GetNameValues()
        {
            return new[] { "Alice", "Bob" };
        }
        
        public static IEnumerable<int> GetAgeValues()
        {
            return new[] { 25, 30 };
        }

        [Test]
        public void TestMultiplePropertyInjection()
        {
            Console.WriteLine($"Testing with Name={Name}, Age={Age}");
            Assert.That(Name).IsNotEmpty();
            Assert.That(Age).IsGreaterThan(0);
        }
    }

    // Test 8: MethodDataSource at class level for constructor parameters (should use DynamicTestMetadata)
    [MethodDataSource(nameof(GetConstructorData))]
    public class TestWithConstructorDataSource
    {
        private readonly int _value;

        public TestWithConstructorDataSource(int value)
        {
            _value = value;
        }

        public static IEnumerable<int> GetConstructorData()
        {
            yield return 5;
            yield return 10;
            yield return 15;
        }

        [Test]
        public void TestConstructorInjectedValue()
        {
            Console.WriteLine($"Testing with constructor value: {_value}");
            Assert.That(_value).IsGreaterThanOrEqualTo(5);
        }
    }

    // Test 9: ArgumentsAttribute (should use StaticTestDefinition)
    [Test]
    [Arguments(1, "test")]
    [Arguments(2, "another")]
    public void TestWithArguments(int number, string text)
    {
        Console.WriteLine($"Testing with number={number}, text={text}");
        Assert.That(number).IsPositive();
        Assert.That(text).IsNotEmpty();
    }

    // Test 10: Mixed - ArgumentsAttribute with MethodDataSource (should use StaticTestDefinition)
    [Test]
    [Arguments(99)]
    [MethodDataSource(nameof(GetAdditionalNumbers))]
    public void TestWithMixedDataSources(int number)
    {
        Console.WriteLine($"Testing with number={number}");
        Assert.That(number).IsPositive();
    }

    public static IEnumerable<int> GetAdditionalNumbers()
    {
        yield return 42;
        yield return 84;
    }
}
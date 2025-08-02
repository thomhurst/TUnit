// using TUnit.Core;
// using System.Threading.Tasks;
//
// namespace TUnit.TestProject;
//
// public class StaticTestDefinitionTests
// {
//     // Test 1: Simple MethodDataSource returning single values
//     [Test]
//     [MethodDataSource(nameof(GetSimpleData))]
//     public async Task TestWithSimpleMethodDataSource(int value)
//     {
//         Console.WriteLine($"Testing with value: {value}");
//         await Assert.That(value).IsGreaterThan(0);
//     }
//
//     public static IEnumerable<int> GetSimpleData()
//     {
//         yield return 1;
//         yield return 2;
//         yield return 3;
//     }
//
//     // Test 2: MethodDataSource returning tuples
//     [Test]
//     [MethodDataSource(nameof(GetTupleData))]
//     public async Task TestWithTupleMethodDataSource(int a, string b)
//     {
//         Console.WriteLine($"Testing with a={a}, b={b}");
//         await Assert.That(a).IsGreaterThan(0);
//         await Assert.That(b).IsNotNull();
//     }
//
//     public static IEnumerable<(int, string)> GetTupleData()
//     {
//         yield return (1, "one");
//         yield return (2, "two");
//         yield return (3, "three");
//     }
//
//     // Test 3: MethodDataSource returning object arrays
//     [Test]
//     [MethodDataSource(nameof(GetObjectArrayData))]
//     public async Task TestWithObjectArrayMethodDataSource(int value, string text, bool flag)
//     {
//         Console.WriteLine($"Testing with value={value}, text={text}, flag={flag}");
//         await Assert.That(value).IsPositive();
//         await Assert.That(text).IsNotEmpty();
//     }
//
//     public static IEnumerable<object[]> GetObjectArrayData()
//     {
//         yield return new object[] { 10, "test1", true };
//         yield return new object[] { 20, "test2", false };
//         yield return new object[] { 30, "test3", true };
//     }
//
//     // Test 4: MethodDataSource on property with simple values
//     [Test]
//     [MethodDataSource(nameof(GetPropertyData))]
//     public async Task TestWithPropertyMethodDataSource(double value)
//     {
//         Console.WriteLine($"Testing with value: {value}");
//         await Assert.That(value).IsGreaterThan(0);
//     }
//
//     public static IEnumerable<double> GetPropertyData()
//     {
//         return new[] { 1.5, 2.5, 3.5 };
//     }
//
//     // Test 5: Instance method data source (non-static)
//     [Test]
//     [MethodDataSource(nameof(GetInstanceData))]
//     public async Task TestWithInstanceMethodDataSource(string value)
//     {
//         Console.WriteLine($"Testing with value: {value}");
//         await Assert.That(value).Contains("instance");
//     }
//
//     public IEnumerable<string> GetInstanceData()
//     {
//         yield return "instance1";
//         yield return "instance2";
//         yield return "instance3";
//     }
//
//     // Test 6: MethodDataSource on property
//     public class TestWithPropertyInjection
//     {
//         [MethodDataSource(nameof(GetPropertyValues))]
//         public required int Value { get; set; }
//
//         public static IEnumerable<int> GetPropertyValues()
//         {
//             return new[] { 100, 200, 300 };
//         }
//
//         [Test]
//         public async Task TestPropertyInjectedValue()
//         {
//             Console.WriteLine($"Testing with injected value: {Value}");
//             await Assert.That(Value).IsGreaterThan(50);
//         }
//     }
//
//     // Test 7: Multiple properties with data sources
//     public class TestWithMultiplePropertyInjection
//     {
//         [MethodDataSource(nameof(GetNameValues))]
//         public required string Name { get; set; }
//
//         [MethodDataSource(nameof(GetAgeValues))]
//         public required int Age { get; set; }
//
//         public static IEnumerable<string> GetNameValues()
//         {
//             return new[] { "Alice", "Bob" };
//         }
//
//         public static IEnumerable<int> GetAgeValues()
//         {
//             return new[] { 25, 30 };
//         }
//
//         [Test]
//         public async Task TestMultiplePropertyInjection()
//         {
//             Console.WriteLine($"Testing with Name={Name}, Age={Age}");
//             await Assert.That(Name).IsNotEmpty();
//             await Assert.That(Age).IsGreaterThan(0);
//         }
//     }
//
//     // Test 8: MethodDataSource at class level for constructor parameters (should use DynamicTestMetadata)
//     [MethodDataSource(nameof(GetConstructorData))]
//     public class TestWithConstructorDataSource
//     {
//         private readonly int _value;
//
//         public TestWithConstructorDataSource(int value)
//         {
//             _value = value;
//         }
//
//         public static IEnumerable<int> GetConstructorData()
//         {
//             yield return 5;
//             yield return 10;
//             yield return 15;
//         }
//
//         [Test]
//         public async Task TestConstructorInjectedValue()
//         {
//             Console.WriteLine($"Testing with constructor value: {_value}");
//             await Assert.That(_value).IsGreaterThanOrEqualTo(5);
//         }
//     }
//
//     // Test 9: ArgumentsAttribute (should use StaticTestDefinition)
//     [Test]
//     [Arguments(1, "test")]
//     [Arguments(2, "another")]
//     public async Task TestWithArguments(int number, string text)
//     {
//         Console.WriteLine($"Testing with number={number}, text={text}");
//         await Assert.That(number).IsPositive();
//         await Assert.That(text).IsNotEmpty();
//     }
//
//     // Test 10: Mixed - ArgumentsAttribute with MethodDataSource (should use StaticTestDefinition)
//     [Test]
//     [Arguments(99)]
//     [MethodDataSource(nameof(GetAdditionalNumbers))]
//     public async Task TestWithMixedDataSources(int number)
//     {
//         Console.WriteLine($"Testing with number={number}");
//         await Assert.That(number).IsPositive();
//     }
//
//     public static IEnumerable<int> GetAdditionalNumbers()
//     {
//         yield return 42;
//         yield return 84;
//     }
// }

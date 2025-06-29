// using TUnit.Core;
//
// namespace TUnit.TestProject;
//
// public class StaticTestDefinitionDemo
// {
//     // Test 1: Simple test without data sources (should use StaticTestDefinition)
//     [Test]
//     public async Task SimpleStaticTest()
//     {
//         Console.WriteLine("Running simple static test");
//         await Assert.That(true).IsTrue();
//     }
//
//     // Test 2: Test with Arguments attribute (should use StaticTestDefinition)
//     [Test]
//     [Arguments(1, "one")]
//     [Arguments(2, "two")]
//     [Arguments(3, "three")]
//     public async Task TestWithArguments(int number, string text)
//     {
//         Console.WriteLine($"Running test with number={number}, text={text}");
//         await Assert.That(number).IsGreaterThan(0);
//         await Assert.That(text).IsNotNull();
//     }
//
//     // Test 3: Test with MethodDataSource returning single values (should use StaticTestDefinition)
//     [Test]
//     [MethodDataSource(nameof(GetNumbers))]
//     public async Task TestWithMethodDataSourceSingleValue(int value)
//     {
//         Console.WriteLine($"Running test with value={value} from MethodDataSource");
//         await Assert.That(value).IsGreaterThan(0);
//     }
//
//     public static IEnumerable<int> GetNumbers()
//     {
//         yield return 10;
//         yield return 20;
//         yield return 30;
//     }
//
//     // Test 4: Test with MethodDataSource returning tuples (should use StaticTestDefinition)
//     [Test]
//     [MethodDataSource(nameof(GetPairs))]
//     public async Task TestWithMethodDataSourceTuples(int id, string name)
//     {
//         Console.WriteLine($"Running test with id={id}, name={name} from MethodDataSource");
//         await Assert.That(id).IsGreaterThan(0);
//         await Assert.That(name).IsNotEmpty();
//     }
//
//     public static IEnumerable<(int, string)> GetPairs()
//     {
//         yield return (1, "Alice");
//         yield return (2, "Bob");
//         yield return (3, "Charlie");
//     }
//
//     // Test 5: Test with MethodDataSource returning object arrays (should use StaticTestDefinition)
//     [Test]
//     [MethodDataSource(nameof(GetComplexData))]
//     public async Task TestWithMethodDataSourceObjectArrays(int id, string name, bool active)
//     {
//         Console.WriteLine($"Running test with id={id}, name={name}, active={active} from MethodDataSource");
//         await Assert.That(id).IsGreaterThan(0);
//         await Assert.That(name).IsNotEmpty();
//     }
//
//     public static IEnumerable<object[]> GetComplexData()
//     {
//         yield return new object[] { 1, "User1", true };
//         yield return new object[] { 2, "User2", false };
//         yield return new object[] { 3, "User3", true };
//     }
//
//     // Test 6: Test with instance method data source (should use StaticTestDefinition)
//     [Test]
//     [MethodDataSource(nameof(GetInstanceData))]
//     public async Task TestWithInstanceMethodDataSource(string value)
//     {
//         Console.WriteLine($"Running test with value={value} from instance MethodDataSource");
//         await Assert.That(value).Contains("data");
//     }
//
//     public IEnumerable<string> GetInstanceData()
//     {
//         yield return "data1";
//         yield return "data2";
//         yield return "data3";
//     }
// }

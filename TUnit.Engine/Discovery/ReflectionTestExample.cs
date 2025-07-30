#if DEBUG
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building;

namespace TUnit.Engine.Discovery
{

/// <summary>
/// Example demonstrating ReflectionTestDataCollector usage
/// This file is for documentation/testing purposes only
/// </summary>
[RequiresUnreferencedCode("Example requires reflection")]
[RequiresDynamicCode("Example requires dynamic code")]
internal static class ReflectionTestExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("=== ReflectionTestDataCollector Example ===");

        // Create a reflection-based test data collector
        var collector = TestDataCollectorFactory.Create(TestExecutionMode.Reflection);

        // Collect all tests using reflection
        var tests = await collector.CollectTestsAsync("123");

        foreach (var test in tests.Take(5)) // Show first 5 tests
        {
            Console.WriteLine($"  - {test.TestClassType.Name}.{test.TestMethodName}");
            Console.WriteLine($"    Class: {test.TestClassType.Name}");
            Console.WriteLine($"    Method: {test.TestMethodName}");

            if (test.IsSkipped)
            {
                Console.WriteLine($"    Skipped: {test.SkipReason}");
            }
            if (test.TimeoutMs.HasValue)
            {
                Console.WriteLine($"    Timeout: {test.TimeoutMs}ms");
            }
            if (test.DataSources.Length > 0)
            {
                Console.WriteLine($"    Data Sources: {test.DataSources.Length}");
            }
        }
    }
}

// Example test classes that would be discovered by reflection
namespace TUnit.Engine.Discovery.Examples
{
    public class SimpleTests
    {
        [Test]
        public void SimpleTest()
        {
            Console.WriteLine("Simple test executed");
        }

        [Test]
        [Category("Integration")]
        [Timeout(5000)]
        public async Task AsyncTestWithAttributes()
        {
            await Task.Delay(100);
            Console.WriteLine("Async test with attributes executed");
        }

        [Test]
        [Arguments(1, 2, 3)]
        [Arguments(4, 5, 6)]
        public void ParameterizedTest(int a, int b, int c)
        {
            Console.WriteLine($"Parameterized test: {a}, {b}, {c}");
        }

        [Test]
        [Skip("Example of skipped test")]
        public void SkippedTest()
        {
            // This won't execute
        }
    }

    public class TestsWithHooks
    {
        [Before(HookType.Class)]
        public static void BeforeClass()
        {
            Console.WriteLine("Before class hook");
        }

        [Before(HookType.Test)]
        public void BeforeEachTest()
        {
            Console.WriteLine("Before test hook");
        }

        [Test]
        public void TestWithHooks()
        {
            Console.WriteLine("Test with hooks executed");
        }

        [After(HookType.Test)]
        public void AfterEachTest()
        {
            Console.WriteLine("After test hook");
        }
    }

    public class GenericTests<T>
    {
        [Test]
        public void GenericTest()
        {
            Console.WriteLine($"Generic test for type: {typeof(T).Name}");
        }
    }
}
}
#endif

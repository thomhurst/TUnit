using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[Arguments(2)]
[ClassDataSource(typeof(DataSource1))]
[ClassDataSource(typeof(DataSource2))]
[ClassDataSource(typeof(DataSource3))]
public class DiagnosticAllDataSourcesCombinedTests
{
    private readonly int classValue;
    private static readonly ConcurrentBag<string> ExecutedTests = [];
    private static readonly ConcurrentBag<string> CreatedInstances = [];
    
    public DiagnosticAllDataSourcesCombinedTests(int value)
    {
        classValue = value;
        CreatedInstances.Add($"Instance created with class value: {value}");
        Console.WriteLine($"[DIAGNOSTIC] Instance created with value: {value}");
    }

    static DiagnosticAllDataSourcesCombinedTests()
    {
        Console.WriteLine("[DIAGNOSTIC] Static constructor called for DiagnosticAllDataSourcesCombinedTests");
    }

    [Test]
    [Arguments("A")]
    [Arguments("B")]
    [MethodDataSource(nameof(GetTestData))]
    public async Task DiagnosticTestWithAllDataSources(string methodValue)
    {
        var testId = $"Class:{classValue},Method:{methodValue}";
        ExecutedTests.Add(testId);
        Console.WriteLine($"[DIAGNOSTIC] Test executed: {testId}");
        await Task.CompletedTask;
    }

    public static IEnumerable<string> GetTestData()
    {
        Console.WriteLine("[DIAGNOSTIC] GetTestData called");
        yield return "X";
        yield return "Y";
    }

    [After(Assembly)]
    public static async Task DiagnosticVerifyExpectedCombinations()
    {
        var executedTests = ExecutedTests.ToList();
        var createdInstances = CreatedInstances.ToList();
        
        Console.WriteLine($"[DIAGNOSTIC] Total instances created: {createdInstances.Count}");
        foreach (var instance in createdInstances.OrderBy(x => x))
        {
            Console.WriteLine($"[DIAGNOSTIC] {instance}");
        }
        
        Console.WriteLine($"[DIAGNOSTIC] Total tests executed: {executedTests.Count}");
        foreach (var test in executedTests.OrderBy(x => x))
        {
            Console.WriteLine($"[DIAGNOSTIC] {test}");
        }
        
        // Skip verification if no tests were executed (e.g., filtered run)
        if (executedTests.Count == 0)
        {
            return;
        }

        // Expected combinations
        var classValues = new[] { 1, 2, 10, 20, 30 };
        var methodValues = new[] { "A", "B", "X", "Y" };

        Console.WriteLine("[DIAGNOSTIC] Expected combinations:");
        foreach (var classVal in classValues)
        {
            foreach (var methodVal in methodValues)
            {
                var expected = $"Class:{classVal},Method:{methodVal}";
                var exists = executedTests.Contains(expected);
                Console.WriteLine($"[DIAGNOSTIC] {expected} - {(exists ? "FOUND" : "MISSING")}");
            }
        }
        
        // Also check for implicit operators
        Console.WriteLine($"[DIAGNOSTIC] DataSource1 implicit operator test: {CheckImplicitOperator<DataSource1>()}");
        Console.WriteLine($"[DIAGNOSTIC] DataSource2 implicit operator test: {CheckImplicitOperator<DataSource2>()}");
        Console.WriteLine($"[DIAGNOSTIC] DataSource3 implicit operator test: {CheckImplicitOperator<DataSource3>()}");
        
        // Clear for next run
        ExecutedTests.Clear();
        CreatedInstances.Clear();
    }
    
    private static string CheckImplicitOperator<T>() where T : new()
    {
        try
        {
            var instance = new T();
            // Try to convert to int using implicit operator
            if (instance is DataSource1 ds1)
            {
                int value = ds1;
                return $"Success - value: {value}";
            }
            else if (instance is DataSource2 ds2)
            {
                int value = ds2;
                return $"Success - value: {value}";
            }
            else if (instance is DataSource3 ds3)
            {
                int value = ds3;
                return $"Success - value: {value}";
            }
            return "Failed - type not recognized";
        }
        catch (Exception ex)
        {
            return $"Failed - {ex.GetType().Name}: {ex.Message}";
        }
    }

    public class DataSource1
    {
        public static implicit operator int(DataSource1 _) => 10;
    }

    public class DataSource2
    {
        public static implicit operator int(DataSource2 _) => 20;
    }

    public class DataSource3
    {
        public static implicit operator int(DataSource3 _) => 30;
    }
}
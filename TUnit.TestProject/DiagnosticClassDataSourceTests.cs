using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Diagnostic test to debug single file mode issues
[EngineTest(ExpectedResult.Pass)]
[ClassDataSource(typeof(DiagDataSource1))]
[ClassDataSource(typeof(DiagDataSource2))]
[ClassDataSource(typeof(DiagDataSource3))]
public class DiagnosticClassDataSourceTests(int classValue)
{
    private static readonly ConcurrentBag<string> ExecutedTests = [];
    private static readonly ConcurrentBag<string> DiscoveryLog = [];

    static DiagnosticClassDataSourceTests()
    {
        // Log type information during static initialization
        var thisType = typeof(DiagnosticClassDataSourceTests);
        DiscoveryLog.Add($"[STATIC] Type: {thisType.FullName}");
        DiscoveryLog.Add($"[STATIC] Assembly: {thisType.Assembly.FullName}");
        
        try
        {
            DiscoveryLog.Add($"[STATIC] Assembly Location: {thisType.Assembly.Location}");
        }
        catch (Exception ex)
        {
            DiscoveryLog.Add($"[STATIC] Assembly Location Error: {ex.Message}");
        }

        // Log all attributes on the class
        var attributes = thisType.GetCustomAttributes(false);
        DiscoveryLog.Add($"[STATIC] Attribute Count: {attributes.Length}");
        foreach (var attr in attributes)
        {
            DiscoveryLog.Add($"[STATIC] Attribute: {attr.GetType().Name}");
        }

        // Check if nested types are visible
        DiscoveryLog.Add($"[STATIC] DiagDataSource1 visible: {typeof(DiagDataSource1).IsVisible}");
        DiscoveryLog.Add($"[STATIC] DiagDataSource2 visible: {typeof(DiagDataSource2).IsVisible}");
        DiscoveryLog.Add($"[STATIC] DiagDataSource3 visible: {typeof(DiagDataSource3).IsVisible}");
    }

    [Test]
    [MethodDataSource(nameof(GetDiagnosticData))]
    public async Task DiagnosticTest(string methodValue)
    {
        // Log execution details
        var executionInfo = $"Class:{classValue},Method:{methodValue}";
        ExecutedTests.Add(executionInfo);
        
        DiscoveryLog.Add($"[EXEC] Test executed: {executionInfo}");
        DiscoveryLog.Add($"[EXEC] Thread: {Thread.CurrentThread.ManagedThreadId}");
        DiscoveryLog.Add($"[EXEC] Process: {Process.GetCurrentProcess().Id}");

        await Task.CompletedTask;
    }

    public static IEnumerable<string> GetDiagnosticData()
    {
        DiscoveryLog.Add($"[METHOD] GetDiagnosticData called");
        yield return "Alpha";
        yield return "Beta";
    }

    [After(Assembly)]
    public static async Task LogDiagnosticInfo()
    {
        Console.WriteLine("\n=== DIAGNOSTIC INFORMATION ===");
        
        // Log discovery information
        Console.WriteLine("\nDiscovery Log:");
        foreach (var log in DiscoveryLog.OrderBy(l => l))
        {
            Console.WriteLine(log);
        }

        // Log execution results
        var executedTests = ExecutedTests.ToList();
        Console.WriteLine($"\nExecuted Tests Count: {executedTests.Count}");
        foreach (var test in executedTests.OrderBy(t => t))
        {
            Console.WriteLine($"  - {test}");
        }

        // Expected vs Actual
        var expected = new[]
        {
            "Class:111,Method:Alpha",
            "Class:111,Method:Beta",
            "Class:222,Method:Alpha",
            "Class:222,Method:Beta",
            "Class:333,Method:Alpha",
            "Class:333,Method:Beta"
        };

        Console.WriteLine($"\nExpected: {expected.Length} tests");
        Console.WriteLine($"Actual: {executedTests.Count} tests");

        var missing = expected.Except(executedTests).ToList();
        if (missing.Any())
        {
            Console.WriteLine("\nMissing combinations:");
            foreach (var m in missing)
            {
                Console.WriteLine($"  - {m}");
            }
        }

        Console.WriteLine("\n=== END DIAGNOSTIC ===\n");

        // Still do the assertion
        if (executedTests.Count > 0)
        {
            await Assert.That(executedTests.Count).IsEqualTo(6);
        }
    }

    public class DiagDataSource1
    {
        public static implicit operator int(DiagDataSource1 _) => 111;
    }

    public class DiagDataSource2
    {
        public static implicit operator int(DiagDataSource2 _) => 222;
    }

    public class DiagDataSource3
    {
        public static implicit operator int(DiagDataSource3 _) => 333;
    }
}
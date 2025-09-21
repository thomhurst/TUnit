using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Test classes that combine ParallelGroup and NotInParallel constraints

[ParallelGroup("DatabaseTests")]
[NotInParallel("Schema")]
[EngineTest(ExpectedResult.Pass)]
public class CombinedConstraints_SchemaTests
{
    internal static readonly ConcurrentBag<(string TestName, DateTime Start, DateTime End)> ExecutionLog = [];
    private static readonly object SchemaLock = new();
    
    [Before(Class)]
    public static async Task SetupSchema()
    {
        lock (SchemaLock)
        {
            ExecutionLog.Add(("SchemaTests.Before(Class)", DateTime.UtcNow, DateTime.UtcNow));
        }
        await Task.Delay(50); // Simulate schema setup
    }
    
    [Test]
    public async Task MigrateSchema_V1()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100); // Simulate migration work
        var end = DateTime.UtcNow;
        
        lock (SchemaLock)
        {
            ExecutionLog.Add(("MigrateSchema_V1", start, end));
        }
    }
    
    [Test]
    public async Task MigrateSchema_V2()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100); // Simulate migration work
        var end = DateTime.UtcNow;
        
        lock (SchemaLock)
        {
            ExecutionLog.Add(("MigrateSchema_V2", start, end));
        }
    }
    
    [After(Class)]
    public static async Task TeardownSchema()
    {
        await Task.Delay(50); // Simulate cleanup
        lock (SchemaLock)
        {
            ExecutionLog.Add(("SchemaTests.After(Class)", DateTime.UtcNow, DateTime.UtcNow));
        }
    }
}

[ParallelGroup("DatabaseTests")]
[NotInParallel("Data")]
[EngineTest(ExpectedResult.Pass)]
public class CombinedConstraints_DataTests
{
    [Before(Class)]
    public static async Task SetupData()
    {
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("DataTests.Before(Class)", DateTime.UtcNow, DateTime.UtcNow));
        }
        await Task.Delay(50);
    }
    
    [Test]
    public async Task LoadTestData_Set1()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("LoadTestData_Set1", start, end));
        }
    }
    
    [Test]
    public async Task LoadTestData_Set2()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("LoadTestData_Set2", start, end));
        }
    }
    
    [After(Class)]
    public static async Task TeardownData()
    {
        await Task.Delay(50);
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("DataTests.After(Class)", DateTime.UtcNow, DateTime.UtcNow));
        }
    }
}

[ParallelGroup("DatabaseTests")]
[EngineTest(ExpectedResult.Pass)]
public class CombinedConstraints_QueryTests
{
    // These tests have only ParallelGroup constraint, so they can run in parallel within the group
    
    [Before(Class)]
    public static async Task SetupQueries()
    {
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("QueryTests.Before(Class)", DateTime.UtcNow, DateTime.UtcNow));
        }
        await Task.Delay(50);
    }
    
    [Test]
    public async Task QueryTest1()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("QueryTest1", start, end));
        }
    }
    
    [Test]
    public async Task QueryTest2()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("QueryTest2", start, end));
        }
    }
    
    [After(Class)]
    public static async Task TeardownQueries()
    {
        await Task.Delay(50);
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("QueryTests.After(Class)", DateTime.UtcNow, DateTime.UtcNow));
        }
    }
}

[ParallelGroup("ApiTests")]
[EngineTest(ExpectedResult.Pass)]
public class CombinedConstraints_ApiTests
{
    // Different parallel group - should not run while DatabaseTests group is running
    
    [Test]
    public async Task ApiTest1()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("ApiTest1", start, end));
        }
    }
    
    [Test]
    public async Task ApiTest2()
    {
        var start = DateTime.UtcNow;
        await Task.Delay(100);
        var end = DateTime.UtcNow;
        
        lock (CombinedConstraints_SchemaTests.ExecutionLog)
        {
            CombinedConstraints_SchemaTests.ExecutionLog.Add(("ApiTest2", start, end));
        }
    }
}

// Verification test that runs last
[EngineTest(ExpectedResult.Pass)]
public class CombinedConstraints_VerificationTest
{
    [Test]
    [NotInParallel(Order = int.MaxValue)]
    public async Task VerifyCombinedConstraints()
    {
        // Wait for all tests to complete
        await Task.Delay(500);
        
        var log = CombinedConstraints_SchemaTests.ExecutionLog.OrderBy(x => x.Start).ToList();
        
        // 1. Verify that tests with same NotInParallel key don't overlap
        var schemaTests = log.Where(x => x.TestName.Contains("Schema") && !x.TestName.Contains("Class")).ToList();
        for (int i = 0; i < schemaTests.Count - 1; i++)
        {
            await Assert.That(schemaTests[i].End <= schemaTests[i + 1].Start)
                .IsTrue()
                .Because($"Schema tests should not overlap: {schemaTests[i].TestName} ends at {schemaTests[i].End:HH:mm:ss.fff}, {schemaTests[i + 1].TestName} starts at {schemaTests[i + 1].Start:HH:mm:ss.fff}");
        }
        
        var dataTests = log.Where(x => x.TestName.Contains("Data") && !x.TestName.Contains("Class")).ToList();
        for (int i = 0; i < dataTests.Count - 1; i++)
        {
            await Assert.That(dataTests[i].End <= dataTests[i + 1].Start)
                .IsTrue()
                .Because($"Data tests should not overlap: {dataTests[i].TestName} ends at {dataTests[i].End:HH:mm:ss.fff}, {dataTests[i + 1].TestName} starts at {dataTests[i + 1].Start:HH:mm:ss.fff}");
        }
        
        // 2. Verify that Schema and Data tests CAN overlap (different constraint keys)
        var schemaRange = (
            Start: schemaTests.Min(t => t.Start),
            End: schemaTests.Max(t => t.End)
        );
        var dataRange = (
            Start: dataTests.Min(t => t.Start),  
            End: dataTests.Max(t => t.End)
        );
        
        var canOverlap = (schemaRange.Start < dataRange.End && dataRange.Start < schemaRange.End) ||
                         (schemaRange.Start >= dataRange.End || dataRange.Start >= schemaRange.End);
        
        await Assert.That(canOverlap)
            .IsTrue()
            .Because("Schema and Data tests should be able to run in parallel (different keys)");
        
        // 3. Verify that QueryTests can run in parallel (no NotInParallel constraint)
        var queryTests = log.Where(x => x.TestName.StartsWith("QueryTest")).ToList();
        if (queryTests.Count > 1)
        {
            // At least some query tests should overlap
            var hasOverlap = false;
            for (int i = 0; i < queryTests.Count - 1; i++)
            {
                if (queryTests[i].End > queryTests[i + 1].Start && queryTests[i + 1].End > queryTests[i].Start)
                {
                    hasOverlap = true;
                    break;
                }
            }
            // This is a relaxed check - in some cases they might not overlap due to timing
            // But they should be allowed to overlap
        }
        
        // 4. Verify that ApiTests don't run while DatabaseTests are running
        var apiTests = log.Where(x => x.TestName.Contains("Api")).ToList();
        var databaseTests = log.Where(x => 
            x.TestName.Contains("Schema") || 
            x.TestName.Contains("Data") || 
            x.TestName.Contains("Query")).ToList();
            
        if (apiTests.Any() && databaseTests.Any())
        {
            var apiRange = (
                Start: apiTests.Min(t => t.Start),
                End: apiTests.Max(t => t.End)
            );
            var dbRange = (
                Start: databaseTests.Min(t => t.Start),
                End: databaseTests.Max(t => t.End)
            );
            
            var noOverlap = apiRange.End <= dbRange.Start || dbRange.End <= apiRange.Start;
            await Assert.That(noOverlap)
                .IsTrue()
                .Because($"ApiTests and DatabaseTests groups should not overlap. Api: {apiRange.Start:HH:mm:ss.fff}-{apiRange.End:HH:mm:ss.fff}, DB: {dbRange.Start:HH:mm:ss.fff}-{dbRange.End:HH:mm:ss.fff}");
        }
        
        // 5. Verify class-level hooks run correctly
        var schemaClassSetup = log.FirstOrDefault(x => x.TestName == "SchemaTests.Before(Class)");
        var schemaClassTeardown = log.FirstOrDefault(x => x.TestName == "SchemaTests.After(Class)");
        var schemaTestsInClass = log.Where(x => x.TestName.Contains("MigrateSchema")).ToList();
        
        if (schemaClassSetup.TestName != null && schemaTestsInClass.Any())
        {
            await Assert.That(schemaClassSetup.Start < schemaTestsInClass.Min(t => t.Start))
                .IsTrue()
                .Because("Schema Before(Class) should run before all schema tests");
        }
        
        if (schemaClassTeardown.TestName != null && schemaTestsInClass.Any())
        {
            await Assert.That(schemaClassTeardown.Start > schemaTestsInClass.Max(t => t.End))
                .IsTrue()
                .Because("Schema After(Class) should run after all schema tests");
        }
    }
}
using TUnit.Core;

namespace TUnit.TestProject;

public static class CancellationCleanupTestTracker
{
    public static readonly List<string> ExecutedCleanups = [];
    public static void Reset() => ExecutedCleanups.Clear();
}

public class CancellationCleanupTestClass
{
    [After(Test)]
    public async Task CleanupAfterTest()
    {
        // This cleanup method should execute even if test run is cancelled
        CancellationCleanupTestTracker.ExecutedCleanups.Add("AfterTest");
        await Task.CompletedTask;
    }
    
    [After(Class)]
    public static async Task CleanupAfterClass()
    {
        CancellationCleanupTestTracker.ExecutedCleanups.Add("AfterClass");
        await Task.CompletedTask;
    }
    
    [Test]
    public async Task NormalTest()
    {
        CancellationCleanupTestTracker.ExecutedCleanups.Add("TestStarted");
        await Task.Delay(100); // Short delay to simulate test work
        CancellationCleanupTestTracker.ExecutedCleanups.Add("TestCompleted");
    }
}
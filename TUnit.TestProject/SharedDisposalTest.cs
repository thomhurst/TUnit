using TUnit.Core;

namespace TUnit.TestProject;

public class DisposableSharedInstance : IAsyncDisposable
{
    private static int _instanceCount = 0;
    private static int _disposedCount = 0;
    
    public readonly int InstanceId;
    public string Value { get; set; } = "shared-instance";
    public bool IsDisposed { get; private set; }

    public DisposableSharedInstance()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[DisposableSharedInstance] Created instance {InstanceId} (total: {_instanceCount}) - Hash: {GetHashCode()}");
    }

    public ValueTask DisposeAsync()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;
            var disposedCount = Interlocked.Increment(ref _disposedCount);
            Console.WriteLine($"[DisposableSharedInstance] Disposed instance {InstanceId} (total disposed: {disposedCount}) - Hash: {GetHashCode()}");
        }
        return default;
    }
    
    public static int InstanceCount => _instanceCount;
    public static int DisposedCount => _disposedCount;
    
    public static void Reset()
    {
        _instanceCount = 0;
        _disposedCount = 0;
    }
}

[ClassDataSource<DisposableSharedInstance>(Shared = SharedType.PerClass)]
public class SharedDisposalTest
{
    private readonly DisposableSharedInstance sharedInstance;
    
    public SharedDisposalTest(DisposableSharedInstance sharedInstance)
    {
        this.sharedInstance = sharedInstance;
    }
    [Test]
    public async Task Test1_ShouldUseSharedInstance()
    {
        Console.WriteLine($"[Test1] Using instance {sharedInstance.InstanceId} (Hash: {sharedInstance.GetHashCode()}), IsDisposed: {sharedInstance.IsDisposed}");
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
        
        // Verify this is actually a shared instance by checking the static counters
        Console.WriteLine($"[Test1] Current counts - Total instances: {DisposableSharedInstance.InstanceCount}, Total disposed: {DisposableSharedInstance.DisposedCount}");
    }

    [Test]
    public async Task Test2_ShouldUseSharedInstance()
    {
        Console.WriteLine($"[Test2] Using instance {sharedInstance.InstanceId} (Hash: {sharedInstance.GetHashCode()}), IsDisposed: {sharedInstance.IsDisposed}");
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
        
        // Verify this is actually a shared instance by checking the static counters
        Console.WriteLine($"[Test2] Current counts - Total instances: {DisposableSharedInstance.InstanceCount}, Total disposed: {DisposableSharedInstance.DisposedCount}");
    }

    [Test]
    public async Task Test3_ShouldUseSharedInstance()
    {
        Console.WriteLine($"[Test3] Using instance {sharedInstance.InstanceId} (Hash: {sharedInstance.GetHashCode()}), IsDisposed: {sharedInstance.IsDisposed}");
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
        
        // Verify this is actually a shared instance by checking the static counters
        Console.WriteLine($"[Test3] Current counts - Total instances: {DisposableSharedInstance.InstanceCount}, Total disposed: {DisposableSharedInstance.DisposedCount}");
    }

    [After(Class)]
    public static async Task VerifySharedInstanceDisposal(ClassHookContext context)
    {
        Console.WriteLine($"[AfterClass] Before delay - Instances: {DisposableSharedInstance.InstanceCount}, Disposed: {DisposableSharedInstance.DisposedCount}");
        
        // Give disposal a chance to complete
        await Task.Delay(1000);
        
        Console.WriteLine($"[AfterClass] After delay - Instances: {DisposableSharedInstance.InstanceCount}, Disposed: {DisposableSharedInstance.DisposedCount}");
        
        // For PerClass sharing, we should have exactly 1 instance created and 1 disposed
        if (DisposableSharedInstance.InstanceCount != 1)
        {
            Console.WriteLine($"[AfterClass] ERROR: Expected 1 instance, but got {DisposableSharedInstance.InstanceCount}");
        }
        
        if (DisposableSharedInstance.DisposedCount != 1)
        {
            Console.WriteLine($"[AfterClass] ERROR: Expected 1 disposed instance, but got {DisposableSharedInstance.DisposedCount}");
        }
        
        // Print the test instances to see if they were using the same shared instance
        var testInstances = context.Tests.Select(t => t.TestDetails.ClassInstance).OfType<SharedDisposalTest>().ToList();
        Console.WriteLine($"[AfterClass] Test instances: {testInstances.Count}");
        for (var i = 0; i < testInstances.Count; i++)
        {
            var instance = testInstances[i];
            Console.WriteLine($"[AfterClass] Test instance {i}: shared instance {instance.sharedInstance.InstanceId} (Hash: {instance.sharedInstance.GetHashCode()}), disposed: {instance.sharedInstance.IsDisposed}");
        }
        
        // Verify all test instances used the same shared instance
        if (testInstances.Count > 1)
        {
            var firstInstanceId = testInstances[0].sharedInstance.InstanceId;
            var firstInstanceHash = testInstances[0].sharedInstance.GetHashCode();
            for (var i = 1; i < testInstances.Count; i++)
            {
                var currentInstanceId = testInstances[i].sharedInstance.InstanceId;
                var currentInstanceHash = testInstances[i].sharedInstance.GetHashCode();
                if (currentInstanceId != firstInstanceId || currentInstanceHash != firstInstanceHash)
                {
                    Console.WriteLine($"[AfterClass] ERROR: Test instance {i} used different shared instance! Expected ID {firstInstanceId} (Hash: {firstInstanceHash}), got ID {currentInstanceId} (Hash: {currentInstanceHash})");
                }
            }
        }
    }
}
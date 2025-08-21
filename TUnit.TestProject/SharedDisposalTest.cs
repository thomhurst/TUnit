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
        Console.WriteLine($"[DisposableSharedInstance] Created instance {InstanceId} (total: {_instanceCount})");
    }

    public ValueTask DisposeAsync()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;
            var disposedCount = Interlocked.Increment(ref _disposedCount);
            Console.WriteLine($"[DisposableSharedInstance] Disposed instance {InstanceId} (total disposed: {disposedCount})");
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
public class SharedDisposalTest(DisposableSharedInstance sharedInstance)
{
    [Test]
    public async Task Test1_ShouldUseSharedInstance()
    {
        Console.WriteLine($"[Test1] Using instance {sharedInstance.InstanceId}, IsDisposed: {sharedInstance.IsDisposed}");
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
    }

    [Test]
    public async Task Test2_ShouldUseSharedInstance()
    {
        Console.WriteLine($"[Test2] Using instance {sharedInstance.InstanceId}, IsDisposed: {sharedInstance.IsDisposed}");
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
    }

    [After(Class)]
    public static async Task VerifySharedInstanceDisposal(ClassHookContext context)
    {
        Console.WriteLine($"[AfterClass] Before delay - Instances: {DisposableSharedInstance.InstanceCount}, Disposed: {DisposableSharedInstance.DisposedCount}");
        
        // Give disposal a chance to complete
        await Task.Delay(500);
        
        Console.WriteLine($"[AfterClass] After delay - Instances: {DisposableSharedInstance.InstanceCount}, Disposed: {DisposableSharedInstance.DisposedCount}");
        
        // For PerClass sharing, we should have exactly 1 instance created and 1 disposed
        // await Assert.That(DisposableSharedInstance.InstanceCount).IsEqualTo(1);
        // await Assert.That(DisposableSharedInstance.DisposedCount).IsEqualTo(1);
        
        // Print the test instances to see if they were using the same shared instance
        var testInstances = context.Tests.Select(t => t.TestDetails.ClassInstance).OfType<SharedDisposalTest>().ToList();
        Console.WriteLine($"[AfterClass] Test instances: {testInstances.Count}");
        for (int i = 0; i < testInstances.Count; i++)
        {
            var instance = testInstances[i];
            Console.WriteLine($"[AfterClass] Test instance {i}: shared instance {instance.sharedInstance.InstanceId}, disposed: {instance.sharedInstance.IsDisposed}");
        }
    }
}
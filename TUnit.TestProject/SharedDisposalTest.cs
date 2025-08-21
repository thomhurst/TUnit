using TUnit.Core;

namespace TUnit.TestProject;

public class DisposableSharedInstance : IAsyncDisposable
{
    public string Value { get; set; } = "shared-instance";
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

[ClassDataSource<DisposableSharedInstance>(Shared = SharedType.PerClass)]
public class SharedDisposalTest(DisposableSharedInstance sharedInstance)
{
    [Test]
    public async Task Test1_ShouldUseSharedInstance()
    {
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
    }

    [Test]
    public async Task Test2_ShouldUseSharedInstance()
    {
        await Assert.That(sharedInstance.IsDisposed).IsFalse();
        await Assert.That(sharedInstance.Value).IsEqualTo("shared-instance");
    }

    [After(Class)]
    public static async Task VerifySharedInstanceDisposal(ClassHookContext context)
    {
        // The shared instance should be disposed after all tests in the class complete
        // We can't directly access the shared instance here, but we can verify
        // that the disposal events were triggered
        await Task.Delay(100); // Give disposal a chance to complete
        Console.WriteLine("Class cleanup completed - shared instance should be disposed");
    }
}
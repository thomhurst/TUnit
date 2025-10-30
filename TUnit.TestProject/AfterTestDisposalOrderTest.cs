using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/3156
/// Ensures that After(Test) hooks are executed before the test instance is disposed.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class AfterTestDisposalOrderTest : IAsyncDisposable
{
    private bool _isDisposed;
    private readonly string _testResource = "TestResource";

    public ValueTask DisposeAsync()
    {
        _isDisposed = true;
        return new ValueTask();
    }

    [Test]
    public async Task Test_ShouldAccessResourcesInAfterTestHook()
    {
        // Test should be able to access resources
        await Assert.That(_isDisposed).IsFalse();
        await Assert.That(_testResource).IsNotNull();
        await Assert.That(_testResource).IsEqualTo("TestResource");
    }

    [After(Test)]
    public async Task AfterTest_ShouldAccessResourcesBeforeDisposal(TestContext context)
    {
        // After(Test) hook should be able to access instance resources before disposal
        await Assert.That(_isDisposed).IsFalse().Because("Test instance should not be disposed before After(Test) hooks");
        await Assert.That(_testResource).IsNotNull().Because("Should be able to access instance fields in After(Test) hook");
        await Assert.That(_testResource).IsEqualTo("TestResource");

        // Mark that we successfully accessed resources
        context.StateBag.Items["AfterTestExecuted"] = true;
        context.StateBag.Items["ResourceValue"] = _testResource;
    }

    [After(Class)]
    public static async Task AfterClass_VerifyDisposalCompleted(ClassHookContext context)
    {
        // Verify that After(Test) hooks were executed
        foreach (var test in context.Tests)
        {
            await Assert.That(test.ObjectBag.ContainsKey("AfterTestExecuted")).IsTrue().Because("After(Test) hook should have executed");
            await Assert.That(test.StateBag.Items["AfterTestExecuted"]).IsEqualTo(true);
            await Assert.That(test.StateBag.Items["ResourceValue"]).IsEqualTo("TestResource");
        }

        // By the time After(Class) runs, all test instances should be disposed
        // We can't directly check _isDisposed here since this is a static method,
        // but the fact that After(Test) ran successfully proves the order is correct
    }
}

/// <summary>
/// Additional test to verify disposal order with async operations
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class AsyncAfterTestDisposalOrderTest : IAsyncDisposable
{
    private MyAsyncResource? _resource;
    private bool _isDisposed;

    public AsyncAfterTestDisposalOrderTest()
    {
        _resource = new MyAsyncResource();
    }

    public async ValueTask DisposeAsync()
    {
        if (_resource != null)
        {
            await _resource.DisposeAsync();
            _resource = null;
        }
        _isDisposed = true;
    }

    [Test]
    public async Task Test_WithAsyncResource()
    {
        await Assert.That(_resource).IsNotNull();
        await Assert.That(_resource!.IsDisposed).IsFalse();

        var value = await _resource.GetValueAsync();
        await Assert.That(value).IsEqualTo("AsyncValue");
    }

    [After(Test)]
    public async Task AfterTest_ShouldAccessAsyncResourceBeforeDisposal()
    {
        await Assert.That(_isDisposed).IsFalse().Because("Test instance should not be disposed before After(Test) hooks");
        await Assert.That(_resource).IsNotNull().Because("Resource should still be available in After(Test) hook");
        await Assert.That(_resource!.IsDisposed).IsFalse().Because("Resource should not be disposed yet");

        // Should be able to use the async resource
        var value = await _resource.GetValueAsync();
        await Assert.That(value).IsEqualTo("AsyncValue");
    }

    private class MyAsyncResource : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public Task<string> GetValueAsync()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MyAsyncResource));
            }
            return Task.FromResult("AsyncValue");
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return new ValueTask();
        }
    }
}

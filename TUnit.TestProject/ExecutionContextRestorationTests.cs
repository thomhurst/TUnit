using System.Runtime.CompilerServices;

namespace TUnit.TestProject;

public class ExecutionContextRestorationTests
{
    private static readonly AsyncLocal<string> TestAsyncLocal = new();
    
    [Before(Test)]
    public async Task BeforeTest(TestContext context)
    {
        // Set a value in async local
        TestAsyncLocal.Value = "BeforeTest";
        
        // Required by TUnit to flow AsyncLocal values to tests
        context.AddAsyncLocalValues();
        
        await Task.Yield();
    }
    
    [After(Test)]
    public async Task AfterTest(TestContext context)
    {
        // The async local should be accessible here
        var value = TestAsyncLocal.Value;
        context.WriteLine($"AfterTest AsyncLocal value: {value}");
        await Task.Yield();
    }
    
    [Test]
    public async Task TestExecutionContextFlow()
    {
        // The test should have access to the async local set in BeforeTest
        var value = TestAsyncLocal.Value;
        await Assert.That(value).IsEqualTo("BeforeTest");
        
        // Modify the value
        TestAsyncLocal.Value = "TestModified";
        await Task.Yield();
    }
    
    [Test]
    public async Task TestExecutionContextIsolation()
    {
        // Each test should have its own execution context
        var value = TestAsyncLocal.Value;
        await Assert.That(value).IsEqualTo("BeforeTest");
        
        // This test's modifications shouldn't affect other tests
        TestAsyncLocal.Value = "IsolationTest";
        await Task.Yield();
    }
}
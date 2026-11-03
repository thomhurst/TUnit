namespace TUnit.TestProject;

public class ExecutionContextRestorationTests
{
    private static readonly AsyncLocal<string> TestAsyncLocal = new();
    private TestContext _context = null!;
    
    [Before(Test)]
    public async Task BeforeTest(TestContext context)
    {
        _context = context;

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
        context.Output.StandardOutput.WriteLine($"AfterTest AsyncLocal value: {value}");
        await Task.Yield();
    }
    
    [Test]
    [Timeout(5000)]
    public async Task TimedTestPreservesExecutionContext()
    {
        await Assert.That(TestContext.Current).IsSameReferenceAs(_context);
        await TestExecutionContextFlow();
        await Assert.That(TestContext.Current).IsSameReferenceAs(_context);
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
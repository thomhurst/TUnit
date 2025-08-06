using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class AsyncLocalFlowVerificationTest
{
    private static readonly AsyncLocal<string> AssemblyValue = new();
    private static readonly AsyncLocal<string> ClassValue = new();
    private static readonly AsyncLocal<string> TestValue = new();

    [Before(Assembly)]
    public static async Task BeforeAssembly(AssemblyHookContext context)
    {
        await Task.CompletedTask;
        AssemblyValue.Value = "FromAssembly";
        context.AddAsyncLocalValues();
    }

    [Before(Class)]
    public static async Task BeforeClass(ClassHookContext context)
    {
        await Task.CompletedTask;
        // Verify assembly value flows here
        if (AssemblyValue.Value != "FromAssembly")
            throw new Exception($"Assembly value not flowing! Got: {AssemblyValue.Value}");

        ClassValue.Value = "FromClass";
        context.AddAsyncLocalValues();
    }

    [Before(Test)]
    public async Task BeforeTest(TestContext context)
    {
        await Task.CompletedTask;
        // Verify both assembly and class values flow here
        if (AssemblyValue.Value != "FromAssembly")
            throw new Exception($"Assembly value not flowing to test hook! Got: {AssemblyValue.Value}");
        if (ClassValue.Value != "FromClass")
            throw new Exception($"Class value not flowing to test hook! Got: {ClassValue.Value}");

        TestValue.Value = "FromTest";
        context.AddAsyncLocalValues();
    }

    [Test]
    public async Task VerifyAsyncLocalFlow()
    {
        await Task.CompletedTask;

        // All three values should be available in the test
        Assert.That(AssemblyValue.Value).IsEqualTo("FromAssembly");
        Assert.That(ClassValue.Value).IsEqualTo("FromClass");
        Assert.That(TestValue.Value).IsEqualTo("FromTest");
    }
}

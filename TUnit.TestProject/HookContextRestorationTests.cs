namespace TUnit.TestProject;

public class HookContextRestorationTests
{
    private static AssemblyHookContext? _capturedAssemblyContextInHook;
    private static ClassHookContext? _capturedClassContextInHook;
    
    [Before(Test)]
    public async Task BeforeTest(TestContext context)
    {
        // Capture the current hook contexts in the test hook
        _capturedAssemblyContextInHook = AssemblyHookContext.Current;
        _capturedClassContextInHook = ClassHookContext.Current;
        
        // Required by TUnit to flow AsyncLocal values to tests
        context.AddAsyncLocalValues();
        
        await Task.Yield();
    }
    
    [Test]
    public async Task Test_Hook_Contexts_Are_Available_In_Test()
    {
        // Check that hook contexts are available during test execution
        var currentAssemblyContext = AssemblyHookContext.Current;
        var currentClassContext = ClassHookContext.Current;
        
        // Verify assembly context
        await Assert.That(currentAssemblyContext).IsNotNull();
        await Assert.That(currentAssemblyContext?.Assembly.GetName().Name).IsEqualTo("TUnit.TestProject");
        
        // Verify class context  
        await Assert.That(currentClassContext).IsNotNull();
        await Assert.That(currentClassContext?.ClassType).IsEqualTo(typeof(HookContextRestorationTests));
        
        // Verify contexts captured in BeforeTest hook are the same
        await Assert.That(_capturedAssemblyContextInHook).IsNotNull();
        await Assert.That(_capturedClassContextInHook).IsNotNull();
        await Assert.That(_capturedAssemblyContextInHook).IsEqualTo(currentAssemblyContext);
        await Assert.That(_capturedClassContextInHook).IsEqualTo(currentClassContext);
    }
    
    [Test]
    public async Task Test_Hook_Contexts_Available_After_Property_Injection()
    {
        // This test verifies contexts are still available after property injection
        var assemblyContext = AssemblyHookContext.Current;
        var classContext = ClassHookContext.Current;
        
        await Assert.That(assemblyContext).IsNotNull();
        await Assert.That(classContext).IsNotNull();
        await Assert.That(classContext?.ClassType).IsEqualTo(typeof(HookContextRestorationTests));
    }
    
    [Before(Class)]
    public static async Task BeforeClass(ClassHookContext context)
    {
        // Verify that ClassHookContext.Current is available in class hooks
        var current = ClassHookContext.Current;
        await Assert.That(current).IsNotNull();
        await Assert.That(current).IsEqualTo(context);
        await Assert.That(context.ClassType).IsEqualTo(typeof(HookContextRestorationTests));
    }
    
    [After(Test)]
    public async Task AfterTest(TestContext context)
    {
        // Verify hook contexts are available in after test hooks
        var assemblyContext = AssemblyHookContext.Current;
        var classContext = ClassHookContext.Current;
        
        await Assert.That(assemblyContext).IsNotNull();
        await Assert.That(classContext).IsNotNull();
        
        context.WriteLine($"AfterTest - Assembly: {assemblyContext?.Assembly.GetName().Name}, Class: {classContext?.ClassType.Name}");
    }
}
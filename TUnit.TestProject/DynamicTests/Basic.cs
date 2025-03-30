namespace TUnit.TestProject.DynamicTests;

public class Basic
{
    public void SomeMethod()
    {
        Console.WriteLine(@"Hello, World!");
    }
    
    public async ValueTask SomeMethod_ValueTask()
    {
        await default(ValueTask);
        Console.WriteLine(@"Hello, World!");
    }
    
    public async Task SomeMethod_Task()
    {
        await Task.CompletedTask;
        Console.WriteLine(@"Hello, World!");
    }

    public void SomeMethod_Args(int a, string b, bool c)
    {
        Console.WriteLine(@"Hello, World!");
    }
    
    public async ValueTask SomeMethod_ValueTask_Args(int a, string b, bool c)
    {
        await default(ValueTask);
        Console.WriteLine(@"Hello, World!");
    }
    
    public async Task SomeMethod_Task_Args(int a, string b, bool c)
    {
        await Task.CompletedTask;
        Console.WriteLine(@"Hello, World!");
    }
    
    [DynamicTestBuilder]
    public void BuildTests(DynamicTestBuilderContext context)
    {
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod(),
            TestMethodArguments = [],
        });
        
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_Task(),
            TestMethodArguments = [],
        });
        
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_ValueTask(),
            TestMethodArguments = [],
        });
        
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
        });
        
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_Task_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
        });
        
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_ValueTask_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
        });
    }
}
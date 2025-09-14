using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

[EngineTest(ExpectedResult.Pass)]
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

#pragma warning disable TUnitWIP0001
    [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
    public void BuildTests(DynamicTestBuilderContext context)
    {
        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod(),
            TestMethodArguments = [],
            Attributes = [new RepeatAttribute(5)]
        });

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_Task(),
            TestMethodArguments = [],
            Attributes = [new RepeatAttribute(5)]
        });

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_ValueTask(),
            TestMethodArguments = [],
            Attributes = [new RepeatAttribute(5)]
        });

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
            Attributes = [new RepeatAttribute(5)]
        });

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_Task_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
            Attributes = [new RepeatAttribute(5)]
        });

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_ValueTask_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
            Attributes = [new RepeatAttribute(5)]
        });

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod_ValueTask_Args(1, "test", true),
            TestMethodArguments = [2, "test", false],
            Attributes = [new RepeatAttribute(5)]
        });
    }
}

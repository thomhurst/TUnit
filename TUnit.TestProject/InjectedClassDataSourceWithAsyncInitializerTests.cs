using TUnit.Core.Interfaces;
#pragma warning disable CS9113 // Parameter is unread.

namespace TUnit.TestProject;

[ClassDataSource<MyClass>(Shared = SharedType.Keyed, Key = "MyKey")]
public class InjectedClassDataSourceWithAsyncInitializerTests(InjectedClassDataSourceWithAsyncInitializerTests.MyClass myClass)
{
    [Before(Test)]
    public Task BeforeTest()
    {
        Console.WriteLine("BeforeTest");
        return Task.CompletedTask;
    }

    [Test]
    public Task Test1()
    {
        Console.WriteLine("Test");
        return Task.CompletedTask;
    }
    
    [Test]
    public Task Test2()
    {
        Console.WriteLine("Test");
        return Task.CompletedTask;
    }
    
    [Test]
    public Task Test3()
    {
        Console.WriteLine("Test");
        return Task.CompletedTask;
    }

    public class MyClass : IAsyncInitializer
    {
        public Task InitializeAsync()
        {
            Console.WriteLine("IAsyncInitializer.InitializeAsync");
            return Task.CompletedTask;
        }
    }
}
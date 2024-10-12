using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[Repeat(15)]
public class CaptureOutputTests
{
    private readonly MyClass _myClass = new();

    [Test]
    public async Task Test()
    {
        _myClass.DoSomething(1);

        using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).IsEqualTo("Test");
            await Assert.That(TestContext.Current!.GetStandardOutput()).IsEqualTo("Blah1");
        }
    }

    [Test]
    public async Task Test2()
    {
        _myClass.DoSomething(2);

        using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).IsEqualTo("Test2");
            await Assert.That(TestContext.Current!.GetStandardOutput()).IsEqualTo("Blah2");
        }
    }
            
    [Test]
    public async Task Test3()
    {
        _myClass.DoSomething(3);

        using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).IsEqualTo("Test3");
            await Assert.That(TestContext.Current!.GetStandardOutput()).IsEqualTo("Blah3");
        }
    }
        
    [Test]
    public async Task Test4()
    {
        _myClass.DoSomething(4);

        using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).IsEqualTo("Test4");
            await Assert.That(TestContext.Current!.GetStandardOutput()).IsEqualTo("Blah4");
        }
    }
        
    [Test]
    public async Task Test5()
    {
        _myClass.DoSomething(5);

        using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).IsEqualTo("Test5");
            await Assert.That(TestContext.Current!.GetStandardOutput()).IsEqualTo("Blah5");
        }
    }

    [After(Class)]
    public static void Log(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            Console.WriteLine(@$"Test {test.TestDetails.TestId} has output: {test.GetStandardOutput()}");
        }
    }
    
    private class MyClass
    {
        public void DoSomething(int i)
        {
            Console.WriteLine($"Blah{i}");
        }
    }
}
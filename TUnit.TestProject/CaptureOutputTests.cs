using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[Repeat(15)]
public class CaptureOutputTests
{
    private readonly MyClass _myClass;

    public CaptureOutputTests()
    {
        _myClass = new MyClass();
    }

    [Test]
    public async Task Test()
    {
        _myClass.DoSomething(1);

        await using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).Is.EqualTo("Test");
            await Assert.That(TestContext.Current!.GetTestOutput()).Is.EqualTo("Blah1");
        }
    }

    [Test]
    public async Task Test2()
    {
        _myClass.DoSomething(2);

        await using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).Is.EqualTo("Test2");
            await Assert.That(TestContext.Current!.GetTestOutput()).Is.EqualTo("Blah2");
        }
    }
            
    [Test]
    public async Task Test3()
    {
        _myClass.DoSomething(3);

        await using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).Is.EqualTo("Test3");
            await Assert.That(TestContext.Current!.GetTestOutput()).Is.EqualTo("Blah3");
        }
    }
        
    [Test]
    public async Task Test4()
    {
        _myClass.DoSomething(4);

        await using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).Is.EqualTo("Test4");
            await Assert.That(TestContext.Current!.GetTestOutput()).Is.EqualTo("Blah4");
        }
    }
        
    [Test]
    public async Task Test5()
    {
        _myClass.DoSomething(5);

        await using (Assert.Multiple())
        {
            await Assert.That(TestContext.Current!.TestDetails.TestName).Is.EqualTo("Test5");
            await Assert.That(TestContext.Current!.GetTestOutput()).Is.EqualTo("Blah5");
        }
    }

    [After(Class)]
    public static void Log(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            Console.WriteLine(@$"Test {test.TestDetails.TestId} has output: {test.GetTestOutput()}");
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
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[Repeat(1500)]
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

        await Assert.That(TestContext.Current?.GetConsoleOutput()).Is.EqualTo("Blah1");
    }
    
    [Test]
    public async Task Test2()
    {
        _myClass.DoSomething(2);

        await Assert.That(TestContext.Current?.GetConsoleOutput()).Is.EqualTo("Blah2");
    }
            
    [Test]
    public async Task Test3()
    {
        _myClass.DoSomething(3);

        await Assert.That(TestContext.Current?.GetConsoleOutput()).Is.EqualTo("Blah3");
    }
        
    [Test]
    public async Task Test4()
    {
        _myClass.DoSomething(4);

        await Assert.That(TestContext.Current?.GetConsoleOutput()).Is.EqualTo("Blah4");
    }
        
    [Test]
    public async Task Test5()
    {
        _myClass.DoSomething(5);

        await Assert.That(TestContext.Current?.GetConsoleOutput()).Is.EqualTo("Blah5");
    }
    
    private class MyClass
    {
        public void DoSomething(int i)
        {
            Console.WriteLine($"Blah{i}");
        }
    }
}
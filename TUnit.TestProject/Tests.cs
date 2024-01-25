using TUnit.Assertions;
using TUnit.Core.Attributes;

namespace TUnit.TestProject;

public class Tests
{
    // [SetUp]
    // public void Setup()
    // {
    // }

    [Test]
    public void Test1()
    {
        var one = "1";
        Assert.That(one, Is.EqualTo("1"));
    }
    
    [Test]
    public void Test2()
    {
        var one = "2";
        Assert.That(one, Is.EqualTo("1"));
    }
    
    [Test]
    public async Task Test3()
    {
        await Task.Yield();
        var one = "1";
        Assert.That(one, Is.EqualTo("1"));
    }
    
    [Test]
    public async Task Test4()
    {
        await Task.Yield();
        var one = "2";
        Assert.That(one, Is.EqualTo("1"));
    }
}
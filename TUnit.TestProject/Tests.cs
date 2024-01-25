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
        var value = "1";
        Assert.That(value, Is.EqualTo("1"));
    }
    
    [Test]
    public void Test2()
    {
        var value = "2";
        Assert.That(value, Is.EqualTo("1"));
    }
    
    [Test]
    public async Task Test3()
    {
        await Task.Yield();
        var value = "1";
        Assert.That(value, Is.EqualTo("1"));
    }
    
    [Test]
    public async Task Test4()
    {
        await Task.Yield();
        var value = "2";
        Assert.That(value, Is.EqualTo("1"));
    }
    
    [TestWithData("1")]
    [TestWithData("2")]
    public void ParameterisedTests1(string value)
    {
        Assert.That(value, Is.EqualTo("1"));
    }
    
    [TestWithData("1")]
    [TestWithData("2")]
    public async Task ParameterisedTests2(string value)
    {
        await Task.Yield();
        Assert.That(value, Is.EqualTo("1"));
    }
}
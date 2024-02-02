using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
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
        Assert.That(value);
    }
    
    [Test]
    public void Test2()
    {
        var value = "2";
        Assert.That(value);
    }
    
    [Test]
    public async Task Test3()
    {
        await Task.Yield();
        var value = "1";
        Assert.That(value);
    }
    
    [Test]
    public async Task Test4()
    {
        await Task.Yield();
        var value = "2";
        Assert.That(value);
    }
    
    [TestWithData("1")]
    [TestWithData("2")]
    public void ParameterisedTests1(string value)
    {
        Assert.That(value);
    }
    
    [TestWithData("1")]
    [TestWithData("2")]
    public async Task ParameterisedTests2(string value)
    {
        await Task.Yield();
        Assert.That(value);
    }

    [Test, Skip("Reason1")]
    public void Skip1()
    {
        var value = "1";
        Assert.That(value);
    }
    
    [Test, Skip("Reason2")]
    public async Task Skip2()
    {
        await Task.Yield();
        var value = "1";
        Assert.That(value);
    }
    
    [TestDataSource(nameof(One))]
    public void TestDataSource1(int value)
    {
        Assert.That(value);
    }
    
    [TestDataSource(nameof(One))]
    public async Task TestDataSource2(int value)
    {
        await Task.Yield();
        Assert.That(value);
    }
    
    [TestDataSource(nameof(Two))]
    public void TestDataSource3(int value)
    {
        Assert.That(value);
    }
    
    [TestDataSource(nameof(Two))]
    public async Task TestDataSource4(int value)
    {
        await Task.Yield();
        Assert.That(value);
    }
    
    [TestDataSource(nameof(TestDataSources), nameof(One))]
    public void TestDataSource5(int value)
    {
        Assert.That(value);
    }
    
    [TestDataSource(nameof(TestDataSources), nameof(One))]
    public async Task TestDataSource6(int value)
    {
        await Task.Yield();
        Assert.That(value);
    }
    
    [TestDataSource(nameof(TestDataSources), nameof(Two))]
    public void TestDataSource7(int value)
    {
        Assert.That(value);
    }
    
    [TestDataSource(nameof(TestDataSources), nameof(Two))]
    public async Task TestDataSource8(int value)
    {
        await Task.Yield();
        Assert.That(value);
    }

    [Test]
    public void TestContext1()
    {
        Assert.That(TestContext.Current.TestName);
    }
    
    [Test]
    public void TestContext2()
    {
        Assert.That(TestContext.Current.TestName);
    }
    
    [Test]
    public void Throws1()
    {
        Assert.That(() => new string([]));
    }
    
    [Test]
    public async Task Throws2()
    {
        await Assert.That(async () =>
        {
            await Task.Yield();
            new string([]);
        }).Is.EqualTo(string.Empty);
    }
    
    [Test]
    public void Throws3()
    {
        Assert.That(() => throw new ApplicationException());
    }
    
    [Test]
    public async Task Throws4()
    {
        await Assert.That(async () =>
        {
            await Task.Yield();
            return true;
        }).Is.False();
    }
    
    [Test, Timeout(500)]
    public async Task Timeout1()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Test]
    public void String_And_Condition()
    {
        Assert.That("1");

    }
    
    [Test]
    public void String_And_Condition2()
    {
        Assert.That("1");
    }
    
    [Test]
    public void Count1()
    {
        var list = new List<int> { 1, 2, 3 };
        Assert.That<object>(list);
        Assert.That<object>(list);
    }
    
    [Test]
    public void Count2()
    {
        var list = new List<int> { 1, 2, 3 };
        Assert.That<object>(list);
    }
    
    [Test]
    public void Count3()
    {
        var list = new[] { 1, 2, 3 };
        Assert.That<object>(list);
    }
    
    [Test]
    public void Count4()
    {
        var list = new[] { 1, 2, 3 };
        Assert.That(list).Is.EquivalentTo(new []{1});
    }
    
    public static int One() => 1;
    public static int Two() => 2;
}
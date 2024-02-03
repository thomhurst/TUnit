using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.TestProject;

public class Tests
{
    // [SetUp]
    // public void Setup()
    // {
    // }

    [Test]
    public async Task Test1()
    {
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    public async Task Test2()
    {
        var value = "2";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    public async Task Test3()
    {
        await Task.Yield();
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    public async Task Test4()
    {
        await Task.Yield();
        var value = "2";
        await Assert.That(value).Is.EqualTo("1");
    }

    [TestWithData("1")]
    [TestWithData("2")]
    public async Task ParameterisedTests1(string value)
    {
        await Assert.That(value).Is.EqualTo("1");
    }

    [TestWithData("1")]
    [TestWithData("2")]
    public async Task ParameterisedTests2(string value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test, Skip("Reason1")]
    public async Task Skip1()
    {
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test, Skip("Reason2")]
    public async Task Skip2()
    {
        await Task.Yield();
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [TestDataSource(nameof(One))]
    public async Task TestDataSource1(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(One))]
    public async Task TestDataSource2(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(Two))]
    public async Task TestDataSource3(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(Two))]
    public async Task TestDataSource4(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(TestDataSources), nameof(One))]
    public async Task TestDataSource5(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(TestDataSources), nameof(One))]
    public async Task TestDataSource6(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(TestDataSources), nameof(Two))]
    public async Task TestDataSource7(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [TestDataSource(nameof(TestDataSources), nameof(Two))]
    public async Task TestDataSource8(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    public async Task TestContext1()
    {
        await Assert.That(TestContext.Current.TestName).Is.EqualTo(nameof(TestContext1));
    }

    [Test]
    public async Task TestContext2()
    {
        await Assert.That(TestContext.Current.TestName).Is.EqualTo(nameof(TestContext1));
    }

    [Test]
    public async Task Throws1()
    {
        await Assert.That(() => new string([])).Is.EqualTo(string.Empty);
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
    public async Task Throws3()
    {
        await Assert.That(() => throw new ApplicationException()).Throws.Exception;
    }

    [Test]
    public async Task Throws4()
    {
        await Assert.That(async () =>
        {
            await Task.Yield();
            return true;
        }).Throws.Nothing;
    }

    [Test, Timeout(500)]
    [TestCategory("Fail")]
    public async Task Timeout1()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task String_And_Condition()
    {
        await Assert.That("1").Is.EqualTo("1").And.Has.Length().EqualTo(1);
    }

    [Test]
    public async Task String_And_Condition2()
    {
        await Assert.That("1").Is.EqualTo("2").And.Has.Length().EqualTo(2);
    }

    [Test]
    public async Task Count1()
    {
        var list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Is.EquivalentTo(new[] { 1, 2, 3 }).And.Has.Count().EqualTo(3);
    }

    [Test]
    public async Task Count2()
    {
        var list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Is.EquivalentTo(new[] { 1, 2, 3 }).And.Has.Count().EqualTo(5);
    }

    public static int One() => 1;
    public static int Two() => 2;
}
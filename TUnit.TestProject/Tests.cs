using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[SuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
public class Tests
{
    [Test]
    [Category("Pass")]
    public async Task ConsoleOutput()
    {
        Console.WriteLine("Blah!");

        await Assert.That(TestContext.Current?.GetTestOutput()).Is.EqualTo("Blah!", StringComparison.Ordinal);
    }
    
    [Test]
    [Category("Pass")]
    public async Task Test1()
    {
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }
    
    [Test]
    [Category("Pass")]
    public async Task LessThan()
    {
        int value = 1;
        await Assert.That(value).Is.LessThan(2);
    }

    [Test]
    [Category("Fail")]
    public async Task Test2()
    {
        var value = "2";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    [Category("Pass")]
    public async Task Test3()
    {
        await Task.Yield();
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    [Category("Fail")]
    public async Task Test4()
    {
        await Task.Yield();
        var value = "2";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    [Arguments("1")]
    [Arguments("2")]
    [Arguments("3")]
    [Arguments("4")]
    [Arguments("5")]
    [Category("Fail")]
    public async Task ParameterisedTests1(string value)
    {
        await Assert.That(value).Is.EqualTo("1").And.Has.Length().EqualTo(1);
    }

    [Test]
    [Arguments("1")]
    [Arguments("2")]
    [Arguments("3")]
    [Arguments("4")]
    [Arguments("5")]
    [Category("Fail")]
    public async Task ParameterisedTests2(string value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test, Skip("Reason1")]
    [Category("Skip")]
    public async Task Skip1()
    {
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test, Skip("Reason2")]
    [Category("Skip")]
    public async Task Skip2()
    {
        await Task.Yield();
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }
    
    [Test, CustomSkip]
    [Category("Skip")]
    public async Task CustomSkip1()
    {
        await Task.Yield();
        var value = "1";
        await Assert.That(value).Is.EqualTo("1");
    }

    [Test]
    [MethodDataSource(nameof(One))]
    [Category("Pass")]
    public async Task TestDataSource1(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(One))]
    [Category("Pass")]
    public async Task TestDataSource2(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(Two))]
    [Category("Fail")]
    public async Task TestDataSource3(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(Two))]
    [Category("Fail")]
    public async Task TestDataSource4(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(One))]
    [Category("Pass")]
    public async Task TestDataSource5(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(One))]
    [Category("Pass")]
    public async Task TestDataSource6(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }
    
    [Test]
    [MethodDataSource(typeof(TestDataSources), "Two")]
    [Category("Pass")]
    public async Task TestDataSource_Wrong(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }
    
    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(Two))]
    [Category("Fail")]
    public async Task TestDataSource7(int value)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(Two))]
    [Category("Fail")]
    public async Task TestDataSource8(int value)
    {
        await Task.Yield();
        await Assert.That(value).Is.EqualTo(1);
    }

    [Test]
    [Category("Pass")]
    public async Task TestContext1()
    {
        await Assert.That(TestContext.Current?.TestDetails.TestName).Is.EqualTo(nameof(TestContext1));
    }

    [Test]
    [Category("Fail")]
    public async Task TestContext2()
    {
        await Assert.That(TestContext.Current?.TestDetails.TestName).Is.EqualTo(nameof(TestContext1));
    }

    [Test]
    [Category("Fail")]
    public async Task Throws1()
    {
        await Assert.That(() => new string([])).Throws.Exception().OfAnyType();
    }

    [Test]
    [Category("Fail")]
    public async Task Throws2()
    {
        await Assert.That(async () =>
        {
            await Task.Yield();
        }).Throws.Exception().OfAnyType();
    }

    [Test]
    [Category("Pass")]
    public async Task Throws3()
    {
        await Assert.That(() => throw new ApplicationException()).Throws.Exception().OfAnyType();
    }

    [Test]
    [Category("Pass")]
    public async Task Throws4()
    {
        await Assert.That(async () =>
        {
            await Task.Yield();
            return true;
        }).Throws.Nothing();
    }

    [Test, Timeout(500)]
    [Category("Fail")]
    public async Task Timeout1(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    [Test]
    [Category("Pass")]
    public async Task String_And_Condition()
    {
        await Assert.That("1").Is.EqualTo("1").And.Has.Length().EqualTo(1);
    }

    [Test]
    [Category("Fail")]
    public async Task String_And_Condition2()
    {
        await Assert.That("1").Is.EqualTo("2").And.Has.Length().EqualTo(2);
    }

    [Test]
    [Category("Pass")]
    public async Task Count1()
    {
        var list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Is.EquivalentTo([1, 2, 3]).And.Has.Count().EqualTo(3);
    }
    
    [Test]
    [Category("Pass")]
    public async Task Single()
    {
        var list = new List<int> { 1 };
        await Assert.That(list).Has.SingleItem();
    }
    
    [Test]
    [Category("Pass")]
    public async Task DistinctItems()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(list).Has.DistinctItems();
    }
    
    [Test]
    [Category("Pass")]
    public async Task Enumerable_NotEmpty()
    {
        var list = new List<int> { 1, 2, 3 };
        await Assert.That(list).IsNotEmpty();
    }

    [Test]
    [Category("Fail")]
    public async Task Count2()
    {
        var list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Is.EquivalentTo([1, 2, 3, 4, 5]).And.Has.Count().EqualTo(5);
    }
    
    [Test]
    public async Task AssertMultiple()
    {
        var list = new List<int> { 1, 2, 3 };
        
        await using (Assert.Multiple())
        {
            await Assert.That(list).Is.EquivalentTo([1, 2, 3, 4, 5]);
            await Assert.That(list).Has.Count().EqualTo(5);
        }
    }
    
    [Test]
    public async Task NotNull()
    {
        string? item = null;

        await Assert.That(item)
            .IsNotNull()
            .And.IsNotEmpty();
    }
    
    [Test]
    public async Task NotNull2()
    {
        var item = "";

        await Assert.That(item).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task Assert_Multiple_With_Or_Conditions()
    {
        var one = "";
        var two = "Foo bar!";
        
        await using (Assert.Multiple())
        {
            await Assert.That(one).Is.Null().Or.Is.Empty();
            await Assert.That(two).Is.EqualTo("Foo bar").Or.Is.Null();
        }
    }

    private int _retryCount = 0;

    [Test]
    public async Task Throws5()
    {
        await Task.CompletedTask;
        Console.WriteLine(_retryCount);
        throw new Exception();
    }
    
    [Test]
    public async Task Throws6()
    {
        await Task.CompletedTask;
        Console.WriteLine(_retryCount);
        throw new Exception();
    }
    
    [Test]
    public void Throws7()
    {
        Console.WriteLine(_retryCount);
        throw new Exception();
    }
    
    [Test]
    public async Task OneSecond()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Long_String_Not_Equals()
    {
        await Assert.That("ABCDEFGHIJKLMNOOPQRSTUVWXYZ")
            .Is.EqualTo("ABCDEFGHIJKLMNOPQRSTUVWXYZ", StringComparison.Ordinal);
    }
    
    [Test]
    public async Task Short_String_Not_Equals()
    {
        await Assert.That("ABCCDE")
            .Is.EqualTo("ABCDE", StringComparison.Ordinal);
    }

    // [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.OneEnumerable))]
    // public async Task TestDataSourceEnumerable(int value)
    // {
    //     await Assert.That(value).Is.EqualTo(1);
    // }
    
    // [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.OneFailingEnumerable))]
    // [Category("Fail")]
    // public async Task TestDataSourceFailingEnumerable(int value)
    // {
    //     await Assert.That(value).Is.EqualTo(1);
    // }

    // [Arguments]
    // public void No_Arg()
    // {
    // }
    
    // [Arguments()]
    // public void No_Arg2()
    // {
    // }
    
    // [Arguments("")]
    // public void WrongType(int i)
    // {
    // }
    
    public static int One() => 1;
    public static int Two() => 2;
}
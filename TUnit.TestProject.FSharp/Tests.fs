namespace TUnit.TestProject.FSharp

open System
open System.Threading
open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Assertions.FSharp.Operations
open TUnit.Core
open TUnit.TestProject
open TUnit.Assertions.AssertConditions.Throws

type Tests() =

    let _retryCount = 0

    [<Test>]
    [<Category("Pass")>]
    member _.ConsoleOutput() = async {
        Console.WriteLine("Blah!")
        do! check ((Assert.That(TestContext.Current.GetStandardOutput(): string | null).IsEqualTo("Blah!", StringComparison.Ordinal)))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Test1() = async {
        let value = "1"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.LessThan() = async {
        let value = 1
        do! check (Assert.That(value).IsLessThan(2))
    }

    [<Test>]
    [<Category("Fail")>]
    member _.Test2() = async {
        let value = "2"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Test3() = async {
        let value = "1"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Category("Fail")>]
    member _.Test4() = async {
        let value = "2"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Arguments("1")>]
    [<Arguments("2")>]
    [<Arguments("3")>]
    [<Arguments("4")>]
    [<Arguments("5")>]
    [<Category("Fail")>]
    member _.ParameterisedTests1(value: string) = async {
        do! check (Assert.That<string>(value).IsEqualTo("1").And.HasLength().EqualTo(1))
    }

    [<Test>]
    [<Arguments("1")>]
    [<Arguments("2")>]
    [<Arguments("3")>]
    [<Arguments("4")>]
    [<Arguments("5")>]
    [<Category("Fail")>]
    member _.ParameterisedTests2(value: string) = async {
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Skip("Reason1")>]
    [<Category("Skip")>]
    member _.Skip1() = async {
        let value = "1"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<Skip("Reason2")>]
    [<Category("Skip")>]
    member _.Skip2() = async {
        let value = "1"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<CustomSkip>]
    [<Category("Skip")>]
    member _.CustomSkip1() = async {
        let value = "1"
        do! check (Assert.That<string>(value).IsEqualTo("1"))
    }

    [<Test>]
    [<MethodDataSource("One")>]
    [<Category("Pass")>]
    member _.TestDataSource1(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource("One")>]
    [<Category("Pass")>]
    member _.TestDataSource2(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource("Two")>]
    [<Category("Fail")>]
    member _.TestDataSource3(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource("Two")>]
    [<Category("Fail")>]
    member _.TestDataSource4(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource(typeof<TestDataSources>, "One")>]
    [<Category("Pass")>]
    member _.TestDataSource5(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource(typeof<TestDataSources>, "One")>]
    [<Category("Pass")>]
    member _.TestDataSource6(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource(typeof<TestDataSources>, "Two")>]
    [<Category("Pass")>]
    member _.TestDataSource_Wrong(value: int) = async {
        do! check (Assert.That(value).IsNotEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource(typeof<TestDataSources>, "Two")>]
    [<Category("Fail")>]
    member _.TestDataSource7(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<MethodDataSource(typeof<TestDataSources>, "Two")>]
    [<Category("Fail")>]
    member _.TestDataSource8(value: int) = async {
        do! check (Assert.That(value).IsEqualTo(1))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.TestContext1() = async {
        do! check ((Assert.That(TestContext.Current.TestDetails.TestName: string | null).IsEqualTo("TestContext1")))
    }

    [<Test>]
    [<Category("Fail")>]
    member _.TestContext2() = async {
        do! check ((Assert.That(TestContext.Current.TestDetails.TestName: string | null).IsEqualTo("TestContext1")))
    }

    [<Test>]
    [<Category("Fail")>]
    member _.Throws1() = async {
        do! check (Assert.That(fun () -> new string([||])).ThrowsException())
    }

    [<Test>]
    [<Category("Fail")>]
    member _.Throws2() = async {
        do! check (Assert.That(fun () -> async { do! Task.Yield() }).ThrowsException())
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Throws3() = async {
        do! check (Assert.That(fun () -> raise (ApplicationException())).ThrowsException())
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Throws4() = async {
        do! check (Assert.That(fun () -> async { do! Async.Yield(); true }).ThrowsNothing())
    }

    [<Test>]
    [<Timeout(500)>]
    [<Category("Fail")>]
    member _.Timeout1(cancellationToken: CancellationToken) = async {
        do! Task.Delay(TimeSpan.FromSeconds(5.0), cancellationToken) |> Async.AwaitTask
    }

    [<Test>]
    [<Category("Pass")>]
    member _.String_And_Condition() = async {
        do! check (Assert.That<string>("1").IsEqualTo("1").And.HasLength().EqualTo(1))
    }

    [<Test>]
    [<Category("Fail")>]
    member _.String_And_Condition2() = async {
        do! check (Assert.That<string>("1").IsEqualTo("2").And.HasLength().EqualTo(2))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Count1() = async {
        let list = [1; 2; 3]
        do! check (Assert.That(list).IsEquivalentTo([1; 2; 3]).And.HasCount().EqualTo(3))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Single() = async {
        let list = [1]
        let item = list |> Seq.head
        do! check(Assert.That(list).HasSingleItem())
        do! check (Assert.That(item).IsEqualTo(1))
    }

    [<Test>]
    [<Category("Pass")>]
    member _.DistinctItems() = async {
        let list = [1; 2; 3; 4; 5]
        do! check (Assert.That(list).HasDistinctItems())
    }

    [<Test>]
    [<Category("Pass")>]
    member _.Enumerable_NotEmpty() = async {
        let list = [1; 2; 3]
        do! check (Assert.That(list).IsNotEmpty())
    }

    [<Test>]
    [<Category("Fail")>]
    member _.Count2() = async {
        let list = [1; 2; 3]
        do! check (Assert.That(list).IsEquivalentTo([1; 2; 3; 4; 5]).And.HasCount().EqualTo(5))
    }

    [<Test>]
    member _.AssertMultiple() = async {
        let list = [1; 2; 3]
        use _ = Assert.Multiple()
        do! check (Assert.That(list).IsEquivalentTo([1; 2; 3; 4; 5]))
        do! check (Assert.That(list).HasCount().EqualTo(5))
    }

    [<Test>]
    member _.NotNull() = async {
        let item: string | null = null
        do! check (Assert.That(item).IsNotNull().And.IsNotEmpty())
    }

    [<Test>]
    member _.NotNull2() = async {
        let item = ""
        do! check (Assert.That(item).IsNotNull().And.IsNotEmpty())
    }

    [<Test>]
    member _.Assert_Multiple_With_Or_Conditions() = async {
        let one = ""
        let two = "Foo bar!"
        use _ = Assert.Multiple()
        do! check (Assert.That(one).IsNull().Or.IsEmpty())
        do! check (Assert.That<string>(two).IsEqualTo("2").Or.IsNull())
    }

    [<Test>]
    member _.Throws5() = async {
        do! Task.CompletedTask |> Async.AwaitTask
        Console.WriteLine(_retryCount)
        raise (Exception())
    }

    [<Test>]
    member _.Throws6() = async {
        do! Task.CompletedTask |> Async.AwaitTask
        Console.WriteLine(_retryCount)
        raise (Exception())
    }

    [<Test>]
    member _.Throws7() =
        Console.WriteLine(_retryCount)
        raise (Exception())

    [<Test>]
    member _.OneSecond() = async {
        do! Task.Delay(TimeSpan.FromSeconds(1.0)) |> Async.AwaitTask
    }

    [<Test>]
    member _.Long_String_Not_Equals() = async {
        do! check (Assert.That<string>("ABCDEFGHIJKLMNOOPQRSTUVWXYZ").IsNotEqualTo("ABCDEFGHIJKLMNOPQRSTUVWXYZ", StringComparison.Ordinal)) 
    }

    [<Test>]
    member _.Short_String_Not_Equals() = async {
        do! check (Assert.That<string>("ABCCDE").IsNotEqualTo("ABCDE", StringComparison.Ordinal))
    }

    // Data source methods
    static member One() = 1
    static member Two() = 2
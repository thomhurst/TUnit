namespace TUnit.TestProject.FSharp

open System
open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Core

// Equivalent of DependsOnTests3.cs

type DependsOnTests3() =
    static let mutable test1Start = DateTime.MinValue
    static let mutable test2Start = DateTime.MinValue
    static let mutable test3Start = DateTime.MinValue

    [<Test>]
    member _.Test1() : Task = task {
        test1Start <- TestContext.Current.Value.TestStart.Value.DateTime
        do! Task.Delay(TimeSpan.FromSeconds(1.0))
        TestContext.Current.Value.ObjectBag.Add("Test1", box "1")
    }

    [<Test>]
    member _.Test2() : Task = task {
        test2Start <- TestContext.Current.Value.TestStart.Value.DateTime
        do! Task.Delay(TimeSpan.FromSeconds(1.0))
        TestContext.Current.Value.ObjectBag.Add("Test2", box "2")
    }

    [<Test>]
    [<DependsOn("Test1")>]
    [<DependsOn("Test2")>]
    member _.Test3() : Task = task {
        test3Start <- TestContext.Current.Value.TestStart.Value.DateTime
        do! Task.Delay(TimeSpan.FromSeconds(1.0))
        let test1 = TestContext.Current.GetTests("Test1")
        let test2 = TestContext.Current.GetTests("Test2")
        do! Assert.That(test1).HasCount().EqualTo(1)
        do! Assert.That(test2).HasCount().EqualTo(1)
        do! Assert.That(test1.[0].ObjectBag).ContainsKey("Test1")
        do! Assert.That(test2.[0].ObjectBag).ContainsKey("Test2")
    }

    [<After(HookType.Class)>]
    static member AssertStartTimes() : Task = task {
        do! Assert.That(test3Start).IsAfterOrEqualTo(test1Start.AddSeconds(0.9))
        do! Assert.That(test3Start).IsAfterOrEqualTo(test2Start.AddSeconds(0.9))
    }

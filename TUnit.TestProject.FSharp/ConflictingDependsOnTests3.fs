namespace TUnit.TestProject.FSharp

open System
open System.Threading.Tasks
open TUnit.Core

[<UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")>]
type ConflictingDependsOnTests3() =
    [<Test>]
    [<DependsOn("Test5")>]
    member _.Test1() : Task = Task.Delay(TimeSpan.FromSeconds(5.0))

    [<Test>]
    [<DependsOn("Test1")>]
    member _.Test2() : Task = Task.CompletedTask

    [<Test>]
    [<DependsOn("Test2")>]
    member _.Test3() : Task = Task.CompletedTask

    [<Test>]
    [<DependsOn("Test3")>]
    member _.Test4() : Task = Task.CompletedTask

    [<Test>]
    [<DependsOn("Test4")>]
    member _.Test5() : Task = Task.CompletedTask

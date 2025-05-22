namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithMultipleAsyncTestMethods() =
    [<Test>]
    member _.Test1() : Task = Task.CompletedTask
    [<Test>]
    member _.Test2() : Task = Task.CompletedTask

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithAsyncTaskTestMethod() =
    [<Test>]
    member _.Test() : Task = Task.CompletedTask

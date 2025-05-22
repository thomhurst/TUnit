namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithAsyncValueTaskTestMethod() =
    [<Test>]
    member _.Test() : ValueTask = ValueTask()

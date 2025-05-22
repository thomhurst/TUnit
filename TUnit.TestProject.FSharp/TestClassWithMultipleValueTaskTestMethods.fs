namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithMultipleValueTaskTestMethods() =
    [<Test>]
    member _.Test1() : ValueTask = ValueTask()
    [<Test>]
    member _.Test2() : ValueTask = ValueTask()

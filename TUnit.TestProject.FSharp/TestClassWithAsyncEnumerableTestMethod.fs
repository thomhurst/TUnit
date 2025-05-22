namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithAsyncEnumerableTestMethod() =
    [<Test>]
    member _.Test() = seq { yield 1 }

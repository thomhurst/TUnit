namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithAsyncVoidTestMethod() =
    [<Test>]
    member _.Test() = async { return () }

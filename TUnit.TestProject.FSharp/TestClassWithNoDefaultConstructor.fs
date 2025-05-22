namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithNoDefaultConstructor.cs

type TestClassWithNoDefaultConstructor(x: int) =
    [<Test>]
    member _.Test() = ()

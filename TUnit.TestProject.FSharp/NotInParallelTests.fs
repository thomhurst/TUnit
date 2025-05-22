namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of NotInParallelTests.cs

type NotInParallelTests() =
    [<Test>]
    member _.Test() = ()

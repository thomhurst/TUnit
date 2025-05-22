namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of OptionalArgumentsTests.cs

type OptionalArgumentsTests() =
    [<Test>]
    member _.Test(?x: int, ?y: string) =
        ()

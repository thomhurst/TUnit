namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of DynamicallyRegisteredTests.cs

type DynamicallyRegisteredTests() =
    [<Test>]
    member _.Test() = ()

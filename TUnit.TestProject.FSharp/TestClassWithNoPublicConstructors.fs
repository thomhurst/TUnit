namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithNoPublicConstructors.cs

type TestClassWithNoPublicConstructors private () =
    [<Test>]
    member _.Test() = ()

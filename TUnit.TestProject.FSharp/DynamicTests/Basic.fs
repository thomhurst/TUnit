namespace TUnit.TestProject.FSharp.DynamicTests

open System.Threading.Tasks
open TUnit.Core

// Equivalent of DynamicTests/Basic.cs

type Basic() =
    [<Test>]
    member _.Test() = ()

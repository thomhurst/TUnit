namespace TUnit.TestProject.FSharp.DynamicTests

open System.Threading.Tasks
open TUnit.Core

// Equivalent of DynamicTests/Runtime.cs

type Runtime() =
    [<Test>]
    member _.Test() = ()

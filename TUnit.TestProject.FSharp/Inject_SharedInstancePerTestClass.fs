namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of Inject_SharedInstancePerTestClass.cs

type Inject_SharedInstancePerTestClass() =
    [<Test>]
    member _.Test() = ()

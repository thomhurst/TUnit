namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of GenericMethodTests.cs

type GenericMethodTests() =
    [<Test>]
    member _.Test<'T when 'T : (new : unit -> 'T)>() =
        let _ = new 'T()
        ()

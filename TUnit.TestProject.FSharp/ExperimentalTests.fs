namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// F# equivalent of ExperimentalTests.cs

type ExperimentalTests() =
    [<Experimental("Blah")>]
    [<Test>]
    member _.SynchronousTest() = ()

    [<Experimental("Blah")>]
    [<Test>]
    member _.AsynchronousTest() : Task = Task.CompletedTask

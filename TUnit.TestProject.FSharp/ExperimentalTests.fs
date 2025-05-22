namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of ExperimentalTests.cs

type ExperimentalTests() =
    [<Experimental("Blah")>]
    [<Test>]
    member _.SynchronousTest() =
        () // Dummy method

    [<Experimental("Blah")>]
    [<Test>]
    member _.AsynchronousTest() : Task =
        Task.CompletedTask

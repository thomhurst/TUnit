namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of BasicTests.cs

type BasicTests() =
    [<Test>]
    member _.SynchronousTest() =
        () // Dummy method

    [<Test>]
    member _.AsynchronousTest() : Task =
        Task.CompletedTask

    [<Test>]
    member _.ValueTaskAsynchronousTest() : ValueTask =
        ValueTask()

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Assertions.FSharp.Operations
open TUnit.Core

/// Tests to verify F# Async<'T> return types are properly executed
type AsyncTests() =

    /// Static tracker to verify tests actually execute
    static member val private ExecutionCount = 0 with get, set

    [<Test>]
    member _.FSharpAsync_BasicExecution() : Async<unit> = async {
        AsyncTests.ExecutionCount <- 1
        do! Async.Sleep 10
    }

    [<Test>]
    [<DependsOn("FSharpAsync_BasicExecution")>]
    member _.VerifyFSharpAsyncExecuted() =
        // This test depends on the previous one and verifies it actually ran
        if AsyncTests.ExecutionCount = 0 then
            failwith "F# Async test did not execute!"
        Task.CompletedTask

    [<Test>]
    member _.FSharpAsync_WithReturnValue() : Async<int> = async {
        do! Async.Sleep 10
        return 42
    }

    [<Test>]
    member _.FSharpAsync_WithAsyncSleep() : Async<unit> = async {
        // Verify async operations work correctly
        do! Async.Sleep 50
    }

    [<Test>]
    member _.FSharpAsync_CallingTask() : Async<unit> = async {
        // F# Async calling Task-based API
        do! Task.Delay(10) |> Async.AwaitTask
    }

    [<Test>]
    member _.FSharpAsync_WithAssertion() : Async<unit> = async {
        let result = 1 + 1
        do! check (Assert.That(result).IsEqualTo(2))
    }

namespace TUnit.TestProject.FSharp

open System
open System.Threading
open System.Threading.Tasks
open TUnit.Core

[<Category("LongFailures")>]
type LongFailures() =
    static let mutable counter = 0

    [<Repeat(100)>]
    [<Test>]
    member _.LongFailure() : Task = task {
        let delay = Interlocked.Increment(&counter)
        do! Task.Delay(TimeSpan.FromSeconds(float delay))
        raise (Exception())
    }
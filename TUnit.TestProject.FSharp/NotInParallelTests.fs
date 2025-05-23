namespace TUnit.TestProject.FSharp

open System
open System.Collections.Concurrent
open System.Linq
open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Core

// F# equivalent of NotInParallelTests.cs

type DateTimeRange(start: DateTime, end_: DateTime) =
    member _.Start = start
    member _.End = end_
    member this.Overlap(other: DateTimeRange) =
        this.Start <= other.End && other.Start <= this.End

type NotInParallelTests() =
    static let testDateTimeRanges = ConcurrentBag<DateTimeRange>()

    [<After(HookType.Test)>]
    member _.TestOverlaps() : Task = task {
        testDateTimeRanges.Add(DateTimeRange(TestContext.Current.TestStart.Value.DateTime, TestContext.Current.Result.End.Value.DateTime))
        do! NotInParallelTests.AssertNoOverlaps()
    }

    [<Test>]
    [<NotInParallel>]
    [<Repeat(3)>]
    member _.NotInParallel_Test1() : Task =
        Task.Delay(500)

    [<Test>]
    [<NotInParallel>]
    [<Repeat(3)>]
    member _.NotInParallel_Test2() : Task =
        Task.Delay(500)

    [<Test>]
    [<NotInParallel>]
    [<Repeat(3)>]
    member _.NotInParallel_Test3() : Task =
        Task.Delay(500)

    static member private AssertNoOverlaps() : Task = task {
        for testDateTimeRange in testDateTimeRanges do
            let overlaps = testDateTimeRanges
                           |> Seq.filter (fun x -> not (Object.ReferenceEquals(x, testDateTimeRange)))
                           |> Seq.exists (fun x -> x.Overlap(testDateTimeRange))
            do! Assert.That(overlaps).IsFalse()
    }

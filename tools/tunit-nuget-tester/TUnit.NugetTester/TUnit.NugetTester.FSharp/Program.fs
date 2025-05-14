namespace TUnit.NugetTester.FSharp

open TUnit.Core

type Tests() =
    [<Test>]
    member this.Test() =
        printfn "Test method executed"
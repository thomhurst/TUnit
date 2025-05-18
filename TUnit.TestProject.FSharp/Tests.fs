namespace TUnit.TestProject.FSharp

open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Assertions.FSharp.Operations
open TUnit.Core

type Tests() =
    [<Test>]
    member this.Test() =
        printfn "Test method executed"

#if NETCOREAPP
    [<Test>]
    member this.TestAsync() = async {
            let result = 1 + 1
            do! check (Assert.That(result).IsPositive())
        }
#endif
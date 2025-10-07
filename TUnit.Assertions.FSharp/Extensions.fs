namespace TUnit.Assertions.FSharp

open System.Runtime.CompilerServices
open TUnit.Assertions.Core

module Operations =
    [<CustomOperation(MaintainsVariableSpaceUsingBind = true)>]
    let check (assertion: Assertion<'T>) =
        assertion.AssertAsync() |> Async.AwaitTask |> Async.Ignore

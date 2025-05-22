namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of NullableByteArgumentTests.cs

type NullableByteArgumentTests() =
    [<Test>]
    member _.Test(x: byte option) = ()

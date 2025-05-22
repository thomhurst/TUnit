namespace TUnit.TestProject.FSharp

open TUnit.Core

// F# equivalent of ByteArgumentTests.cs

type ByteArgumentTests() =
    [<Test>]
    [<Arguments(1uy)>]
    member _.Normal(b: byte) =
        () // Dummy method

    [<Test>]
    [<Arguments(1uy)>]
    [<Arguments(null)>]
    member _.Nullable(b: byte option) =
        () // Dummy method

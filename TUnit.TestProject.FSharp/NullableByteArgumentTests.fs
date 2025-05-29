namespace TUnit.TestProject.FSharp

open TUnit.Core

// F# equivalent of NullableByteArgumentTests.cs

type NullableByteArgumentTests() =
    [<Test>]
    [<Arguments(1uy)>]
    [<Arguments(null)>]
    member _.Test(someByte: byte option) =
        ()

    [<Test>]
    [<Arguments(1uy, 1uy)>]
    [<Arguments(1uy, null)>]
    member _.Test2(byte1: byte, byte2: byte option) =
        ()

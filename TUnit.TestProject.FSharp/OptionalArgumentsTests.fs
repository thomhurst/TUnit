namespace TUnit.TestProject.FSharp

open TUnit.Core

// F# equivalent of OptionalArgumentsTests.cs

type OptionalArgumentsTests() =
    [<Test>]
    [<Arguments(1)>]
    member _.Test(value: int, ?flag: bool) =
        let flag = defaultArg flag true
        () // Dummy Method

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithMultipleTestAttributes.cs

type TestClassWithMultipleTestAttributes() =
    [<Test>]
    [<Test>]
    member _.Test() = ()

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithTestCaseSource.cs

type TestClassWithTestCaseSource() =
    [<TestCaseSource("TestCases")>]
    member _.Test(x: int) = ()

    static member TestCases = [| 1; 2; 3 |]

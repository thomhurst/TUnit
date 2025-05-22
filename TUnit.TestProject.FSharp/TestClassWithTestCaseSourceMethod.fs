namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithTestCaseSourceMethod.cs

type TestClassWithTestCaseSourceMethod() =
    [<TestCaseSource("GetTestCases")>]
    member _.Test(x: int) = ()

    static member GetTestCases() = [| 1; 2; 3 |]

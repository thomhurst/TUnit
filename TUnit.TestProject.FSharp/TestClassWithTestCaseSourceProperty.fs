namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithTestCaseSourceProperty.cs

type TestClassWithTestCaseSourceProperty() =
    [<TestCaseSource("TestCases")>]
    member _.Test(x: int) = ()

    static member TestCases = [| 1; 2; 3 |]

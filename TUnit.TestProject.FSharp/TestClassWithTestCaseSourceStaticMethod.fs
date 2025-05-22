namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithTestCaseSourceStaticMethod.cs

type TestClassWithTestCaseSourceStaticMethod() =
    [<TestCaseSource("GetTestCases")>]
    member _.Test(x: int) = ()

    static member GetTestCases() = [| 1; 2; 3 |]

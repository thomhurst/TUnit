namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithTestCaseSourceStaticField.cs

type TestClassWithTestCaseSourceStaticField() =
    [<TestCaseSource("TestCases")>]
    member _.Test(x: int) = ()

    static member val TestCases = [| 1; 2; 3 |] with get, set

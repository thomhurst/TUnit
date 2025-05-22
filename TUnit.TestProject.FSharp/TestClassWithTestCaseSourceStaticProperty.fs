namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithTestCaseSourceStaticProperty.cs

type TestClassWithTestCaseSourceStaticProperty() =
    [<TestCaseSource("TestCases")>]
    member _.Test(x: int) = ()

    static member TestCases = [| 1; 2; 3 |]

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithNoTestMethodsButWithTestAttribute.cs

type TestClassWithNoTestMethodsButWithTestAttribute() =
    [<Test>]
    member _.HelperMethod() = ()

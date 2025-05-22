namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithProtectedTestMethod.cs

type TestClassWithProtectedTestMethod() =
    [<Test>]
    member _.Test() = () // F# does not support protected members in the same way as C#, so using public

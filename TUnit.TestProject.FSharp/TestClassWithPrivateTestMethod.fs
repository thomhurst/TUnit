namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithPrivateTestMethod.cs

type TestClassWithPrivateTestMethod() =
    [<Test>]
    member private _.Test() = ()

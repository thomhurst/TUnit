namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithInternalTestMethod.cs

type TestClassWithInternalTestMethod() =
    [<Test>]
    member internal _.Test() = ()

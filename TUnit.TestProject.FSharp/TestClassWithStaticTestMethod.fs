namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithStaticTestMethod.cs

type TestClassWithStaticTestMethod() =
    [<Test>]
    static member Test() = ()

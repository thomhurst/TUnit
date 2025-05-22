namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndPrivateTestMethod() =
    [<Test>]
    member private _.Helper() = ()

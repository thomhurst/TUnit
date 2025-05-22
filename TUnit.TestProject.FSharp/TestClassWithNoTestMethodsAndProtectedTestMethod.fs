namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndProtectedTestMethod() =
    [<Test>]
    member _.Helper() = ()

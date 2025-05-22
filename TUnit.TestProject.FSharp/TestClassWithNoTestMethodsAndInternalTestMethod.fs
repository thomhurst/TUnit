namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndInternalTestMethod() =
    [<Test>]
    member internal _.Helper() = ()

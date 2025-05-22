namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithGenericTestMethod() =
    [<Test>]
    member _.Test<'T>() = ()

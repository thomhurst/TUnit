namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndNoTestMethodsButWithTestAttribute() =
    [<Test>]
    member _.Helper() = ()

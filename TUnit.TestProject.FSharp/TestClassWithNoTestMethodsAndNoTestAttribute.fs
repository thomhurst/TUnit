namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndNoTestAttribute() =
    member _.Helper() = ()

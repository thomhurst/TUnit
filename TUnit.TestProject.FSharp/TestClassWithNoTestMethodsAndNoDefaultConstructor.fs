namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndNoDefaultConstructor(x: int) =
    member _.Helper() = ()

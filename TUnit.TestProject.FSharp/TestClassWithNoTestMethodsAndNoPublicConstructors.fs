namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithNoTestMethodsAndNoPublicConstructors private () =
    member _.Helper() = ()

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithNoTestAttribute.cs

type TestClassWithNoTestAttribute() =
    member _.Test() = ()

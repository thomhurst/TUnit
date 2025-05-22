namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithMultipleConstructorsAndNoTestMethods() =
    new(x: int) as this = TestClassWithMultipleConstructorsAndNoTestMethods() then ()
    member _.Helper() = ()

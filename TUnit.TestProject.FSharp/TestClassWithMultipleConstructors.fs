namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithMultipleConstructors() =
    new(x: int) as this = TestClassWithMultipleConstructors() then ()
    [<Test>]
    member _.Test() = ()

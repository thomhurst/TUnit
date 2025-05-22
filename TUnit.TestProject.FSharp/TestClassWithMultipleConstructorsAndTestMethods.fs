namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithMultipleConstructorsAndTestMethods() =
    new(x: int) as this = TestClassWithMultipleConstructorsAndTestMethods() then ()
    [<Test>]
    member _.Test1() = ()
    [<Test>]
    member _.Test2() = ()

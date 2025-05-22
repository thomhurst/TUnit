namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of TestClassWithMultipleTestMethods.cs

type TestClassWithMultipleTestMethods() =
    [<Test>]
    member _.Test1() = ()
    [<Test>]
    member _.Test2() = ()

namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of InjectedClassDataSourceWithAsyncInitializerTests.cs

type InjectedClassDataSourceWithAsyncInitializerTests() =
    [<Test>]
    member _.Test(x: int) =
        ()

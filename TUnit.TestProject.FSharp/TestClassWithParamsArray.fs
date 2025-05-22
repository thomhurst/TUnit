namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

type TestClassWithParamsArray() =
    [<Test>]
    member _.Test([<ParamArray>] xs: int array) = ()

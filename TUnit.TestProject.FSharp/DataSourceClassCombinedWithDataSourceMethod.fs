namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of DataSourceClassCombinedWithDataSourceMethod.cs

type DataSourceClassCombinedWithDataSourceMethod() =
    [<Test>]
    member _.Test(x: int) = ()

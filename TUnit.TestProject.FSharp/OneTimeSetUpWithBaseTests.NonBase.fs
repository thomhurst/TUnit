namespace TUnit.TestProject.FSharp.OneTimeSetUpWithBaseTests

open System.Threading.Tasks
open TUnit.Core

// Equivalent of OneTimeSetUpWithBaseTests.NonBase.cs

type NonBase() =
    inherit Base1()

    [<Before(HookType.Class)>]
    static member NonBaseOneTimeSetup() : Task =
        Task.CompletedTask

    [<Before(HookType.Test)>]
    member _.NonBaseSetUp() : Task =
        Task.CompletedTask

    [<Test>]
    member _.Test() = ()

namespace TUnit.TestProject.FSharp.OneTimeSetUpWithBaseTests

open System.Threading.Tasks
open TUnit.Core

// Equivalent of OneTimeSetUpWithBaseTests.Base2.cs

type Base2() =
    [<Before(HookType.Class)>]
    static member Base2OneTimeSetup() : Task =
        Task.CompletedTask

    [<Before(HookType.Test)>]
    member _.Base2SetUp() : Task =
        Task.CompletedTask

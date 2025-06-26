namespace TUnit.TestProject.FSharp.OneTimeSetUpWithBaseTests

open System.Threading.Tasks
open TUnit.Core

// Equivalent of OneTimeSetUpWithBaseTests.Base1.cs

type Base1() =
    [<Before(HookType.Class)>]
    static member Base1OneTimeSetup() : Task =
        Task.CompletedTask

    [<Before(HookType.Test)>]
    member _.Base1SetUp() : Task =
        Task.CompletedTask

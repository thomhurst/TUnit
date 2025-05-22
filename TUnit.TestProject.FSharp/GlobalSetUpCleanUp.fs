namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// F# equivalent of GlobalSetUpCleanUp.cs

module GlobalSetUpCleanUp =
    [<Before(HookType.Assembly)>]
    let BlahSetUp() =
        ()

    [<Before(HookType.Assembly)>]
    let BlahSetUp2() =
        ()

    [<After(HookType.Assembly)>]
    let BlahCleanUp() : Task =
        Task.CompletedTask

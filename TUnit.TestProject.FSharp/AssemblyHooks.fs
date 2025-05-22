namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open TUnit.Core

// Equivalent of AssemblyHooks.cs

type AssemblyHooks() =
    [<Before(HookType.Assembly)>]
    static member AssemblySetup() : Task = Task.CompletedTask
    [<After(HookType.Assembly)>]
    static member AssemblyTeardown() : Task = Task.CompletedTask

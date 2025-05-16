namespace TestProject

// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly
open System
open System.Diagnostics.CodeAnalysis
open TUnit.Core

[<assembly: Retry(3)>]
[<assembly: ExcludeFromCodeCoverage>]
do ()

type GlobalHooks() =
    [<Before(HookType.TestSession)>]
    static member SetUp() =
        Console.WriteLine("Or you can define methods that do stuff before...")

    [<After(HookType.TestSession)>]
    static member CleanUp() =
        Console.WriteLine("...and after!")


namespace TUnit.TestProject.FSharp

open System.Threading
open System.Threading.Tasks
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Core
open TUnit.Assertions.FSharp.Operations

// F# equivalent of AssemblyHooks.cs

type AssemblyHooks() =
    static let mutable beforeHook1Calls = 0

    [<Before(HookType.Assembly)>]
    static member BeforeHook1() =
        beforeHook1Calls <- beforeHook1Calls + 1

#if NET
    [<Before(HookType.Assembly)>]
    static member BeforeHook2(context: AssemblyHookContext) = async{
        do! check(Assert.That(context.TestCount).IsPositive())
    }
#endif

    [<Before(HookType.Assembly)>]
    [<Timeout(30000)>]
    static member BeforeHook3(cancellationToken: CancellationToken) =
        ()

#if NET
    [<Before(HookType.Assembly)>]
    [<Timeout(30000)>]
    static member BeforeHook4(context: AssemblyHookContext, cancellationToken: CancellationToken) = async {
        do! check(Assert.That(context.TestCount).IsPositive())
    }
#endif

    [<After(HookType.Assembly)>]
    static member AfterHook1() = async {
        do! check(Assert.That(beforeHook1Calls).IsEqualTo(1))
    }

#if NET
    [<After(HookType.Assembly)>]
    static member AfterHook2(context: AssemblyHookContext) = async {
        do! check(Assert.That(context.TestCount).IsPositive())
    }
#endif

    [<After(HookType.Assembly)>]
    [<Timeout(30000)>]
    static member AfterHook3(cancellationToken: CancellationToken) =
        ()

#if NET
    [<After(HookType.Assembly)>]
    [<Timeout(30000)>]
    static member AfterHook4(context: AssemblyHookContext, cancellationToken: CancellationToken)  = async {
        do! check(Assert.That(context.TestCount).IsPositive())
    }
#endif

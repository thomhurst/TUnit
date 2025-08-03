namespace TUnit.TestProject.FSharp

open System
open Microsoft.Extensions.DependencyInjection
open TUnit
open TUnit.TestProject
open TUnit.Core

[<ClassConstructor(typeof<DependencyInjectionClassConstructor>)>]
[<NotInParallel>]
type ClassConstructorWithEnumerableTest(dummy: DummyReferenceTypeClass) =
    let mutable isDisposed = false

    [<Before(HookType.Test)>]
    member _.Setup() =
        if isDisposed then
            raise (ObjectDisposedException(nameof(ClassConstructorWithEnumerableTest)))

    [<Test>]
    [<MethodDataSource("GetValues")>]
    member _.DoSomething(value: int) =
        // Just use the dummy object that was injected
        // In F#, we don't need to check for null as it won't compile without proper injection
        ()

    static member GetValues() : seq<int> = seq { yield 1; yield 2; yield 3; yield 4 }

    interface IDisposable with
        member _.Dispose() =
            isDisposed <- true
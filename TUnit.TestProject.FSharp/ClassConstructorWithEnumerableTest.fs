namespace TUnit.TestProject.FSharp

open System
open Microsoft.Extensions.DependencyInjection
open TUnit
open TUnit.TestProject
open TUnit.Core

[<ClassConstructor(typeof<DependencyInjectionClassConstructor>)>]
[<NotInParallel>]
type ClassConstructorWithEnumerableTest(services: IServiceProvider) =
    let mutable isDisposed = false

    [<Before(HookType.Test)>]
    member _.Setup() =
        if isDisposed then
            raise (ObjectDisposedException(nameof(ClassConstructorWithEnumerableTest)))

    [<Test>]
    [<MethodDataSource("GetValues")>]
    member _.DoSomething(value: int) =
        ActivatorUtilities.GetServiceOrCreateInstance<DummyReferenceTypeClass>(services) |> ignore

    static member GetValues() : seq<int> = seq { yield 1; yield 2; yield 3; yield 4 }

    interface IDisposable with
        member _.Dispose() =
            isDisposed <- true
namespace TUnit.TestProject.FSharp

open System
open Microsoft.Extensions.DependencyInjection
open TUnit.Core.Interfaces
open TUnit.Core
open System.Threading.Tasks

// F# equivalent of DependencyInjectionClassConstructor.cs

type DependencyInjectionClassConstructor() =
    let mutable scope: AsyncServiceScope option = None
    let serviceProvider =
        ServiceCollection()
            .AddTransient<DummyReferenceTypeClass>()
            .BuildServiceProvider()
    interface IClassConstructor with
        member _.Create(t: Type, classConstructorMetadata: ClassConstructorMetadata) =
            if scope.IsNone then
                scope <- Some(serviceProvider.CreateAsyncScope())
            ActivatorUtilities.GetServiceOrCreateInstance(scope.Value.ServiceProvider, t)
    interface ITestEndEventReceiver with
        member _.OnTestEnd(testContext: AfterTestContext) =
            match scope with
            | Some s -> s.DisposeAsync()
            | None -> ValueTask()
        member _.Order = 0
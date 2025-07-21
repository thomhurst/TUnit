namespace TUnit.TestProject

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open TUnit.Core.Interfaces

type DependencyInjectionClassConstructor() =
    let serviceProvider: IServiceProvider = 
        ServiceCollection()
            .AddTransient<DummyReferenceTypeClass>()
            .BuildServiceProvider()
    let mutable scope : AsyncServiceScope option = None

    interface IClassConstructor with
        member _.Create(typ, _) =
            if scope.IsNone then
                scope <- Some(serviceProvider.CreateAsyncScope())
            let instance = ActivatorUtilities.GetServiceOrCreateInstance(scope.Value.ServiceProvider, typ)
            Task.FromResult(instance)

    interface ITestEndEventReceiver with
        member _.OnTestEnd(_testContext) =
            match scope with
            | Some s -> s.DisposeAsync()
            | None -> ValueTask()
        member _.Order = 0
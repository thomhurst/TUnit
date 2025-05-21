namespace TUnit.TestProject.FSharp

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
            ActivatorUtilities.GetServiceOrCreateInstance(scope.Value.ServiceProvider, typ)

    interface ITestEndEventReceiver with
        member _.OnTestEnd(_testContext) =
            match scope with
            | Some s -> s.DisposeAsync()
            | None -> ValueTask()
        member _.Order = 0
namespace TUnit.TestProject

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open TUnit.Core.Interfaces

type DependencyInjectionClassConstructor() =
    let serviceProvider: IServiceProvider = 
        let services = ServiceCollection()
        services.AddTransient<DummyReferenceTypeClass>() |> ignore
        services.BuildServiceProvider()

    interface IClassConstructor with
        member _.Create(typ, _) =
            let instance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, typ)
            Task.FromResult(instance)
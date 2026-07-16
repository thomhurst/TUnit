namespace TUnit.FSharp

open System
open Microsoft.Extensions.DependencyInjection
open TUnit.Core.Interfaces
open TUnit.Core

type DependencyInjectionClassConstructor() =
    let serviceProvider =
        ServiceCollection()
            // Register your dependencies here, e.g.:
            .AddTransient<DataClass>()
            .BuildServiceProvider()

    interface IClassConstructor with
        member _.Create(typ: Type, _: ClassConstructorMetadata) : Threading.Tasks.Task<obj> =
            Console.WriteLine("You can also control how your test classes are new'd up, giving you lots of power and the ability to utilise tools such as dependency injection")
            Threading.Tasks.Task.FromResult(ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, typ))
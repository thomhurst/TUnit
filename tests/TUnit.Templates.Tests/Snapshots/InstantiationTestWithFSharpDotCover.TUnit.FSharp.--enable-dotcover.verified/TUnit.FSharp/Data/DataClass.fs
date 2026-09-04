namespace TUnit.FSharp

open System
open System.Threading.Tasks
open TUnit.Core.Interfaces

type DataClass() =
    interface IAsyncInitializer with
        member _.InitializeAsync() : Task =
            Console.Out.WriteLineAsync("Classes can be injected into tests, and they can perform some initialisation logic such as starting an in-memory server or a test container.")

    interface IAsyncDisposable with
        member _.DisposeAsync() : ValueTask =
            ValueTask(Console.Out.WriteLineAsync("And when the class is finished with, we can clean up any resources."))


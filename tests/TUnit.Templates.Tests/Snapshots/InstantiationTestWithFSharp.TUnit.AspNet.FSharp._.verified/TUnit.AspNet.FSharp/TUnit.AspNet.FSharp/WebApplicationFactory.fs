namespace TUnit.AspNet.FSharp

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open TUnit.Core.Interfaces

type WebApplicationFactory() =
    inherit WebApplicationFactory<WebApp.Program.Program>()

    interface IAsyncInitializer with
        member this.InitializeAsync() : Task =
            let _ = this.Server
            Task.CompletedTask



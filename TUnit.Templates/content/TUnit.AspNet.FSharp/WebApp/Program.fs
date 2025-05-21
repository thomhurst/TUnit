namespace WebApp
#nowarn "20"
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type Program() =
    static member exitCode = 0

    static member Main(args: string[]) =
        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddControllers()

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.MapGet("/ping", Func<string>(fun () -> "Hello, World!")) |> ignore

        app.Run()

        Program.exitCode

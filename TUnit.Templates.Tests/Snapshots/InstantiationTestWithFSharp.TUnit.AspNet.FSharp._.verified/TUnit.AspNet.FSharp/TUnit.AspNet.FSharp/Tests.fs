namespace TUnit.AspNet.FSharp

open System
open System.Threading.Tasks
open TUnit
open TUnit.Core
open TUnit.Assertions
open TUnit.Assertions.FSharp.Operations
open TUnit.Assertions.Extensions

type Tests() =

    [<ClassDataSource(typeof<WebApplicationFactory>, Shared = [|SharedType.PerTestSession|])>]
    member val public WebApplicationFactory: WebApplicationFactory = Unchecked.defaultof<WebApplicationFactory> with get, set

    [<Test>]
    member this.Test() =
        async {
            let client = this.WebApplicationFactory.CreateClient()
            let! response = client.GetAsync("/ping") |> Async.AwaitTask
            let! stringContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            // F# does not honour [OverloadResolutionPriority], so call the string-specific
            // extension directly to avoid IsEqualTo ambiguity between the string, generic
            // and implicit-conversion overloads.
            do! check (StringEqualsAssertionExtensions.IsEqualTo(Assert.That<string>(stringContent), "Hello, World!"))
        }

    [<Test>]
    member this.GetWeatherForecast() =
        async {
            let client = this.WebApplicationFactory.CreateClient()
            let! response = client.GetAsync("/weatherforecast") |> Async.AwaitTask
            let! stringContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            do! check (Assert.That<string>(stringContent).IsNotNullOrEmpty())
        }

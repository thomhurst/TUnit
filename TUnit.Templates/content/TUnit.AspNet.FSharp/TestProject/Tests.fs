namespace TestProject

open System
open System.Threading.Tasks
open TUnit
open TUnit.Core
open TUnit.Assertions
open TUnit.Assertions.FSharp.Operations
open TUnit.Assertions.Extensions

type Tests() =

    [<ClassDataSource(typeof<WebApplicationFactory>, Shared = SharedType.PerTestSession)>]
    member val public WebApplicationFactory: WebApplicationFactory = Unchecked.defaultof<WebApplicationFactory> with get, set

    [<Test>]
    member this.Test() =
        async {
            let client = this.WebApplicationFactory.CreateClient()
            let! response = client.GetAsync("/ping") |> Async.AwaitTask
            let! stringContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            // Use named argument for the optional parameter to help F# resolve the overload
            do! check (Assert.That(stringContent).IsEqualTo("Hello World"))
        }

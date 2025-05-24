---
sidebar_position: 2
---

# ASP.NET Core Web App/Api

If you want to test a web app, you can utilise the Microsoft.Mvc.Testing packages to wrap your web app within an in-memory test server.

```csharp
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // You can also override certain services here to mock things out

        // Grab a reference to the server
        // This forces it to initialize.
        // By doing it within this method, it's thread safe.
        // And avoids multiple initialisations from different tests if parallelisation is switched on
        _ = Server;

        return Task.CompletedTask;
    }
}
```

This factory can then be injected into your tests, and whether you have one shared instance, or shared per class/assembly, or a new instance each time, is up to you!

The `IAsyncInitializer.InitializeAsync` method that you can see above will be called before your tests are invoked, so you know all the initialisation has already been done for you.

```csharp
public class MyTests
{
    [ClassDataSource<WebAppFactory>(Shared = SharedType.PerTestSession)]
    public required WebAppFactory WebAppFactory { get; init; }
    
    [Test]
    public async Task Test1()
    {
        var client = WebAppFactory.CreateClient();

        var response = await client.GetAsync("/my/endpoint");
        
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

Alternatively, you can use the `[NotInParallel]` attribute to avoid parallelism and multi-initialisation. But you'll most likely be sacrificing test speeds if tests can't run in parallel.

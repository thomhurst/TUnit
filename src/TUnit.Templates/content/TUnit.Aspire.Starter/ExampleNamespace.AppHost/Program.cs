var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddDotnetProject("apiservice", "../ExampleNamespace.ApiService/ExampleNamespace.ApiService.csproj");

builder.AddDotnetProject("webfrontend", "../ExampleNamespace.WebApp/ExampleNamespace.WebApp.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

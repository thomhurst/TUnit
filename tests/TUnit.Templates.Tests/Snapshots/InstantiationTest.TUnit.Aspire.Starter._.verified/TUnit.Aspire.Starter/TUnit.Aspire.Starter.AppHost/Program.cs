var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddDotnetProject("apiservice", "../TUnit.Aspire.Starter.ApiService/TUnit.Aspire.Starter.ApiService.csproj");

builder.AddDotnetProject("webfrontend", "../TUnit.Aspire.Starter.WebApp/TUnit.Aspire.Starter.WebApp.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

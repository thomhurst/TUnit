var builder = DistributedApplication.CreateBuilder(args);

builder.AddParameter("my-secret", secret: true);
builder.AddContainer("nginx-no-healthcheck", "nginx");
builder.AddProject<Projects.TUnit_Aspire_Tests_ApiService>("api-service")
    .WithHttpEndpoint(targetPort: 8080);

builder.Build().Run();

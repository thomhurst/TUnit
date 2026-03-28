var builder = DistributedApplication.CreateBuilder(args);

builder.AddParameter("my-secret", secret: true);
builder.AddContainer("nginx-no-healthcheck", "nginx");

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

// Parameter resource — in Aspire 13.2.0, ParameterResource no longer implements
// IResourceWithoutLifetime, so AspireFixture's GetWaitableResourceNames will try
// to wait for it. This may cause hangs with AllHealthy behavior.
var secret = builder.AddParameter("my-secret", secret: true);

// Container WITHOUT health checks
builder.AddContainer("nginx-no-healthcheck", "nginx");

// Container WITH health check
builder.AddContainer("nginx-with-healthcheck", "nginx")
    .WithHttpEndpoint(targetPort: 80)
    .WithHttpHealthCheck("/");

builder.Build().Run();

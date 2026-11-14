using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddParameter("my-secret", secret: true);
// These resources only serve HTTP and export telemetry over HTTP. They do not need
// Aspire to prepare custom CA trust bundles before DCP can start them (#7616).
builder.AddContainer("nginx-no-healthcheck", "nginx")
    .WithCertificateTrustScope(CertificateTrustScope.None);
builder.AddProject<Projects.TUnit_Aspire_Tests_ApiService>("api-service")
    .WithHttpEndpoint(targetPort: 8080)
    .WithCertificateTrustScope(CertificateTrustScope.None);

builder.Build().Run();

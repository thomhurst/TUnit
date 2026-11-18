using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Runs the API service through <c>AddDotnetProject</c>, which creates a <c>DotnetProjectResource</c>
/// (an executable resource) rather than a <see cref="ProjectResource"/>.
/// </summary>
public class DotnetProjectResourceFixture : AspireFixture<Projects.TUnit_Aspire_Tests_AppHost>
{
    public const string ServiceName = "dotnet-api-service";

    protected override TimeSpan ResourceTimeout => TimeSpan.FromSeconds(120);

    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;

    protected override IEnumerable<string> ResourcesToWaitFor() => [ServiceName];

    protected override IEnumerable<string> ResourcesToRemove() => ["nginx-no-healthcheck", "api-service", "my-secret"];

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        // Relative paths resolve against the AppHost directory.
        builder.AddDotnetProject(ServiceName, "../TUnit.Aspire.Tests.ApiService/TUnit.Aspire.Tests.ApiService.csproj")
            .WithHttpEndpoint(env: "ASPNETCORE_HTTP_PORTS")
            .WithCertificateTrustScope(CertificateTrustScope.None);
    }
}

[ClassDataSource<DotnetProjectResourceFixture>(Shared = SharedType.PerClass)]
[Category("Docker")]
[Category("Integration")]
public class DotnetProjectResourceTests(DotnetProjectResourceFixture fixture)
{
    [Test]
    public async Task Logs_FromDotnetProjectResource_AppearInTestOutput()
    {
        var marker = $"dotnet-project-marker-{Guid.NewGuid():N}";

        var client = fixture.CreateHttpClient(DotnetProjectResourceFixture.ServiceName);
        using var response = await client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}");

        await Assert.That((int)response.StatusCode).IsEqualTo(200);

        var output = await PollForOutput(marker);
        await Assert.That(output).Contains($"[{DotnetProjectResourceFixture.ServiceName}]");
        await Assert.That(output).Contains(marker);
    }

    private static async Task<string> PollForOutput(string expectedMarker)
    {
        var testContext = TestContext.Current!;
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < TimeSpan.FromSeconds(30))
        {
            var output = testContext.GetStandardOutput();
            if (output.Contains(expectedMarker))
            {
                return output;
            }

            await Task.Delay(250);
        }

        return testContext.GetStandardOutput();
    }
}

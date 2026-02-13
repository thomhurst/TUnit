using System.Net.Http.Headers;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Base unauthenticated API client fixture.
/// Nested dependency: injects DistributedAppFixture to create HTTP clients.
/// </summary>
public class ApiClientFixture : IAsyncInitializer
{
    [ClassDataSource<DistributedAppFixture>(Shared = SharedType.PerTestSession)]
    public required DistributedAppFixture App { get; init; }

    public HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Client = App.CreateHttpClient("apiservice");
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        return Task.CompletedTask;
    }
}

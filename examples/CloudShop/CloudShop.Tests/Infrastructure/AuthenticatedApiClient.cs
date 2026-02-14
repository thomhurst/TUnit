using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Base class for authenticated API clients.
/// Nested dependency: injects DistributedAppFixture to create HTTP clients and authenticate.
/// Subclasses define the specific user credentials.
/// </summary>
public abstract class AuthenticatedApiClient : IAsyncInitializer
{
    [ClassDataSource<DistributedAppFixture>(Shared = SharedType.PerTestSession)]
    public required DistributedAppFixture App { get; init; }

    public HttpClient Client { get; private set; } = null!;
    public string Email => UserEmail;
    public string Role => UserRole;

    protected abstract string UserEmail { get; }
    protected abstract string UserPassword { get; }
    protected abstract string UserRole { get; }

    public async Task InitializeAsync()
    {
        Client = App.CreateHttpClient("apiservice");
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Authenticate and set bearer token
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(UserEmail, UserPassword));

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}

/// <summary>
/// Admin API client with full permissions.
/// Shared per test session - all admin tests reuse the same authenticated client.
/// </summary>
public class AdminApiClient : AuthenticatedApiClient
{
    protected override string UserEmail => "admin@cloudshop.test";
    protected override string UserPassword => "Admin123!";
    protected override string UserRole => "admin";
}

/// <summary>
/// Customer API client with limited permissions.
/// Shared per test session - all customer tests reuse the same authenticated client.
/// </summary>
public class CustomerApiClient : AuthenticatedApiClient
{
    protected override string UserEmail => "customer@cloudshop.test";
    protected override string UserPassword => "Customer123!";
    protected override string UserRole => "customer";
}

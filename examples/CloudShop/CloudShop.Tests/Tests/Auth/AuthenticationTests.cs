using System.Net;
using System.Net.Http.Json;
using CloudShop.Shared.Contracts;
using CloudShop.Tests.Assertions;
using CloudShop.Tests.DataSources;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Auth;

/// <summary>
/// Tests authentication endpoints (login, register).
///
/// Showcases:
/// - [ClassDataSource] for unauthenticated API client (no auth needed for login/register)
/// - [MethodDataSource] for various credential scenarios
/// - [Category] for test filtering
/// </summary>
[Category("Integration"), Category("Auth")]
public class AuthenticationTests
{
    [ClassDataSource<ApiClientFixture>(Shared = SharedType.PerTestSession)]
    public required ApiClientFixture Api { get; init; }

    [Test]
    public async Task Valid_Login_Returns_Token()
    {
        var response = await Api.Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin@cloudshop.test", "Admin123!"));

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.AccessToken).IsNotNull();
        await Assert.That(token.Email).IsEqualTo("admin@cloudshop.test");
        await Assert.That(token.Role).IsEqualTo("admin");
        await Assert.That(token.ExpiresAt).IsGreaterThan(DateTime.UtcNow);
    }

    [Test]
    [MethodDataSource(typeof(UserDataSources), nameof(UserDataSources.InvalidCredentials))]
    public async Task Invalid_Login_Returns_Unauthorized(LoginRequest credentials)
    {
        var response = await Api.Client.PostAsJsonAsync("/api/auth/login", credentials);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [MethodDataSource(typeof(UserDataSources), nameof(UserDataSources.NewUsers))]
    public async Task Can_Register_New_User(RegisterRequest registration)
    {
        var response = await Api.Client.PostAsJsonAsync("/api/auth/register", registration);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.Email).IsEqualTo(registration.Email);
        await Assert.That(token.Role).IsEqualTo("customer"); // New users are always customers
    }

    [Test]
    public async Task Duplicate_Registration_Returns_Conflict()
    {
        // Try to register with an existing email
        var response = await Api.Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("admin@cloudshop.test", "NewPassword123!", "Duplicate User"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }
}

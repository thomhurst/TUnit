using System.Net;
using TUnit.Mocks;

namespace TUnit.Mocks.Http.Tests;

public class MockHttpClientFactoryTests
{
    private const string ClientName = "test-client";

    [Test]
    public async Task CreateClient_ReturnsConfiguredResponseFromDefaultHandler()
    {
        var factory = Mock.HttpClientFactory().WithBaseAddress("http://localhost");
        factory.Handler.OnGet("/api/users").RespondWithJson("""[{"id":1}]""");

        using var client = factory.CreateClient(ClientName);
        var response = await client.GetAsync("/api/users");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).Contains("id");
    }

    [Test]
    public async Task CreateClient_SurvivesUsingBlockDisposal()
    {
        var factory = Mock.HttpClientFactory().WithBaseAddress("http://localhost");
        factory.Handler.OnAnyRequest().Respond(HttpStatusCode.OK);

        for (var i = 0; i < 3; i++)
        {
            using var client = factory.CreateClient(ClientName);
            var response = await client.GetAsync("/ping");
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        await Assert.That(factory.Handler.Requests).Count().IsEqualTo(3);
    }

    [Test]
    public async Task CreateClient_ReturnsFreshInstanceEachCall()
    {
        var factory = Mock.HttpClientFactory();

        var a = factory.CreateClient(ClientName);
        var b = factory.CreateClient(ClientName);

        await Assert.That(a).IsNotSameReferenceAs(b);
    }

    [Test]
    public async Task WithHandler_UsesNamedHandlerForMatchingName()
    {
        var usersHandler = Mock.HttpHandler();
        usersHandler.OnGet("/").RespondWithJson("""{"who":"users"}""");

        var ordersHandler = Mock.HttpHandler();
        ordersHandler.OnGet("/").RespondWithJson("""{"who":"orders"}""");

        var factory = Mock.HttpClientFactory()
            .WithBaseAddress("http://localhost")
            .WithHandler("users", usersHandler)
            .WithHandler("orders", ordersHandler);

        using var usersClient = factory.CreateClient("users");
        var usersBody = await (await usersClient.GetAsync("/")).Content.ReadAsStringAsync();

        using var ordersClient = factory.CreateClient("orders");
        var ordersBody = await (await ordersClient.GetAsync("/")).Content.ReadAsStringAsync();

        await Assert.That(usersBody).Contains("users");
        await Assert.That(ordersBody).Contains("orders");
        await Assert.That(usersHandler.Requests).Count().IsEqualTo(1);
        await Assert.That(ordersHandler.Requests).Count().IsEqualTo(1);
    }

    [Test]
    public async Task HandlerFor_FallsBackToDefaultWhenNameNotRegistered()
    {
        var factory = Mock.HttpClientFactory();

        await Assert.That(factory.HandlerFor("unregistered")).IsSameReferenceAs(factory.Handler);
    }

    [Test]
    public async Task CreateClient_NameIsCaseInsensitive()
    {
        var namedHandler = Mock.HttpHandler();
        var factory = Mock.HttpClientFactory().WithHandler("Users", namedHandler);

        await Assert.That(factory.HandlerFor("USERS")).IsSameReferenceAs(namedHandler);
        await Assert.That(factory.HandlerFor("users")).IsSameReferenceAs(namedHandler);
    }

    [Test]
    public async Task Verify_TracksRequestsAcrossMultipleClientLifetimes()
    {
        var factory = Mock.HttpClientFactory().WithBaseAddress("http://localhost");
        factory.Handler.OnGet("/api/data").Respond(HttpStatusCode.OK);

        using (var c1 = factory.CreateClient(ClientName))
        {
            await c1.GetAsync("/api/data");
        }
        using (var c2 = factory.CreateClient(ClientName))
        {
            await c2.GetAsync("/api/data");
        }

        factory.Handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/data"), Times.Exactly(2));
    }
}

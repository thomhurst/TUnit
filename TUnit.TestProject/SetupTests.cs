using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class Base1
{
    [OneTimeSetUp]
    public static async Task Setup1()
    {
    }
}

public class Base2 : Base1
{
    [OneTimeSetUp]
    public static async Task Setup2()
    {
    }
}

public class Base3 : Base2
{
    [OneTimeSetUp]
    public static async Task Setup3()
    {
    }
}

public class SetupTests : Base3
{
    private static WebApplication _app = null!;
    private static string _serverAddress = null!;
    private HttpResponseMessage? _response;

    
    private static int OneTimeSetUpExecutionCount = 0;
    private static int OneTimeCleanUpExecutionCount = 0;

    [OneTimeSetUp]
    public static async Task SetUpLocalWebServer()
    {
        try
        {
            Interlocked.Increment(ref OneTimeSetUpExecutionCount);
            var builder = WebApplication.CreateBuilder();
            _app = builder.Build();

            _app.MapGet("/ping", ([FromQuery] string testName) => 
            $"Hello {testName}!");

            await _app.StartAsync();
            _serverAddress = _app.Services.GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Last();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($$"""
                                  OneTimeSetUp Count: {{OneTimeSetUpExecutionCount}}
                                  OneTimeCleanUp Count: {{OneTimeCleanUpExecutionCount}}
                                  """, e);
        }
    }

    [OneTimeCleanUp]
    public static async Task StopServer()
    {
        Interlocked.Increment(ref OneTimeCleanUpExecutionCount);

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
    
    [BeforeEachTest]
    public async Task Setup()
    {
        _response = await new HttpClient().GetAsync($"{_serverAddress}/ping/?testName={TestContext.Current?.TestInformation.TestName}");
    }

    [Test]
    public async Task TestServerResponse1()
    {
        await Assert.That(_response?.StatusCode).Is.Not.Null().And.Is.EqualTo(HttpStatusCode.OK);
        
        await Assert.That(await _response!.Content.ReadAsStringAsync())
            .Is.EqualTo("Hello TestServerResponse1!");
    }
    
    [Test]
    public async Task TestServerResponse2()
    {
        await Assert.That(_response?.StatusCode).Is.Not.Null()
            .And.Is.EqualTo(HttpStatusCode.OK);

        await Assert.That(await _response!.Content.ReadAsStringAsync())
            .Is.EqualTo("Hello TestServerResponse2!");
    }
}
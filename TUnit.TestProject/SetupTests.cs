using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class Base1
{
    [Before(Class)]
    public static async Task BeforeAll1()
    {
        await Task.CompletedTask;
    }
    
    [Before(Test)]
    public async Task BeforeEach1()
    {
        await Task.CompletedTask;
    }
}

public class Base2 : Base1
{
    [Before(Class)]
    public static async Task BeforeAll2()
    {
        await Task.CompletedTask;
    }
    
    [Before(Test)]
    public async Task BeforeEach2()
    {
        await Task.CompletedTask;
    }
}

public class Base3 : Base2
{
    [Before(Class)]
    public static async Task BeforeAll3()
    {
        await Task.CompletedTask;
    }
    
    [Before(Test)]
    public async Task BeforeEach3()
    {
        await Task.CompletedTask;
    }
}

public class SetupTests : Base3
{
    private static WebApplication _app = null!;
    private static string _serverAddress = null!;
    private HttpResponseMessage? _response;
    
    private static int _beforeAllTestsInClassExecutionCount;
    private static int _afterAllTestsInClassExecutionCount;

    [Before(Class)]
    public static async Task SetUpLocalWebServer()
    {
        try
        {
            Interlocked.Increment(ref _beforeAllTestsInClassExecutionCount);
            var builder = WebApplication.CreateBuilder();
            _app = builder.Build();

            _app.MapGet("/ping", context => context.Response.WriteAsync($"Hello {context.Request.Query["testName"]}!"));

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
                                  Before(Class) Count: {{_beforeAllTestsInClassExecutionCount}}
                                  After(Class) Count: {{_afterAllTestsInClassExecutionCount}}
                                  """, e);
        }
    }

    [After(Class)]
    public static async Task StopServer()
    {
        Interlocked.Increment(ref _afterAllTestsInClassExecutionCount);

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
    
    [Before(Test)]
    public async Task Setup()
    {
        _response = await new HttpClient().GetAsync($"{_serverAddress}/ping/?testName={TestContext.Current?.TestDetails.TestName}");
    }

    [After(Test)]
    public void Dispose()
    {
        _response?.Dispose();
    }

    [Test]
    public async Task TestServerResponse1()
    {
        await Assert.That(_response?.StatusCode).IsNotNull().And.IsEquatableOrEqualTo(HttpStatusCode.OK);
        
        await Assert.That(await _response!.Content.ReadAsStringAsync())
            .IsEqualTo("Hello TestServerResponse1!");
    }
    
    [Test]
    public async Task TestServerResponse2()
    {
        await Assert.That(_response?.StatusCode).IsNotNull()
            .And.IsEquatableOrEqualTo(HttpStatusCode.OK);

        await Assert.That(await _response!.Content.ReadAsStringAsync())
            .IsEqualTo("Hello TestServerResponse2!");
    }
}
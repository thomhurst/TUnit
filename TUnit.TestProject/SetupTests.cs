﻿using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
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
    
    [Before(EachTest)]
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
    
    [Before(EachTest)]
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
    
    [Before(EachTest)]
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
    
    private static int _beforeAllTestsInClassExecutionCount = 0;
    private static int _afterAllTestsInClassExecutionCount = 0;

    [Before(Class)]
    public static async Task SetUpLocalWebServer()
    {
        try
        {
            Interlocked.Increment(ref _beforeAllTestsInClassExecutionCount);
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
    
    [Before(EachTest)]
    public async Task Setup()
    {
        _response = await new HttpClient().GetAsync($"{_serverAddress}/ping/?testName={TestContext.Current?.TestDetails.TestName}");
    }

    [After(EachTest)]
    public void Dispose()
    {
        _response?.Dispose();
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
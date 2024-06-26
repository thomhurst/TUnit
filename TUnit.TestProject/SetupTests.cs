﻿using System.Net;
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
    [BeforeAllTestsInClass]
    public static async Task BeforeAll1()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach1()
    {
        await Task.CompletedTask;
    }
}

public class Base2 : Base1
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll2()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach2()
    {
        await Task.CompletedTask;
    }
}

public class Base3 : Base2
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll3()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
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
    
    private static int BeforeAllTestsInClassExecutionCount = 0;
    private static int AfterAllTestsInClassExecutionCount = 0;

    [BeforeAllTestsInClass]
    public static async Task SetUpLocalWebServer()
    {
        try
        {
            Interlocked.Increment(ref BeforeAllTestsInClassExecutionCount);
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
                                  BeforeAllTestsInClass Count: {{BeforeAllTestsInClassExecutionCount}}
                                  AfterAllTestsInClass Count: {{AfterAllTestsInClassExecutionCount}}
                                  """, e);
        }
    }

    [AfterAllTestsInClass]
    public static async Task StopServer()
    {
        Interlocked.Increment(ref AfterAllTestsInClassExecutionCount);

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
    
    [BeforeEachTest]
    public async Task Setup()
    {
        _response = await new HttpClient().GetAsync($"{_serverAddress}/ping/?testName={TestContext.Current?.TestInformation.TestName}");
    }

    [AfterEachTest]
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
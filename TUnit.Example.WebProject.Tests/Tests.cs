using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace TUnit.Example.WebProject.Tests;

public class Tests : TestBase
{
    [Test]
    public async Task Test1()
    {
        var response = await Client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
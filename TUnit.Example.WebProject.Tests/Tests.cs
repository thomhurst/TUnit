using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TUnit.Core;

namespace TUnit.Example.WebProject.Tests;

public class Tests : TestBase
{
    [Test]
    public async Task Test()
    {
        var response = await Client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
using System.Net;
using Shouldly;

namespace TUnit.Example.WebProject.Tests;

public class Tests : TestBase
{
    [Test]
    public async Task Test1()
    {
        var response = await Client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
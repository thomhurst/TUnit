#if NET
using System.Net;
using System.Net.Http.Json;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.CustomAssertions;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    public async Task Test()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(new ProblemDetails
            {
                Title = "Invalid Authentication Token",
                Detail = "No token provided"
            }, ProblemDetailsSourceGenerationContext.Default.ProblemDetails)
        };
        
        await Assert.That(response)
            .IsProblemDetails()
            .And
            .HasTitle("Invalid Authentication Token")
            .And
            .HasDetail("No token provided");
    }
}
#endif
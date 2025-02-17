namespace TestProject.Tests;

public class WebTests
{
    [Test]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Act
        var httpClient = (GlobalHooks.App ?? throw new NullReferenceException()).CreateHttpClient("webfrontend");
        if (GlobalHooks.ResourceNotificationService != null)
            await GlobalHooks.ResourceNotificationService
                .WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var response = await httpClient.GetAsync("/");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task IncreaseCounterTest()
    {
        // Act
        var httpClient = (GlobalHooks.App ?? throw new NullReferenceException()).CreateHttpClient("webfrontend");
        if (GlobalHooks.ResourceNotificationService != null)
            await GlobalHooks.ResourceNotificationService
                .WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var response = await httpClient.GetAsync("/");
    }
}

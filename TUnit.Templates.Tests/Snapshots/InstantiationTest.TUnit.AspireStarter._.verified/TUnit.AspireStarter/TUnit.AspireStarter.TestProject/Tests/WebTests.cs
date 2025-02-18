using Microsoft.Playwright;
using TUnit.Playwright;

namespace TUnit.AspireStarter.TestProject.Tests;

public class WebTests: PageTest
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
        var url = (GlobalHooks.App ?? throw new NullReferenceException()).GetEndpoint("webfrontend");
        if (GlobalHooks.ResourceNotificationService != null)
            await GlobalHooks.ResourceNotificationService
                .WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        await Page.GotoAsync(url.AbsoluteUri);
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Counter" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Click me" }).ClickAsync();
        await Expect(Page.GetByText("Current count:")).ToContainTextAsync("1");
    }
}

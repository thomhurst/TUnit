using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.Playwright;

namespace TUnit.Playwright;

public class TwoContextFixtureTests
{
    [ClassDataSource<PageFixture>]
    public required PageFixture Alice { get; init; }

    [ClassDataSource<PageFixture>]
    public required PageFixture Bob { get; init; }

    [Test]
    public async Task Two_Pages_Have_Isolated_Storage_But_Share_Browser()
    {
        await NavigateToStorageOrigin(Alice);
        await NavigateToStorageOrigin(Bob);

        await Alice.Page.EvaluateAsync("() => localStorage.setItem('user', 'alice')");

        var aliceUser = await Alice.Page.EvaluateAsync<string?>("() => localStorage.getItem('user')");
        var bobUser = await Bob.Page.EvaluateAsync<string?>("() => localStorage.getItem('user')");

        await Assert.That(aliceUser).IsEqualTo("alice");
        await Assert.That(bobUser).IsNull();

        await Assert.That(Alice.Page).IsNotSameReferenceAs(Bob.Page);
        await Assert.That(Alice.ContextFixture.Context).IsNotSameReferenceAs(Bob.ContextFixture.Context);
        await Assert.That(Alice.ContextFixture.BrowserFixture.Browser)
            .IsSameReferenceAs(Bob.ContextFixture.BrowserFixture.Browser);
    }

    private static async Task NavigateToStorageOrigin(PageFixture fixture)
    {
        await fixture.Page.RouteAsync("https://tunit-playwright.test/**", route =>
            route.FulfillAsync(new()
            {
                Body = "<!doctype html><title>TUnit Playwright</title>",
                ContentType = "text/html",
            }));

        await fixture.Page.GotoAsync("https://tunit-playwright.test/");
    }
}

using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class ContextTest : BrowserTest
{
    public IBrowserContext Context { get; private set; } = null!;

    public virtual BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        return new()
        {
            Locale = "en-US",
            ColorScheme = ColorScheme.Light,
        };
    }

    [Before(HookType.Test)]
    public async Task ContextSetup(TestContext testContext)
    {
        Context = await NewContext(ContextOptions(testContext)).ConfigureAwait(false);
    }
}

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

    [Before(HookType.Test, "", 0)]
    public async Task ContextSetup(TestContext testContext)
    {
        if (Browser == null)
        {
            throw new InvalidOperationException($"Browser is not initialized. This may indicate that {nameof(BrowserTest)}.{nameof(BrowserSetup)} did not execute properly.");
        }
        
        Context = await NewContext(ContextOptions(testContext)).ConfigureAwait(false);
    }
}

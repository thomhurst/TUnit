using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class ContextTest : BrowserTest
{
    public IBrowserContext Context { get; private set; } = null!;

    public virtual BrowserNewContextOptions ContextOptions()
    {
        return new()
        {
            Locale = "en-US",
            ColorScheme = ColorScheme.Light,
        };
    }

    [Before(HookType.Test)]
    public async Task ContextSetup()
    {
        Context = await NewContext(ContextOptions()).ConfigureAwait(false);
    }
}

using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class PageTest : ContextTest
{
    public PageTest()
    {
    }

    public PageTest(BrowserTypeLaunchOptions options) : base(options)
    {
    }

    public IPage Page { get; private set; } = null!;

    [Before(HookType.Test, "", 0)]
    public async Task PageSetup()
    {
        if (Context == null)
        {
            throw new InvalidOperationException($"Browser context is not initialized. This may indicate that {nameof(ContextTest)}.{nameof(ContextSetup)} did not execute properly.");
        }

        Page = await Context.NewPageAsync().ConfigureAwait(false);
    }
}

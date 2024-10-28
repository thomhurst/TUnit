using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class PageTest : ContextTest
{
    public IPage Page { get; private set; } = null!;

    [Before(HookType.Test)]
    public async Task PageSetup()
    {
        Page = await Context.NewPageAsync().ConfigureAwait(false);
    }
}

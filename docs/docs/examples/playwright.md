# Playwright

There is a NuGet package to help with Playwright: `TUnit.Playwright`

Once that is installed, a test can be as simple as:

```csharp
public class Tests : PageTest
{
    [Test]
    public async Task Test()
    {
        await Page.GotoAsync("https://www.github.com/thomhurst/TUnit");
    }
}
```

By inheriting from `PageTest`, the base class handles setting up and disposing your playwright objects for you.

The following properties are available to use:

- `Page`
- `Context`
- `Browser`
- `Playwright`

You can override the `BrowserName` to control which browser you want to launch.

The possible values are:
- chromium
- firefox
- webkit

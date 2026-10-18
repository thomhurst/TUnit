using System.Diagnostics;

namespace TUnit.Playwright;

public class Hooks
{
    [Before(TestSession)]
    public static void InstallPlaywright()
    {
        if (Debugger.IsAttached)
        {
            Environment.SetEnvironmentVariable("PWDEBUG", "1");
        }

        // Tests use Chromium by default (PlaywrightTest.BrowserName). Add "firefox" or "webkit"
        // here if you override BrowserName, so unused browsers are not downloaded.
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            // On Linux, browsers also need system libraries: run `playwright install --with-deps` once.
            throw new InvalidOperationException($"Playwright browser installation failed with exit code {exitCode}.");
        }
    }
}

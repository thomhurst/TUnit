using System.Diagnostics;

namespace TestProject;

public class Hooks
{
    [Before(TestSession)]
    public static void InstallPlaywright()
    {
        if (Debugger.IsAttached)
        {
            Environment.SetEnvironmentVariable("PWDEBUG", "1");
        }

        var exitCode = Microsoft.Playwright.Program.Main(["install"]);
        if (exitCode != 0)
        {
            // On Linux, browsers also need system libraries: run `playwright install --with-deps` once.
            throw new InvalidOperationException($"Playwright browser installation failed with exit code {exitCode}.");
        }
    }
}

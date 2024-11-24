using System.Diagnostics;

namespace TUnit.Playwright.Tests;

public class Hooks
{
    [Before(Assembly)]
    public static void InstallPlaywright()
    {
        if (Debugger.IsAttached)
        {
            Environment.SetEnvironmentVariable("PWDEBUG", "1");
        }
        
        Microsoft.Playwright.Program.Main(["install"]);
    }
}
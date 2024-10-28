namespace TUnit.Playwright.Tests;

public class Tests : PageTest
{
    [Before(TestSession)]
    public static void InstallPlaywright()
    {
        Microsoft.Playwright.Program.Main(["install"]);
    }
    
    [Test]
    public async Task Test()
    {
        await Page.GotoAsync("https://www.github.com/thomhurst/TUnit");
    }
}
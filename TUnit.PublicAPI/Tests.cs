using System.Reflection;
using PublicApiGenerator;

namespace TUnit.PublicAPI;

public class Tests
{
    [Test]
    public Task Core_Library_Has_No_API_Changes()
    {
        return VerifyPublicApi(typeof(TestAttribute).Assembly);
    }
    
    [Test]
    public Task Engine_Library_Has_No_API_Changes()
    {
        return VerifyPublicApi(typeof(Engine.Services.TUnitRunner).Assembly);
    }
    
    [Test]
    public Task Assertions_Library_Has_No_API_Changes()
    {
        return VerifyPublicApi(typeof(Assertions.Assert).Assembly);
    }
    
    [Test]
    public Task Playwright_Library_Has_No_API_Changes()
    {
        return VerifyPublicApi(typeof(Playwright.PageTest).Assembly);
    }

    private Task VerifyPublicApi(Assembly assembly)
    {
        var publicApi = assembly.GeneratePublicApi();

        return Verify(publicApi).UniqueForTargetFrameworkAndVersion(assembly);   
    }
}
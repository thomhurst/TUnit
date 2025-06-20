using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PublicApiGenerator;

namespace TUnit.PublicAPI;

public partial class Tests
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

    private async Task VerifyPublicApi(Assembly assembly)
    {
        var publicApi = assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes =
            [
                "System.Reflection.AssemblyMetadataAttribute"
            ]
        });

        await Verify(publicApi)
            .AddScrubber(sb => Scrub(sb))
            .ScrubLinesWithReplace(x => x.Replace("\r\n", "\n"))
            .ScrubLinesWithReplace(line =>
            {
                if (line.Contains("public static class AssemblyLoader"))
                {
                    return "public static class AssemblyLoader_Guid";
                }

                return line;
            })
            .OnVerifyMismatch(async (pair, message, verify) =>
            {
                var received = await FilePolyfill.ReadAllTextAsync(pair.ReceivedPath);
                var verified = await FilePolyfill.ReadAllTextAsync(pair.VerifiedPath);
                
                // Better diff message since original one is too large
                await Assert.That(Scrub(received)).IsEqualTo(Scrub(verified));
            })
            .UniqueForTargetFrameworkAndVersion(assembly);
    }
    
    private StringBuilder Scrub(StringBuilder text)
    {
        var newText = UrlRegex().Replace(text.ToString(), string.Empty);
        return new StringBuilder(newText);
    }
    
    private string Scrub(string text)
    {
        return Scrub(new StringBuilder(text.Replace("\r\n", "\n"))).ToString();
    }


#if NET
    [GeneratedRegex(@"((http|https):\/\/)?(www\.)?([a-zA-Z0-9-]+\.[a-zA-Z]{2,})(\/[a-zA-Z0-9-._~:\/?#[\]@!$&'()*+,;=]*)?")]
    public static partial Regex UrlRegex();
#else
    public static Regex UrlRegex() =>
        new Regex(
            @"((http|https):\/\/)?(www\.)?([a-zA-Z0-9-]+\.[a-zA-Z]{2,})(\/[a-zA-Z0-9-._~:\/?#[\]@!$&'()*+,;=]*)?");
#endif
}
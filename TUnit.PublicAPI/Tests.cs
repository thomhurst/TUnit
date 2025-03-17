using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using PublicApiGenerator;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

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
        var publicApi = assembly.GeneratePublicApi();

        await Verify(publicApi)
            .AddScrubber(sb => Scrub(sb))
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
        var stringBuilder = text
            .Replace(".git\"", "\"");
        
        var scrubbed = FilePathRegex().Replace(stringBuilder.ToString(), "<FilePath>");
        
        return new StringBuilder(scrubbed);
    }
    
    private string Scrub(string text)
    {
        return Scrub(new StringBuilder(text)).ToString();
    }

    private static Regex FilePathRegex()
    {
#if NET
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new Regex(@"(\/{1,2}(?:[^\\\/:*?""<>|\r\n]+\/{1,2})*[^\\\/:*?""<>|\r\n]*)");
        }
#endif
        
        return new Regex(@"([a-zA-Z]:\\{1,2}(?:[^\\\/:*?""<>|\r\n]+\\b)*[^\\\/:*?""<>|\r\n]*)");
    }
}
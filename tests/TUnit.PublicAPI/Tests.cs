using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PublicApiGenerator;

namespace TUnit.PublicAPI;

public partial class Tests
{
    [Test]
    public Task Core_Library_Has_No_API_Changes()
        => VerifyPublicApi(typeof(TestAttribute).Assembly);

    [Test]
    public Task Assertions_Library_Has_No_API_Changes()
        => VerifyPublicApi(typeof(Assertions.Assert).Assembly);

    [Test]
    public Task Playwright_Library_Has_No_API_Changes()
        => VerifyPublicApi(typeof(Playwright.PageTest).Assembly);

#if NET
    [Test]
    public Task Should_Library_Has_No_API_Changes()
        => VerifyPublicApi(typeof(Assertions.Should.ShouldExtensions).Assembly);

    [Test]
    public Task OpenTelemetry_Library_Has_No_API_Changes()
        => VerifyPublicApi(typeof(TUnit.OpenTelemetry.TUnitOpenTelemetry).Assembly);
#endif

    private async Task VerifyPublicApi(Assembly assembly)
    {
        var publicApi = assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes =
            [
                "System.Reflection.AssemblyMetadataAttribute"
            ]
        });

        await VerifyTUnit.Verify(publicApi)
            .AddScrubber(sb =>
            {
                var replaced = ScrubSourcePaths(sb.ToString());
                sb.Clear();
                sb.Append(replaced);
                return sb;
            })
            .AddScrubber(Scrub)
            .AddScrubber(sb => new StringBuilder(sb.ToString().Replace("\r\n", "\n")))
            .ScrubLinesWithReplace(x => x.Replace("\r\n", "\n"))
            .ScrubLinesWithReplace(line =>
            {
                return line.Contains("public static class AssemblyLoader")
                    ? "public static class AssemblyLoader_Guid"
                    : line;
            })
            .ScrubFilePaths()
            .OnVerifyMismatch(async (pair, message, verify) =>
            {
                var received = await File.ReadAllTextAsync(pair.ReceivedPath);
                var verified = await File.ReadAllTextAsync(pair.VerifiedPath);

                // Better diff message since original one is too large
                await Assert.That(Scrub(received)).IsEqualTo(Scrub(verified));
            })
            .UniqueForTargetFrameworkAndVersion(assembly);
    }

    /// <summary>
    /// Scrubs source path literals (hook attributes' <c>[CallerFilePath]</c> argument) before the URL regex mangles
    /// them. CI builds embed deterministic "/_/..." paths; local builds embed the full checkout path, which the
    /// generator wraps into "..." + "..." pieces once it exceeds 80 characters. Only a rooted ".cs" path followed by
    /// the <c>[CallerLineNumber]</c> argument matches, so public string values such as "/v1" are left alone.
    /// </summary>
    internal static string ScrubSourcePaths(string text) => SourcePathRegex().Replace(text, @"""PATH_SCRUBBED""");

    private StringBuilder Scrub(StringBuilder text)
    {
        var newText = UrlRegex().Replace(text.ToString(), string.Empty)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\\r\\n", "\\n")
            .Replace("\\r", "\\n");

        text.Clear();
        text.Append(newText);
        return text;
    }

    private string Scrub(string text)
    {
        var sb = new StringBuilder(text);
        Scrub(sb);
        return sb.ToString();
    }


    private const string SourcePathPattern =
        @"""(?:/|[A-Za-z]:(?:\\\\|/))[^""\r\n]*(?:""[ \t]*\+[ \t]*\r?\n[ \t]*""[^""\r\n]*)*\.cs""(?=,[ \t]*\d+[,)])";

#if NET
    [GeneratedRegex(SourcePathPattern)]
    private static partial Regex SourcePathRegex();

    [GeneratedRegex(@"((http|https):\/\/)?(www\.)?([a-zA-Z0-9-]+\.[a-zA-Z]{2,})(\/[a-zA-Z0-9-._~:\/?#[\]@!$&'()*+,;=]*)?")]
    public static partial Regex UrlRegex();
#else
    private static Regex SourcePathRegex() => new(SourcePathPattern);

    public static Regex UrlRegex() =>
        new(
            @"((http|https):\/\/)?(www\.)?([a-zA-Z0-9-]+\.[a-zA-Z]{2,})(\/[a-zA-Z0-9-._~:\/?#[\]@!$&'()*+,;=]*)?");
#endif
}

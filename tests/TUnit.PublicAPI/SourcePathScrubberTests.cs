namespace TUnit.PublicAPI;

public class SourcePathScrubberTests
{
    [Test]
    [Arguments("[.Before(., \"/_/TUnit.OpenTelemetry/File.cs\", 39, Order=-2147482648)]")]
    [Arguments("[.After(., \"/_/F.cs\", 7)]")]
    [Arguments("[.Before(., \"C:\\\\a\\\\File.cs\", 39)]")]
    [Arguments("[.Before(., \"C:/a/File.cs\", 39)]")]
    public async Task Scrubs_Single_Line_Caller_File_Paths(string line)
    {
        var scrubbed = Tests.ScrubSourcePaths(line);

        await Assert.That(scrubbed).Contains("\"PATH_SCRUBBED\"");
        await Assert.That(scrubbed).DoesNotContain("File.cs");
    }

    [Test]
    public async Task Scrubs_Wrapped_Unix_Caller_File_Path()
    {
        var text = "[.Before(., \"/home/user/src/TUnit/TUnit.OpenTelemetry/AutoStart/\" +\n" +
                   "            \"TUnitOpenTelemetryAutoStart.cs\", 39, Order=-2147482648)]";

        var scrubbed = Tests.ScrubSourcePaths(text);

        await Assert.That(scrubbed).IsEqualTo("[.Before(., \"PATH_SCRUBBED\", 39, Order=-2147482648)]");
    }

    [Test]
    public async Task Scrubs_Wrapped_Windows_Caller_File_Path()
    {
        var text = "[.After(., \"C:\\\\Users\\\\dev\\\\source\\\\repos\\\\TUnit\\\\TUnit.OpenTelemetry\\\\\" +\r\n" +
                   "            \"AutoStart\\\\TUnitOpenTelemetryAutoStart.cs\", 61)]";

        var scrubbed = Tests.ScrubSourcePaths(text);

        await Assert.That(scrubbed).IsEqualTo("[.After(., \"PATH_SCRUBBED\", 61)]");
    }

    [Test]
    public async Task Scrubs_Wrapped_Windows_Caller_File_Path_With_Forward_Slashes()
    {
        var text = "[.After(., \"C:/Users/dev/source/repos/TUnit/TUnit.OpenTelemetry/\" +\r\n" +
                   "            \"AutoStart/TUnitOpenTelemetryAutoStart.cs\", 61)]";

        var scrubbed = Tests.ScrubSourcePaths(text);

        await Assert.That(scrubbed).IsEqualTo("[.After(., \"PATH_SCRUBBED\", 61)]");
    }

    [Test]
    [Arguments("public const string Prefix = \"/v1\";")]
    [Arguments("[Route(\"/v1\", 2)]")]
    [Arguments("public const string Template = \"/api/File.cs\";")]
    [Arguments("[Description(\"C:\\\\docs\\\\readme.md\")]")]
    [Arguments("[Description(\"File.cs\", 39)]")]
    [Arguments("[Route(\"/api/File.cs\", 2)]")]
    [Arguments("[Route(\"C:/api/File.cs\", 2)]")]
    public async Task Leaves_Unrelated_Strings_Alone(string line)
    {
        await Assert.That(Tests.ScrubSourcePaths(line)).IsEqualTo(line);
    }
}

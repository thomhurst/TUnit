using System.Xml.Linq;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Engine.Reporters;
using TUnit.Engine.Xml;

namespace TUnit.UnitTests;

public class JUnitXmlWriterTests
{
    private static TestNodeUpdateMessage PassedTest(params IProperty[] properties) => new(
        new SessionUid("session"),
        new TestNode
        {
            Uid = new TestNodeUid("test-1"),
            DisplayName = "Adds",
            Properties = new PropertyBag([PassedTestNodeStateProperty.CachedInstance, .. properties]),
        });

    private static TestFileLocationProperty Location(string filePath, int line) =>
        new(filePath, new LinePositionSpan(new LinePosition(line, 5), new LinePosition(line + 4, 6)));

    private static XElement SingleTestCase(string xml) => XDocument.Parse(xml).Descendants("testcase").Single();

    [Test]
    public async Task Writes_UnobservedTaskExceptions_To_Suite_SystemErr()
    {
        var xml = JUnitXmlWriter.GenerateXml([PassedTest()], filter: null,
            [new InvalidOperationException("leaked task")]);

        await Assert.That(xml).Contains("<system-err>Unobserved task exception: System.InvalidOperationException: leaked task");
        await Assert.That(xml.IndexOf("<system-err>", StringComparison.Ordinal))
            .IsGreaterThan(xml.LastIndexOf("<testcase", StringComparison.Ordinal));
    }

    [Test]
    public async Task Omits_Suite_SystemErr_Without_UnobservedTaskExceptions()
    {
        var xml = JUnitXmlWriter.GenerateXml([PassedTest()], filter: null);

        await Assert.That(xml).DoesNotContain("system-err");
    }

    [Test]
    [Arguments(@"C:\src\shop\tests\MathTests.cs", null, null, @"C:\src\shop\tests\MathTests.cs")]
    [Arguments("/home/runner/work/shop/shop/tests/MathTests.cs", "/home/runner/work/shop/shop", "octo/shop", "tests/MathTests.cs")]
    [Arguments(@"D:\a\shop\shop\tests\MathTests.cs", @"D:\a\shop\shop", "octo/shop", "tests/MathTests.cs")]
    [Arguments("/_/tests/MathTests.cs", "/builds/octo/shop", "octo/shop", "tests/MathTests.cs")]
    public async Task Writes_TestCase_File_And_Line_From_Test_Location(string filePath, string? workspace, string? repo, string expectedFile)
    {
        var sourceControl = new SourceControlContext(null, null, null, repo, workspace, null);

        var xml = JUnitXmlWriter.GenerateXml([PassedTest(Location(filePath, 12))], filter: null, sourceControl: sourceControl);

        var testCase = SingleTestCase(xml);
        await Assert.That(testCase.Attribute("file")?.Value).IsEqualTo(expectedFile);
        await Assert.That(testCase.Attribute("line")?.Value).IsEqualTo("12");
    }

    [Test]
    [Arguments(null, 0)]
    [Arguments("Unknown", 0)]
    [Arguments("", 7)]
    public async Task Omits_TestCase_File_And_Line_Without_Known_Location(string? filePath, int line)
    {
        var test = filePath is null ? PassedTest() : PassedTest(Location(filePath, line));

        var xml = JUnitXmlWriter.GenerateXml([test], filter: null);

        var testCase = SingleTestCase(xml);
        await Assert.That(testCase.Attribute("file")).IsNull();
        await Assert.That(testCase.Attribute("line")).IsNull();
    }
}

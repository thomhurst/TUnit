using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Engine.Xml;

namespace TUnit.UnitTests;

public class JUnitXmlWriterTests
{
    private static TestNodeUpdateMessage PassedTest() => new(
        new SessionUid("session"),
        new TestNode
        {
            Uid = new TestNodeUid("test-1"),
            DisplayName = "Adds",
            Properties = new PropertyBag(PassedTestNodeStateProperty.CachedInstance),
        });

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
}

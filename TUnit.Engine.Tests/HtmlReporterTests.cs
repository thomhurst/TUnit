#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Shouldly;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Tests;

public class HtmlReporterTests
{
    private sealed class MockExtension : IExtension
    {
        public string Uid => "MockExtension";
        public string DisplayName => "Mock";
        public string Version => "1.0.0";
        public string Description => "Mock Extension";
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }

    [Test]
    public void HtmlReporter_Implements_IDataProducer()
    {
        var reporter = new HtmlReporter(new MockExtension());
        reporter.ShouldBeAssignableTo<Microsoft.Testing.Platform.Extensions.Messages.IDataProducer>();
    }

    [Test]
    public void HtmlReporter_DataTypesProduced_Contains_SessionFileArtifact()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var producer = (Microsoft.Testing.Platform.Extensions.Messages.IDataProducer)reporter;
        producer.DataTypesProduced.ShouldContain(typeof(SessionFileArtifact));
    }
}

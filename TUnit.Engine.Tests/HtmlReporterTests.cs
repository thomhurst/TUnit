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

    private sealed class CapturingMessageBus : Microsoft.Testing.Platform.Messages.IMessageBus
    {
        public List<(Microsoft.Testing.Platform.Extensions.Messages.IDataProducer Producer, Microsoft.Testing.Platform.Extensions.Messages.IData Data)> Published = [];

        public Task PublishAsync(Microsoft.Testing.Platform.Extensions.Messages.IDataProducer dataProducer, Microsoft.Testing.Platform.Extensions.Messages.IData value)
        {
            Published.Add((dataProducer, value));
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task PublishArtifactAsync_Publishes_SessionFileArtifact_When_SessionContext_Set_And_File_Exists()
    {
        // Arrange
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        reporter.SetMessageBus(bus);

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            await reporter.PublishArtifactAsync(tempFile, CancellationToken.None);

            // Assert
            bus.Published.Count.ShouldBe(1);
            var artifact = bus.Published[0].Data.ShouldBeOfType<Microsoft.Testing.Platform.Extensions.Messages.SessionFileArtifact>();
            artifact.FileInfo.FullName.ShouldBe(new FileInfo(tempFile).FullName);
            artifact.DisplayName.ShouldBe("HTML Test Report");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task PublishArtifactAsync_Is_NoOp_When_SessionContext_Not_Set()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var tempFile = Path.GetTempFileName();
        try
        {
            // Should not throw
            await reporter.PublishArtifactAsync(tempFile, CancellationToken.None);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task PublishArtifactAsync_Is_NoOp_When_File_Does_Not_Exist()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        reporter.SetMessageBus(bus);

        await reporter.PublishArtifactAsync("/nonexistent/path/report.html", CancellationToken.None);

        bus.Published.Count.ShouldBe(0);
    }
}

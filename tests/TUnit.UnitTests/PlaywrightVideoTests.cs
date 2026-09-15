using Microsoft.Playwright;
using NSubstitute;
using TUnit.Core;
using TUnit.Playwright;

namespace TUnit.UnitTests;

public class PlaywrightVideoTests
{
    [Test]
    [Arguments(typeof(ContextTest))]
    [Arguments(typeof(PageTest))]
    public async Task RecordVideoConfiguresSupportedTests(Type classType)
    {
        using var scope = new VideoTestScope(classType: classType);
        var attribute = new RecordVideoAttribute(scope.Directory, 640, 480);

        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var options = new ContextTest().ContextOptions(scope.Context);

        await Assert.That(options.RecordVideoDir).IsEqualTo(scope.Directory);
        await Assert.That(options.ViewportSize!.Width).IsEqualTo(640);
        await Assert.That(options.ViewportSize.Height).IsEqualTo(480);
    }

    [Test]
    public async Task RecordVideoRejectsFixtureBasedTestsDuringDiscovery()
    {
        using var scope = new VideoTestScope(classType: typeof(FixtureBasedTest));

        await Assert.That(async () => await new RecordVideoAttribute().OnTestDiscovered(
                new DiscoveredTestContext("Video", scope.Context)))
            .Throws<InvalidOperationException>()
            .WithMessageContaining("ContextFixture or PageFixture");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TeardownAttachesPagesOpenedBeforeOrDuringContextClose(bool duringClose)
    {
        using var scope = new VideoTestScope();
        var page = scope.CreatePage();
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });

        if (duringClose)
        {
            scope.BrowserContext.CloseAsync().Returns(_ =>
            {
                scope.OpenPage(page);
                return Task.CompletedTask;
            });
        }
        else
        {
            scope.OpenPage(page);
            await page.CloseAsync();
            // Closed pages are absent from BrowserContext.Pages throughout this test.
        }

        await scope.Test.BrowserTearDown(scope.Context);

        var artifact = scope.Context.Output.Artifacts.Single();
        await Assert.That(artifact.File.Name).IsEqualTo("Video.webm");
        await Assert.That(File.ReadAllText(artifact.File.FullName)).IsEqualTo("recording");
    }

    [Test]
    public async Task TeardownStillAttachesVideosAndClosesOtherContextsAfterCloseFailure()
    {
        using var scope = new VideoTestScope();
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });
        scope.OpenPage(scope.CreatePage());
        scope.BrowserContext.CloseAsync().Returns(Task.FromException(new InvalidOperationException("close failed")));
        var secondContext = Substitute.For<IBrowserContext>();
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(secondContext);
        await scope.Test.NewContext(new BrowserNewContextOptions());

        await Assert.That(() => scope.Test.BrowserTearDown(scope.Context)).Throws<AggregateException>();

        await secondContext.Received(1).CloseAsync();
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(1);
    }

    [Test]
    public async Task LatePageEventFromPreviousContextDoesNotLeakIntoNextAttempt()
    {
        using var scope = new VideoTestScope();
        EventHandler<IPage>? pendingHandler = null;
        scope.BrowserContext.When(context => context.Page += Arg.Any<EventHandler<IPage>>())
            .Do(call => pendingHandler = call.Arg<EventHandler<IPage>>());
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });
        await scope.Test.BrowserTearDown(scope.Context);

        var nextContext = Substitute.For<IBrowserContext>();
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(nextContext);
        scope.Test.Browser = scope.Browser;
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });

        // Simulate an event invocation that captured the old delegate before unsubscribe.
        pendingHandler!(scope.BrowserContext, scope.CreatePage());
        nextContext.Page += Raise.Event<EventHandler<IPage>>(nextContext, scope.CreatePage());
        await scope.Test.BrowserTearDown(scope.Context);

        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ContextWithoutRecordingDoesNotSubscribeToPages()
    {
        using var scope = new VideoTestScope();
        await scope.Test.NewContext(new BrowserNewContextOptions());

        scope.BrowserContext.DidNotReceive().Page += Arg.Any<EventHandler<IPage>>();
        await scope.Test.BrowserTearDown(scope.Context);
        await scope.BrowserContext.Received(1).CloseAsync();
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("Video\0Name", "Video_Name.webm")]
    [Arguments("\0\0", "__.webm")]
    [Arguments("", "test.webm")]
    [Arguments("Video Name", "Video-Name.webm")]
    public async Task VideoNamesReplaceInvalidCharacters(string testName, string expected)
    {
        using var scope = new VideoTestScope(testName);
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });
        scope.OpenPage(scope.CreatePage());

        await scope.Test.BrowserTearDown(scope.Context);

        await Assert.That(scope.Context.Output.Artifacts.Single().File.Name).IsEqualTo(expected);
    }

    [Test]
    public async Task MultiplePagesPreserveExistingVideosAndRetryAttemptNames()
    {
        using var scope = new VideoTestScope();
        scope.Context.CurrentRetryAttempt = 1;
        var existing = Path.Combine(scope.Directory, "Video-attempt2-1.webm");
        File.WriteAllText(existing, "existing recording");
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });
        scope.OpenPage(scope.CreatePage());
        scope.OpenPage(scope.CreatePage());

        await scope.Test.BrowserTearDown(scope.Context);

        await Assert.That(scope.Context.Output.Artifacts.Select(x => x.File.Name).ToArray())
            .IsEquivalentTo(new[] { "Video-attempt2-1-2.webm", "Video-attempt2-2.webm" });
        await Assert.That(File.ReadAllText(existing)).IsEqualTo("existing recording");
    }

    private sealed class FixtureBasedTest
    {
        [ClassDataSource<PageFixture>]
        public required PageFixture Fixture { get; init; }
    }

    private sealed class VideoTestScope : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "TUnit-video-tests", Guid.NewGuid().ToString("N"));
        public TestContext Context { get; }
        public IBrowser Browser { get; } = Substitute.For<IBrowser>();
        public IBrowserContext BrowserContext { get; } = Substitute.For<IBrowserContext>();
        public BrowserTest Test { get; }

        public VideoTestScope(string testName = "Video", Type? classType = null)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var current = TestContext.Current!;
            Context = new TestContext(testName, current.ServiceProvider, current.ClassContext,
                new TestBuilderContext { TestMetadata = current.Metadata.TestDetails.MethodMetadata }, CancellationToken.None);
            Context.Metadata.TestDetails = new TestDetails([])
            {
                TestId = Context.Id, TestName = testName, ClassType = classType ?? typeof(PageTest),
                MethodName = testName, ClassInstance = this, TestMethodArguments = [], TestClassArguments = [],
                MethodMetadata = current.Metadata.TestDetails.MethodMetadata, ReturnType = typeof(void),
                AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
            };
            Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(BrowserContext);
            BrowserContext.Pages.Returns(Array.Empty<IPage>());
            Test = new BrowserTest { Browser = Browser };
        }

        public IPage CreatePage()
        {
            var sourcePath = Path.Combine(Directory, $"{Guid.NewGuid():N}.webm");
            File.WriteAllText(sourcePath, "recording");
            var video = Substitute.For<IVideo>();
            video.PathAsync().Returns(sourcePath);
            var page = Substitute.For<IPage>();
            page.Video.Returns(video);
            return page;
        }

        public void OpenPage(IPage page) => BrowserContext.Page += Raise.Event<EventHandler<IPage>>(BrowserContext, page);

        public void Dispose()
        {
            Context.RemoveFromRegistry();
            Context.Dispose();
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}

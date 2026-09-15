using Microsoft.Playwright;
using NSubstitute;
using NSubstitute.Extensions;
using TUnit.Core;
using TUnit.Core.Interfaces;
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
    public async Task RecordVideoSupportsFixtureBasedTestsDuringDiscovery()
    {
        using var scope = new VideoTestScope(classType: typeof(FixtureBasedTest));

        var attribute = new RecordVideoAttribute();
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        await Assert.That(PlaywrightContextOptions.Recording(scope.Context)).IsSameReferenceAs(attribute);
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
    public async Task TeardownRejectsNewContextsWhileClosing()
    {
        using var scope = new VideoTestScope();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        var closure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.BrowserContext.CloseAsync().Returns(closure.Task);
        var teardown = scope.Test.BrowserTearDown(scope.Context);

        try
        {
            await Assert.That(teardown.IsCompleted).IsFalse();
            await Assert.That(() => scope.Test.NewContext(new BrowserNewContextOptions()))
                .Throws<InvalidOperationException>().WithMessageContaining("after teardown has started");
            await scope.Browser.Received(1).NewContextAsync(Arg.Any<BrowserNewContextOptions>());
        }
        finally
        {
            closure.TrySetResult();
            await teardown;
        }

        await Assert.That(() => scope.Test.NewContext(new BrowserNewContextOptions())).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TeardownWaitsForPendingContextCreation(bool creationFails)
    {
        using var scope = new VideoTestScope();
        var creation = new TaskCompletionSource<IBrowserContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(creation.Task);
        var pendingContext = scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });
        var page = scope.CreatePage();
        scope.BrowserContext.CloseAsync().Returns(_ =>
        {
            scope.OpenPage(page);
            return Task.CompletedTask;
        });
        var teardown = scope.Test.BrowserTearDown(scope.Context);

        try
        {
            await Assert.That(teardown.IsCompleted).IsFalse();
        }
        finally
        {
            if (creationFails)
            {
                creation.TrySetException(new InvalidOperationException("creation failed"));
                await Assert.That(async () => await pendingContext).Throws<InvalidOperationException>();
            }
            else
            {
                creation.TrySetResult(scope.BrowserContext);
                await pendingContext;
            }

            await teardown;
        }

        await scope.BrowserContext.Received(creationFails ? 0 : 1).CloseAsync();
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(creationFails ? 0 : 1);
        if (!creationFails)
        {
            scope.BrowserContext.Received(1).Page -= Arg.Any<EventHandler<IPage>>();
        }
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
    [Arguments("CON", "_CON.webm")]
    [Arguments("prn", "_prn.webm")]
    [Arguments("Aux", "_Aux.webm")]
    [Arguments("NUL.tar.gz", "_NUL.tar.gz.webm")]
    [Arguments("COM1", "_COM1.webm")]
    [Arguments("LPT9.test", "_LPT9.test.webm")]
    [Arguments("COM¹", "_COM¹.webm")]
    [Arguments("LPT²", "_LPT².webm")]
    [Arguments("COM³", "_COM³.webm")]
    [Arguments("CONIN$", "_CONIN$.webm")]
    [Arguments("CONOUT$", "_CONOUT$.webm")]
    [Arguments("COM10", "COM10.webm")]
    [Arguments("Console", "Console.webm")]
    public async Task VideoNamesReplaceInvalidCharacters(string testName, string expected)
    {
        using var scope = new VideoTestScope(testName);
        await scope.Test.NewContext(new BrowserNewContextOptions { RecordVideoDir = scope.Directory });
        scope.OpenPage(scope.CreatePage());

        await scope.Test.BrowserTearDown(scope.Context);

        await Assert.That(scope.Context.Output.Artifacts.Single().File.Name).IsEqualTo(expected);
        await Assert.That(File.ReadAllText(scope.Context.Output.Artifacts.Single().File.FullName)).IsEqualTo("recording");
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

    [Test]
    public async Task CompositionRecordsEachAttemptAndInitializesOnlyOnceWithinAttempt()
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var fixture = scope.CreateFixture();
        var contexts = new List<IBrowserContext>();
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(_ =>
        {
            var context = Substitute.For<IBrowserContext>();
            context.NewPageAsync().Returns(_ =>
            {
                var page = scope.CreatePage();
                context.Page += Raise.Event<EventHandler<IPage>>(context, page);
                return page;
            });
            contexts.Add(context);
            return context;
        });

        await Task.WhenAll(ObjectInitializer.InitializeAsync(fixture).AsTask(), ObjectInitializer.InitializeAsync(fixture).AsTask());
        var firstPage = fixture.Page;
        await Assert.That(ObjectInitializer.IsInitialized(fixture)).IsTrue();
        await attribute.OnTestEnd(scope.Context);
        await attribute.OnTestEnd(scope.Context);
        scope.Context.CurrentRetryAttempt = 1;
        await ObjectInitializer.InitializeAsync(fixture);
        await Assert.That(fixture.Page).IsNotSameReferenceAs(firstPage);
        await attribute.OnTestEnd(scope.Context);
        await fixture.DisposeAsync();
        await fixture.ContextFixture.DisposeAsync();

        await Assert.That(contexts.Count).IsEqualTo(2);
        foreach (var context in contexts)
        {
            await context.Received(1).CloseAsync();
        }

        await Assert.That(scope.Context.Output.Artifacts.Select(x => x.File.Name).ToArray())
            .IsEquivalentTo(new[] { "Video.webm", "Video-attempt2.webm" });
    }

    [Test]
    public async Task CompositionWithoutRecordingKeepsFixtureLifetimeAcrossRetries()
    {
        using var scope = new VideoTestScope();
        var fixture = scope.CreateFixture();
        await ObjectInitializer.InitializeAsync(fixture);
        var page = fixture.Page;
        scope.Context.CurrentRetryAttempt = 1;
        await ObjectInitializer.InitializeAsync(fixture);

        await Assert.That(fixture.Page).IsSameReferenceAs(page);
        await scope.Browser.Received(1).NewContextAsync(Arg.Any<BrowserNewContextOptions>());
        scope.BrowserContext.DidNotReceive().Page += Arg.Any<EventHandler<IPage>>();
        await fixture.DisposeAsync();
        await fixture.ContextFixture.DisposeAsync();
    }

    [Test]
    public async Task CompositionClosesContextWhenPageSetupFails()
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var fixture = scope.CreateFixture();
        scope.BrowserContext.Configure().NewPageAsync().Returns(Task.FromException<IPage>(new InvalidOperationException("page failed")));

        await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();
        await attribute.OnTestEnd(scope.Context);
        await fixture.ContextFixture.DisposeAsync();

        await scope.BrowserContext.Received(1).CloseAsync();
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CompositionCachesSetupFailureWithinAttemptAndRetriesOnNextAttempt()
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var fixture = scope.CreateFixture();
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>())
            .Returns(Task.FromException<IBrowserContext>(new InvalidOperationException("context failed")), Task.FromResult(scope.BrowserContext));

        await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();
        await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();
        await scope.Browser.Received(1).NewContextAsync(Arg.Any<BrowserNewContextOptions>());
        await attribute.OnTestEnd(scope.Context);
        scope.Context.CurrentRetryAttempt = 1;
        await ObjectInitializer.InitializeAsync(fixture);
        await attribute.OnTestEnd(scope.Context);

        await Assert.That(scope.Context.Output.Artifacts.Single().File.Name).IsEqualTo("Video-attempt2.webm");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CompositionRejectsFixtureReuseBetweenTests(bool firstTestRecords)
    {
        using var first = new VideoTestScope("First");
        var attribute = new RecordVideoAttribute(first.Directory);
        if (firstTestRecords)
        {
            await attribute.OnTestDiscovered(new DiscoveredTestContext("First", first.Context));
        }

        var fixture = first.CreateFixture();
        await ObjectInitializer.InitializeAsync(fixture);
        using var second = new VideoTestScope("Second");
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Second", second.Context));
        await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture))
            .Throws<InvalidOperationException>().WithMessageContaining("private to one test");

        await attribute.OnTestEnd(first.Context);
        await fixture.DisposeAsync();
        await fixture.ContextFixture.DisposeAsync();
        await Assert.That(second.Context.Output.Artifacts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RecordingUsesCapturedOwnerAndAttemptDuringDisposal()
    {
        using var owner = new VideoTestScope("Owner");
        var attribute = new RecordVideoAttribute(owner.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Owner", owner.Context));
        var fixture = owner.CreateFixture();
        await ObjectInitializer.InitializeAsync(fixture);
        owner.Context.CurrentRetryAttempt = 5;
        using var other = new VideoTestScope("Other");
        await attribute.OnTestEnd(owner.Context);

        await Assert.That(owner.Context.Output.Artifacts.Single().File.Name).IsEqualTo("Owner.webm");
        await Assert.That(other.Context.Output.Artifacts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RecordingPreservesCustomContextOptions()
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory, 640, 480);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var original = new BrowserNewContextOptions { Locale = "fr-FR", BaseURL = "https://example.com", IgnoreHTTPSErrors = true };
        var fixture = new CustomContextFixture(original) { BrowserFixture = scope.CreateFixture().ContextFixture.BrowserFixture };
        await ObjectInitializer.InitializeAsync(fixture);
        await scope.Browser.Received(1).NewContextAsync(Arg.Is<BrowserNewContextOptions>(options =>
            options.Locale == "fr-FR" && options.BaseURL == original.BaseURL && options.IgnoreHTTPSErrors == true &&
            options.RecordVideoDir == scope.Directory && options.ViewportSize!.Width == 640));
        await Assert.That(original.RecordVideoDir).IsNull();
        await Assert.That(original.ViewportSize).IsNull();
        await attribute.OnTestEnd(scope.Context);
    }

    private sealed class CustomContextFixture(BrowserNewContextOptions options) : ContextFixture
    {
        protected override BrowserNewContextOptions GetContextOptions() => options;
    }

    [Test]
    public async Task OptionOnlyRecordingKeepsSharedFixtureLifetimeWithoutAttributingVideos()
    {
        using var first = new VideoTestScope("First");
        var options = new BrowserNewContextOptions { RecordVideoDir = first.Directory };
        var fixture = new CustomContextFixture(options) { BrowserFixture = first.CreateFixture().ContextFixture.BrowserFixture };
        TraceScopeRegistry.RegisterFromDataSource(new ClassDataSourceAttribute<ContextFixture> { Shared = SharedType.PerTestSession }, [fixture]);
        await ObjectInitializer.InitializeAsync(fixture);
        var firstPage = await fixture.Context.NewPageAsync();
        var firstPath = await firstPage.Video!.PathAsync();
        first.Context.CurrentRetryAttempt = 1;
        await ObjectInitializer.InitializeAsync(fixture);

        using var second = new VideoTestScope("Second");
        await ObjectInitializer.InitializeAsync(fixture);
        var secondPage = await fixture.Context.NewPageAsync();
        var secondPath = await secondPage.Video!.PathAsync();
        await fixture.DisposeAsync();

        await first.Browser.Received(1).NewContextAsync(Arg.Is<BrowserNewContextOptions>(value => value.RecordVideoDir == first.Directory));
        await first.BrowserContext.Received(1).CloseAsync();
        first.BrowserContext.DidNotReceive().Page += Arg.Any<EventHandler<IPage>>();
        await Assert.That(first.Context.Output.Artifacts.Count).IsEqualTo(0);
        await Assert.That(second.Context.Output.Artifacts.Count).IsEqualTo(0);
        await Assert.That(File.ReadAllText(firstPath)).IsEqualTo("recording");
        await Assert.That(File.ReadAllText(secondPath)).IsEqualTo("recording");
    }

    [Test]
    public async Task PageFixturePreservesManuallyInitializedContext()
    {
        using var scope = new VideoTestScope();
        var fixture = scope.CreateFixture();
        await fixture.ContextFixture.InitializeAsync();
        await fixture.InitializeAsync();

        await scope.Browser.Received(1).NewContextAsync(Arg.Any<BrowserNewContextOptions>());
        await fixture.DisposeAsync();
        await fixture.ContextFixture.DisposeAsync();
    }

    [Test]
    public async Task CompositionFlushesRecordingWhenPageCloseFails()
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var fixture = scope.CreateFixture();
        await ObjectInitializer.InitializeAsync(fixture);
        fixture.Page.CloseAsync().Returns(Task.FromException(new InvalidOperationException("page close failed")));

        await Assert.That(async () => await attribute.OnTestEnd(scope.Context)).Throws<AggregateException>();

        await scope.BrowserContext.Received(1).CloseAsync();
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CancelledInitializationStillClosesResourcesCreatedLater()
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var fixture = scope.CreateFixture();
        var creation = new TaskCompletionSource<IBrowserContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(creation.Task);
        using var cancellation = new CancellationTokenSource();
        var initialize = ObjectInitializer.InitializeAsync(fixture, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.That(async () => await initialize).Throws<OperationCanceledException>();
        var cleanup = attribute.OnTestEnd(scope.Context).AsTask();

        try
        {
            await Assert.That(cleanup.IsCompleted).IsFalse();
        }
        finally
        {
            creation.TrySetResult(scope.BrowserContext);
            await cleanup;
        }

        await scope.BrowserContext.Received(1).CloseAsync();
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(SharedType.PerClass)]
    [Arguments(SharedType.PerTestSession)]
    public async Task RecordingRejectsDeclaredSharedContextScope(SharedType shared)
    {
        using var scope = new VideoTestScope();
        var attribute = new RecordVideoAttribute(scope.Directory);
        await attribute.OnTestDiscovered(new DiscoveredTestContext("Video", scope.Context));
        var fixture = scope.CreateFixture();
        TraceScopeRegistry.RegisterFromDataSource(new ClassDataSourceAttribute<ContextFixture> { Shared = shared }, [fixture.ContextFixture]);

        await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture))
            .Throws<InvalidOperationException>().WithMessageContaining("SharedType.None");
        await attribute.OnTestEnd(scope.Context);
        await scope.Browser.DidNotReceive().NewContextAsync(Arg.Any<BrowserNewContextOptions>());
    }

    private sealed class VideoTestScope : IDisposable
    {
        private readonly TestContext? _previousContext = TestContext.Current;
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
            TestContext.Current = Context;
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

        public PageFixture CreateFixture()
        {
            BrowserContext.NewPageAsync().Returns(_ =>
            {
                var page = CreatePage();
                OpenPage(page);
                return page;
            });
            return new PageFixture
            {
                ContextFixture = new ContextFixture
                {
                    BrowserFixture = new BrowserFixture { Browser = Browser, PlaywrightFixture = null! }
                }
            };
        }

        public void OpenPage(IPage page) => BrowserContext.Page += Raise.Event<EventHandler<IPage>>(BrowserContext, page);

        public void Dispose()
        {
            TestContext.Current = _previousContext;
            Context.RemoveFromRegistry();
            Context.Dispose();
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}

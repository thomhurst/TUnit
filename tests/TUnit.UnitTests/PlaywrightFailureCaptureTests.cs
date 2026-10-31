using Microsoft.Playwright;
using NSubstitute;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Playwright;

namespace TUnit.UnitTests;

[NotInParallel(nameof(PlaywrightFailureCaptureTests))]
public class PlaywrightFailureCaptureTests
{
    [Test]
    [Arguments(TestState.Failed)]
    [Arguments(TestState.Timeout)]
    [Arguments(TestState.Cancelled)]
    public async Task BrowserTestAttachesScreenshotAndTraceWhenTestDoesNotPass(TestState state)
    {
        using var scope = new CaptureScope("Login(\"admin\")");
        await scope.DiscoverAsync(new CaptureOnFailureAttribute(scope.Directory));
        var page = scope.AddPage();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(state);

        await scope.Test.BrowserTearDown(scope.Context);

        var prefix = PlaywrightArtifactNames.ForTest(scope.Context, 0);
        await Assert.That(prefix).EndsWith("_Login(_admin_)");
        await Assert.That(scope.ArtifactNames()).IsEquivalentTo(new[] { prefix + ".png", prefix + ".trace.zip" });
        await page.Received(1).ScreenshotAsync(Arg.Is<PageScreenshotOptions>(options => options.FullPage == true));
        await scope.Tracing.Received(1).StartAsync(Arg.Is<TracingStartOptions>(options =>
            options.Screenshots == true && options.Snapshots == true && options.Sources == true));
        Received.InOrder(() =>
        {
            page.ScreenshotAsync(Arg.Any<PageScreenshotOptions>());
            scope.Tracing.StopAsync(Arg.Any<TracingStopOptions>());
            scope.BrowserContext.CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        });
    }

    [Test]
    [Arguments(TestState.Passed)]
    [Arguments(TestState.Skipped)]
    public async Task PassingTestDiscardsTraceAndWritesNoArtifacts(TestState state)
    {
        using var scope = new CaptureScope();
        var attribute = new CaptureOnFailureAttribute(scope.Directory);
        await scope.DiscoverAsync(attribute);
        var page = scope.AddPage();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(state);

        await scope.Test.BrowserTearDown(scope.Context);

        // A later teardown hook could still fail the test, so the decision waits for test end.
        await scope.BrowserContext.DidNotReceive().CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        await attribute.OnTestEnd(scope.Context);

        await scope.BrowserContext.Received(1).CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        await scope.Tracing.Received(1).StopAsync(Arg.Is<TracingStopOptions?>(options => options == null || options.Path == null));
        await page.DidNotReceive().ScreenshotAsync(Arg.Any<PageScreenshotOptions>());
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(0);
        await Assert.That(Directory.GetFiles(scope.Directory)).IsEmpty();
    }

    [Test]
    public async Task TeardownFailureAfterBrowserTearDownStillCaptures()
    {
        using var scope = new CaptureScope();
        var attribute = new CaptureOnFailureAttribute(scope.Directory);
        await scope.DiscoverAsync(attribute);
        var page = scope.AddPage();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(TestState.Passed);

        await scope.Test.BrowserTearDown(scope.Context);
        // The engine marks the test failed when a teardown hook throws, before late receivers run.
        scope.SetResult(TestState.Failed);
        await attribute.OnTestEnd(scope.Context);

        var prefix = PlaywrightArtifactNames.ForTest(scope.Context, 0);
        await Assert.That(scope.ArtifactNames()).IsEquivalentTo(new[] { prefix + ".png", prefix + ".trace.zip" });
        Received.InOrder(() =>
        {
            page.ScreenshotAsync(Arg.Any<PageScreenshotOptions>());
            scope.Tracing.StopAsync(Arg.Any<TracingStopOptions>());
            scope.BrowserContext.CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        });
    }

    [Test]
    public async Task InvalidPathIsReportedAndContextsStillClose()
    {
        using var scope = new CaptureScope();
        await scope.DiscoverAsync(new CaptureOnFailureAttribute("bad\0path"));
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(TestState.Failed);

        await scope.Test.BrowserTearDown(scope.Context);

        await scope.BrowserContext.Received(1).CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        await Assert.That(scope.Context.GetErrorOutput()).Contains("could not save failure artifacts");
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InvalidPathStillDisposesFixtures()
    {
        using var scope = new CaptureScope();
        var attribute = new CaptureOnFailureAttribute("bad\0path");
        await scope.DiscoverAsync(attribute);
        var fixture = scope.CreateFixture();
        await ObjectInitializer.InitializeAsync(fixture);
        var page = fixture.Page;
        scope.SetResult(TestState.Failed);

        await attribute.OnTestEnd(scope.Context);

        await page.Received(1).CloseAsync(Arg.Any<PageCloseOptions>());
        await scope.BrowserContext.Received(1).CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        await Assert.That(scope.Context.GetErrorOutput()).Contains("could not save failure artifacts");
    }

    [Test]
    public async Task CancelledSetupStillCapturesBeforePagesClose()
    {
        using var scope = new CaptureScope();
        var attribute = new CaptureOnFailureAttribute(scope.Directory);
        await scope.DiscoverAsync(attribute);
        var fixture = scope.CreateFixture();
        var creation = new TaskCompletionSource<IBrowserContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(creation.Task);
        using var cancellation = new CancellationTokenSource();
        var initialize = ObjectInitializer.InitializeAsync(fixture, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.That(async () => await initialize).Throws<OperationCanceledException>();
        scope.SetResult(TestState.Cancelled);
        var cleanup = attribute.OnTestEnd(scope.Context).AsTask();

        creation.TrySetResult(scope.BrowserContext);
        await cleanup;

        var page = scope.Pages.Single();
        var prefix = PlaywrightArtifactNames.ForTest(scope.Context, 0);
        await Assert.That(scope.ArtifactNames()).IsEquivalentTo(new[] { prefix + ".png", prefix + ".trace.zip" });
        Received.InOrder(() =>
        {
            page.ScreenshotAsync(Arg.Any<PageScreenshotOptions>());
            page.CloseAsync(Arg.Any<PageCloseOptions>());
        });
    }

    [Test]
    [Arguments("a:b*c?d<e>f|g\"h", "a_b_c_d_e_f_g_h")]
    [Arguments("back\\slash/forward", "back_slash_forward")]
    [Arguments("tab\there", "tab_here")]
    public async Task SanitizeRejectsWindowsInvalidCharactersOnEveryPlatform(string value, string expected)
    {
        await Assert.That(PlaywrightArtifactNames.Sanitize(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task TestsReusingASharedFixtureGetTheDisconnectNote()
    {
        using var first = new CaptureScope("First");
        var fixture = first.CreateFixture();
        await ObjectInitializer.InitializeAsync(fixture);
        first.Browser.Disconnected += Raise.Event<EventHandler<IBrowser>>(first.Browser, first.Browser);

        using var second = new CaptureScope("Second");
        second.SetResult(TestState.Failed, new PlaywrightException("Target page, context or browser has been closed"));
        await ((ITestEndEventReceiver)fixture).OnTestEnd(second.Context);

        await Assert.That(second.Context.GetErrorOutput()).Contains("browser disconnected at");
        await fixture.DisposeAsync();
        await fixture.ContextFixture.DisposeAsync();
    }

    [Test]
    public async Task WithoutAttributeNothingIsTraced()
    {
        using var scope = new CaptureScope();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(TestState.Failed);

        await scope.Test.BrowserTearDown(scope.Context);

        await scope.Tracing.DidNotReceive().StartAsync(Arg.Any<TracingStartOptions>());
        await Assert.That(scope.Context.Output.Artifacts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ScreenshotOnlyDoesNotTrace()
    {
        using var scope = new CaptureScope();
        await scope.DiscoverAsync(new CaptureOnFailureAttribute(scope.Directory) { Trace = false });
        scope.AddPage();
        scope.AddPage();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(TestState.Failed);

        await scope.Test.BrowserTearDown(scope.Context);

        var prefix = PlaywrightArtifactNames.ForTest(scope.Context, 0);
        await scope.Tracing.DidNotReceive().StartAsync(Arg.Any<TracingStartOptions>());
        await scope.Tracing.DidNotReceive().StopAsync(Arg.Any<TracingStopOptions>());
        await Assert.That(scope.ArtifactNames()).IsEquivalentTo(new[] { prefix + "-page1.png", prefix + "-page2.png" });
    }

    [Test]
    public async Task VariantsWithTheSameDisplayNameAndRetriesGetDistinctNames()
    {
        using var first = new CaptureScope("Same");
        using var second = new CaptureScope("Same");
        first.Context.CurrentRetryAttempt = 2;

        var firstName = PlaywrightArtifactNames.ForTest(first.Context, first.Context.CurrentRetryAttempt);
        var secondName = PlaywrightArtifactNames.ForTest(second.Context, 0);

        await Assert.That(firstName).EndsWith("_Same-attempt3");
        await Assert.That(secondName).EndsWith("_Same");
        await Assert.That(firstName.Split('_')[0]).IsNotEqualTo(secondName.Split('_')[0]);
        await Assert.That(PlaywrightArtifactNames.ForTest(second.Context, 0)).IsEqualTo(secondName);
    }

    [Test]
    public async Task ReservedNamesNeverOverwriteExistingFiles()
    {
        using var scope = new CaptureScope();
        var first = PlaywrightArtifactNames.Reserve(scope.Directory, "name", ".png");
        var second = PlaywrightArtifactNames.Reserve(scope.Directory, "name", ".png");

        await Assert.That(Path.GetFileName(first)).IsEqualTo("name.png");
        await Assert.That(Path.GetFileName(second)).IsEqualTo("name-2.png");
    }

    [Test]
    public async Task LongDisplayNamesAreTruncated()
    {
        using var scope = new CaptureScope(new string('x', 300));

        await Assert.That(PlaywrightArtifactNames.ForTest(scope.Context, 0).Length).IsLessThanOrEqualTo(9 + 80);
    }

    [Test]
    public async Task CaptureFailureIsReportedWithoutFailingTeardown()
    {
        using var scope = new CaptureScope();
        await scope.DiscoverAsync(new CaptureOnFailureAttribute(scope.Directory));
        var page = scope.AddPage();
        page.ScreenshotAsync(Arg.Any<PageScreenshotOptions>())
            .Returns(Task.FromException<byte[]>(new PlaywrightException("Target page, context or browser has been closed")));
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.SetResult(TestState.Failed);

        await scope.Test.BrowserTearDown(scope.Context);

        var prefix = PlaywrightArtifactNames.ForTest(scope.Context, 0);
        await Assert.That(scope.ArtifactNames()).IsEquivalentTo(new[] { prefix + ".trace.zip" });
        await Assert.That(File.Exists(Path.Combine(scope.Directory, prefix + ".png"))).IsFalse();
        await Assert.That(scope.Context.GetErrorOutput()).Contains("could not take screenshot");
        await scope.BrowserContext.Received(1).CloseAsync(Arg.Any<BrowserContextCloseOptions>());
    }

    [Test]
    public async Task FixturesCaptureBeforePagesCloseAndRecreateEachAttempt()
    {
        using var scope = new CaptureScope();
        var attribute = new CaptureOnFailureAttribute(scope.Directory);
        await scope.DiscoverAsync(attribute);
        var fixture = scope.CreateFixture();

        await ObjectInitializer.InitializeAsync(fixture);
        var firstPage = fixture.Page;
        scope.SetResult(TestState.Failed);
        await attribute.OnTestEnd(scope.Context);

        Received.InOrder(() =>
        {
            firstPage.ScreenshotAsync(Arg.Any<PageScreenshotOptions>());
            scope.Tracing.StopAsync(Arg.Any<TracingStopOptions>());
            firstPage.CloseAsync(Arg.Any<PageCloseOptions>());
            scope.BrowserContext.CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        });

        scope.Context.CurrentRetryAttempt = 1;
        scope.SetResult(TestState.Passed);
        await ObjectInitializer.InitializeAsync(fixture);
        await Assert.That(fixture.Page).IsNotSameReferenceAs(firstPage);
        await attribute.OnTestEnd(scope.Context);

        var prefix = PlaywrightArtifactNames.ForTest(scope.Context, 0);
        await Assert.That(scope.ArtifactNames()).IsEquivalentTo(new[] { prefix + ".png", prefix + ".trace.zip" });
        await scope.Browser.Received(2).NewContextAsync(Arg.Any<BrowserNewContextOptions>());
    }

    [Test]
    public async Task SharedFixturesAreNotCaptured()
    {
        using var scope = new CaptureScope();
        var attribute = new CaptureOnFailureAttribute(scope.Directory);
        await scope.DiscoverAsync(attribute);
        var fixture = scope.CreateFixture();
        TraceScopeRegistry.RegisterFromDataSource(new ClassDataSourceAttribute<ContextFixture> { Shared = SharedType.PerTestSession }, [fixture.ContextFixture]);

        await ObjectInitializer.InitializeAsync(fixture);
        scope.SetResult(TestState.Failed);
        await attribute.OnTestEnd(scope.Context);

        await scope.Tracing.DidNotReceive().StartAsync(Arg.Any<TracingStartOptions>());
        await scope.BrowserContext.DidNotReceive().CloseAsync(Arg.Any<BrowserContextCloseOptions>());
        await fixture.DisposeAsync();
        await fixture.ContextFixture.DisposeAsync();
    }

    [Test]
    public async Task UnexpectedBrowserDisconnectIsReportedOnPlaywrightFailures()
    {
        using var scope = new CaptureScope();
        await scope.Test.NewContext(new BrowserNewContextOptions());
        scope.Browser.Disconnected += Raise.Event<EventHandler<IBrowser>>(scope.Browser, scope.Browser);
        scope.SetResult(TestState.Failed, new PlaywrightException("Target page, context or browser has been closed"));

        await scope.Test.BrowserTearDown(scope.Context);
        await scope.Test.BrowserTearDown(scope.Context);

        var output = scope.Context.GetErrorOutput();
        await Assert.That(output).Contains("browser disconnected at");
        await Assert.That(output.IndexOf("disconnected at", StringComparison.Ordinal))
            .IsEqualTo(output.LastIndexOf("disconnected at", StringComparison.Ordinal));
    }

    [Test]
    public async Task DisconnectIsNotReportedForUnrelatedFailuresOrExpectedClose()
    {
        using var unrelated = new CaptureScope();
        await unrelated.Test.NewContext(new BrowserNewContextOptions());
        unrelated.Browser.Disconnected += Raise.Event<EventHandler<IBrowser>>(unrelated.Browser, unrelated.Browser);
        unrelated.SetResult(TestState.Failed, new InvalidOperationException("assertion failed"));
        await unrelated.Test.BrowserTearDown(unrelated.Context);

        using var closed = new CaptureScope();
        await closed.Test.NewContext(new BrowserNewContextOptions());
        await new BrowserFixture { Browser = closed.Browser, PlaywrightFixture = null! }.DisposeAsync();
        closed.Browser.Disconnected += Raise.Event<EventHandler<IBrowser>>(closed.Browser, closed.Browser);
        closed.SetResult(TestState.Failed, new PlaywrightException("Target page, context or browser has been closed"));
        await closed.Test.BrowserTearDown(closed.Context);

        await Assert.That(unrelated.Context.GetErrorOutput()).DoesNotContain("disconnected at");
        await Assert.That(closed.Context.GetErrorOutput()).DoesNotContain("disconnected at");
    }

    [Test]
    public async Task FixtureNamesDisconnectedBrowserWhenContextCreationFails()
    {
        using var scope = new CaptureScope();
        var fixture = scope.CreateFixture();
        PlaywrightBrowserMonitor.For(scope.Browser);
        scope.Browser.Disconnected += Raise.Event<EventHandler<IBrowser>>(scope.Browser, scope.Browser);
        scope.Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>())
            .Returns(Task.FromException<IBrowserContext>(new PlaywrightException("Target page, context or browser has been closed")));

        var exception = await Assert.That(async () => await ObjectInitializer.InitializeAsync(fixture)).Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("browser disconnected at");
        await Assert.That(exception.InnerException).IsTypeOf<PlaywrightException>();
        await Assert.That(fixture.ContextFixture.BrowserFixture.DisconnectedAt).IsNotNull();
    }

    [Test]
    public async Task DisconnectedSharedBrowserIsRelaunchedWhenEnabled()
    {
        using var scope = new CaptureScope();
        var replacement = Substitute.For<IBrowser>();
        replacement.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(scope.BrowserContext);
        var browserFixture = new RelaunchingBrowserFixture(replacement) { Browser = scope.Browser, PlaywrightFixture = null! };
        var fixture = new ContextFixture { BrowserFixture = browserFixture };
        PlaywrightBrowserMonitor.For(scope.Browser);
        scope.Browser.Disconnected += Raise.Event<EventHandler<IBrowser>>(scope.Browser, scope.Browser);
        var settings = TUnitPlaywrightSettings.Default;
        settings.RelaunchDisconnectedBrowser = true;

        try
        {
            await fixture.InitializeAsync();
        }
        finally
        {
            settings.RelaunchDisconnectedBrowser = false;
        }

        await Assert.That(browserFixture.Browser).IsSameReferenceAs(replacement);
        await Assert.That(browserFixture.DisconnectedAt).IsNull();
        await scope.Browser.DidNotReceive().NewContextAsync(Arg.Any<BrowserNewContextOptions>());
        await replacement.Received(1).NewContextAsync(Arg.Any<BrowserNewContextOptions>());
        await fixture.DisposeAsync();
    }

    private sealed class RelaunchingBrowserFixture(IBrowser replacement) : BrowserFixture
    {
        public override Task InitializeAsync()
        {
            Browser = replacement;
            return Task.CompletedTask;
        }
    }

    private sealed class CaptureScope : IDisposable
    {
        private readonly TestContext? _previousContext = TestContext.Current;
        private readonly List<IPage> _pages = [];
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "TUnit-capture-tests", Guid.NewGuid().ToString("N"));
        public TestContext Context { get; }
        public IBrowser Browser { get; } = Substitute.For<IBrowser>();
        public IBrowserContext BrowserContext { get; } = Substitute.For<IBrowserContext>();
        public ITracing Tracing { get; } = Substitute.For<ITracing>();
        public BrowserTest Test { get; }

        public CaptureScope(string testName = "Capture")
        {
            System.IO.Directory.CreateDirectory(Directory);
            var current = TestContext.Current!;
            Context = new TestContext(testName, current.ServiceProvider, current.ClassContext,
                new TestBuilderContext { TestMetadata = current.Metadata.TestDetails.MethodMetadata }, CancellationToken.None);
            Context.Metadata.TestDetails = new TestDetails([])
            {
                TestId = Guid.NewGuid().ToString(), TestName = testName, ClassType = typeof(PageTest),
                MethodName = testName, ClassInstance = this, TestMethodArguments = [], TestClassArguments = [],
                MethodMetadata = current.Metadata.TestDetails.MethodMetadata, ReturnType = typeof(void),
                AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
            };
            Context.Metadata.DisplayName = testName;
            Browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(BrowserContext);
            BrowserContext.Tracing.Returns(Tracing);
            BrowserContext.Pages.Returns(_ => _pages.ToArray());
            BrowserContext.NewPageAsync().Returns(_ => AddPage());
            Tracing.StopAsync(Arg.Any<TracingStopOptions>()).Returns(call =>
            {
                if (call.Arg<TracingStopOptions?>()?.Path is { } path)
                {
                    File.WriteAllText(path, "trace");
                }

                return Task.CompletedTask;
            });
            Test = new BrowserTest { Browser = Browser };
            TestContext.Current = Context;
        }

        public IReadOnlyList<IPage> Pages => _pages;

        public ValueTask DiscoverAsync(CaptureOnFailureAttribute attribute) =>
            attribute.OnTestDiscovered(new DiscoveredTestContext(Context.Metadata.TestName, Context));

        public IPage AddPage()
        {
            var page = Substitute.For<IPage>();
            page.ScreenshotAsync(Arg.Any<PageScreenshotOptions>()).Returns(call =>
            {
                File.WriteAllText(call.Arg<PageScreenshotOptions>().Path!, "png");
                return Array.Empty<byte>();
            });
            _pages.Add(page);
            return page;
        }

        public PageFixture CreateFixture() => new()
        {
            ContextFixture = new ContextFixture
            {
                BrowserFixture = new BrowserFixture { Browser = Browser, PlaywrightFixture = null! }
            }
        };

        public void SetResult(TestState state, Exception? exception = null) => Context.Execution.Result = new TestResult
        {
            State = state, Start = null, End = null, Duration = null, ComputerName = Environment.MachineName,
            Exception = exception ?? (state == TestState.Failed ? new InvalidOperationException("failed") : null),
        };

        public string[] ArtifactNames() => Context.Output.Artifacts.Select(artifact => artifact.File.Name).ToArray();

        public void Dispose()
        {
            TestContext.Current = _previousContext;
            Context.RemoveFromRegistry();
            Context.Dispose();
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}

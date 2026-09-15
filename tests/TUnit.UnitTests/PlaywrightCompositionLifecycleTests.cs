using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright;
using NSubstitute;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Playwright;

namespace TUnit.UnitTests;

public class PlaywrightCompositionLifecycleTests : ITestStartEventReceiver
{
    internal static readonly ConcurrentBag<string> RecordingDirectories = [];
    private int _starts;

    public int Order => 0;

    public ValueTask OnTestStart(TestContext context)
    {
        _starts++;
        return default;
    }

    [ClassDataSource<TestPageFixture>]
    public required TestPageFixture First { get; init; }

    [ClassDataSource<TestPageFixture>]
    public required TestPageFixture Second { get; init; }

    [Before(Test)]
    public async Task PagesAreReadyBeforeSetupOnEveryAttempt()
    {
        var context = TestContext.Current!;
        var attempt = context.Execution.CurrentRetryAttempt;
        await Assert.That(_starts).IsEqualTo(0);
        await Assert.That(First.Page.Url).IsEqualTo($"attempt-{attempt}");
        await Assert.That(Second.Page.Url).IsEqualTo($"attempt-{attempt}");
        await Assert.That(First.ContextFixture.Context).IsNotSameReferenceAs(Second.ContextFixture.Context);
        await Assert.That(context.Output.Artifacts.Count).IsEqualTo(attempt * 2);
        foreach (var artifact in context.Output.Artifacts)
        {
            await Assert.That(artifact.File.Exists).IsTrue();
        }
    }

    [Test, RecordVideo, Retry(1)]
    public void RecordingFixturesSurviveRetryWithFreshPages()
    {
        if (TestContext.Current!.Execution.CurrentRetryAttempt == 0)
        {
            throw new InvalidOperationException("Exercise fixture retry lifecycle.");
        }
    }

    [Test, RecordVideo]
    public void RecordingWorksWithoutRetries()
    {
    }

    [After(Test)]
    public async Task PagesRemainOpenThroughTeardownHooks()
    {
        await Assert.That(_starts).IsEqualTo(1);
        await Assert.That(First.Page.IsClosed).IsFalse();
        await Assert.That(Second.Page.IsClosed).IsFalse();
    }

    public sealed class TestPageFixture : PageFixture
    {
        [SetsRequiredMembers]
        public TestPageFixture()
        {
            var browser = Substitute.For<IBrowser>();
            browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(_ => CreateContext());
            ContextFixture = new ContextFixture
            {
                BrowserFixture = new StubBrowserFixture
                {
                    Browser = browser,
                    PlaywrightFixture = new StubPlaywrightFixture()
                }
            };
        }

        private static IBrowserContext CreateContext()
        {
            var attempt = TestContext.Current!.Execution.CurrentRetryAttempt;
            var directory = Path.Combine(Path.GetTempPath(), "TUnit-composition-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            RecordingDirectories.Add(directory);
            var context = Substitute.For<IBrowserContext>();
            context.NewPageAsync().Returns(_ =>
            {
                var source = Path.Combine(directory, $"{Guid.NewGuid():N}.webm");
                File.WriteAllText(source, "test recording");
                var video = Substitute.For<IVideo>();
                video.PathAsync().Returns(source);
                var page = Substitute.For<IPage>();
                page.Video.Returns(video);
                page.Url.Returns($"attempt-{attempt}");
                page.CloseAsync().Returns(_ =>
                {
                    page.IsClosed.Returns(true);
                    return Task.CompletedTask;
                });
                context.Page += Raise.Event<EventHandler<IPage>>(context, page);
                return page;
            });
            return context;
        }
    }

    private sealed class StubBrowserFixture : BrowserFixture
    {
        public override Task InitializeAsync() => Task.CompletedTask;
    }

    private sealed class StubPlaywrightFixture : PlaywrightFixture
    {
        public override Task InitializeAsync() => Task.CompletedTask;
    }
}

public static class PlaywrightCompositionCleanup
{
    [After(TestSession)]
    public static void DeleteTestRecordings()
    {
        while (PlaywrightCompositionLifecycleTests.RecordingDirectories.TryTake(out var directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

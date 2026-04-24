using TUnit.Assertions;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;

namespace TUnit.UnitTests;

// Interceptor is abstract; writes funnel through a private helper that calls
// Context.Current.MarkConsoleOutputCaptured(), so running writes under the
// current TestContext lets us observe the flag via HasCapturedConsoleOutput.
internal sealed class TestConsoleInterceptor : OptimizedConsoleInterceptor
{
    protected override LogLevel SinkLogLevel => LogLevel.Information;

    protected override ConsoleLineBuffer GetLineBuffer() => Context.Current.ConsoleStdOutLineBuffer;

    private protected override TextWriter GetOriginalOut() => TextWriter.Null;

    private protected override void ResetDefault()
    {
    }
}

public class ConsoleOutputCaptureFlagTests
{
    [Test]
    public async Task NewContext_HasNotCapturedConsoleOutput()
    {
        var context = new TestableContext();

        await Assert.That(context.HasCapturedConsoleOutput).IsFalse();
    }

    [Test]
    public async Task MarkConsoleOutputCaptured_SetsFlagTrue()
    {
        var context = new TestableContext();

        context.MarkConsoleOutputCaptured();

        await Assert.That(context.HasCapturedConsoleOutput).IsTrue();
    }

    [Test]
    public async Task InterceptorWrite_PartialNoNewline_MarksCurrentContextCaptured()
    {
        var testContext = TestContext.Current!;

        var interceptor = new TestConsoleInterceptor();
        interceptor.Write("x");

        await Assert.That(testContext.HasCapturedConsoleOutput).IsTrue();

        // Drain the partial buffer so it doesn't leak into the rest of the test's output.
        testContext.ConsoleStdOutLineBuffer.Drain();
    }

    [Test]
    public async Task InterceptorWriteLine_EmptyLine_MarksCurrentContextCaptured()
    {
        var testContext = TestContext.Current!;

        var interceptor = new TestConsoleInterceptor();
        interceptor.WriteLine();

        await Assert.That(testContext.HasCapturedConsoleOutput).IsTrue();
    }
}

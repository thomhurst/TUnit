using TUnit.Core;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

public class RetryDelegateCancellationTests
{
    [Test]
    [Arguments(true, true)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    public async Task RetryHandlerPreservesCancellation(bool requestCancellation, bool throwFromHandler)
    {
        var current = TestContext.Current!;
        var builder = new TestBuilderContext { TestMetadata = current.TestDetails.MethodMetadata };
        var context = new TestContext(nameof(RetryDelegateCancellationTests), current.ServiceProvider,
            current.ClassContext, builder, CancellationToken.None);
        context.Metadata.TestDetails = new TestDetails([])
        {
            TestId = context.Id, TestName = "Retry", ClassType = typeof(RetryDelegateCancellationTests),
            MethodName = "Retry", ClassInstance = this, TestMethodArguments = [], TestClassArguments = [],
            MethodMetadata = current.TestDetails.MethodMetadata, ReturnType = typeof(void), RetryLimit = 1,
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };
        var executable = new StubExecutableTest { TestId = context.Id, Arguments = [], Context = context };
        context.InitializeTestCancellation(CancellationToken.None);
        context.CompleteTestCancellation();
        var failure = new InvalidOperationException("Retry handler failure");
        builder.Events.OnTestRetry += (_, _) =>
        {
            if (requestCancellation)
            {
                context.Cancel();
            }
            if (throwFromHandler)
            {
                throw failure;
            }
            return default;
        };
        var attempts = 0;
        try
        {
            async Task Execute() => await RetryHelper.ExecuteWithRetry(context, () =>
            {
                attempts++;
                throw new InvalidOperationException("Attempt failure");
            });

            if (requestCancellation)
            {
                await Execute();
                await Assert.That(executable.State).IsEqualTo(TestState.Cancelled);
            }
            else
            {
                var thrown = await Assert.That(Execute).Throws<InvalidOperationException>();
                await Assert.That(thrown).IsSameReferenceAs(failure);
                context.Cancel();
                await Assert.That(context.IsTestCancellationRequested).IsFalse();
            }
            await Assert.That(attempts).IsEqualTo(1);
        }
        finally
        {
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    private sealed class StubExecutableTest : AbstractExecutableTest
    {
        public override Task<object> CreateInstanceAsync() => throw new NotSupportedException();
        public override Task InvokeTestAsync(object instance, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

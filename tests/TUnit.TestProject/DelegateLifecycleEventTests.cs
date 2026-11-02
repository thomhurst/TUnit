using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class DelegateLifecycleEventTests
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscribeAttribute : DataSourceGeneratorAttribute<int>
    {
        protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            yield return () =>
            {
                var events = metadata.TestBuilderContext.Current.Events;
                events.OnTestRegistered += Record("registered");
                events.OnTestStart += Record("start");
                events.OnTestEnd += Record("end");
                events.OnTestSkipped += Record("skipped");
                events.OnTestRetry += (_, args) =>
                {
                    args.TestContext.StateBag.Items["retry"] = args.RetryAttempt;
                    return default;
                };
                return 1;
            };
        }

        private static Func<object, TestContext, ValueTask> Record(string name) => (_, context) =>
        {
            context.StateBag.Items.TryGetValue(name, out var count);
            context.StateBag.Items[name] = (count as int? ?? 0) + 1;
            return default;
        };
    }

    [Test, Subscribe, EngineTest(ExpectedResult.Pass)]
    public async Task Passing(int value)
    {
        await Assert.That(TestContext.Current!.StateBag.Items["registered"]).IsEqualTo(1);
        await Assert.That(TestContext.Current.StateBag.Items["start"]).IsEqualTo(1);
    }

    [Test, Subscribe, EngineTest(ExpectedResult.Pass)]
    public async Task Generic<T>(T value)
    {
        await Assert.That(TestContext.Current!.StateBag.Items["registered"]).IsEqualTo(1);
        await Assert.That(TestContext.Current.StateBag.Items["start"]).IsEqualTo(1);
    }

    [Test, Subscribe, Retry(1), EngineTest(ExpectedResult.Pass)]
    public async Task Retried(int value)
    {
        var context = TestContext.Current!;
        if (!context.StateBag.Items.ContainsKey("retry"))
        {
            throw new InvalidOperationException("Exercise retry notification");
        }

        await Assert.That(context.StateBag.Items["retry"]).IsEqualTo(1);
        await Assert.That(context.StateBag.Items["start"]).IsEqualTo(2);
        await Assert.That(context.StateBag.Items["end"]).IsEqualTo(1);
    }

    [Test, Subscribe, Skip("Exercise skipped notification")]
    public void Skipped(int value) => throw new InvalidOperationException("Must not execute");

    [After(Class)]
    public static async Task Verify(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            await Assert.That(test.StateBag.Items["registered"]).IsEqualTo(1);
            if (test.Metadata.TestDetails.MethodName == nameof(Skipped))
            {
                await Assert.That(test.StateBag.Items["skipped"]).IsEqualTo(1);
                await Assert.That(test.StateBag.Items["end"]).IsEqualTo(1);
                await Assert.That(test.StateBag.Items.ContainsKey("start")).IsFalse();
            }
            else
            {
                var expected = test.Metadata.TestDetails.MethodName == nameof(Retried) ? 2 : 1;
                await Assert.That(test.StateBag.Items["end"]).IsEqualTo(expected);
            }
        }
    }
}

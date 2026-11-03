using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class DelegateLifecycleEventTests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class SubscribeAttribute : DataSourceGeneratorAttribute<int>
    {
        public int Rows { get; set; } = 1;

        protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            for (var i = 0; i < Rows; i++)
            {
                yield return () =>
                {
                    metadata.TestBuilderContext.Current.StateBag["factoryState"] = "preserved";
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

[DelegateLifecycleEventTests.Subscribe(Rows = 2)]
public class ClassDelegateLifecycleEventTests(int value)
{
    [Test, Repeat(1), EngineTest(ExpectedResult.Pass)]
    public async Task FirstRow()
    {
        await Assert.That(value).IsEqualTo(1);
        await Assert.That(TestContext.Current!.StateBag.Items["factoryState"]).IsEqualTo("preserved");
        await Assert.That(TestContext.Current!.StateBag.Items["registered"]).IsEqualTo(1);
        await Assert.That(TestContext.Current.StateBag.Items["start"]).IsEqualTo(1);
    }

    [After(Class)]
    public static async Task Verify(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            await Assert.That(test.StateBag.Items["end"]).IsEqualTo(1);
        }
    }
}

public class DataScopeRetryLifecycleTests
{
    public sealed class Scope : IDisposable
    {
        public int DisposeCount;
        public Service? Service;
        public void Dispose()
        {
            DisposeCount++;
            Service?.Dispose();
        }
    }

    public sealed record Service(Scope Scope) : IDisposable
    {
        public int DisposeCount;
        public Dependency Dependency { get; } = new();

        public void Dispose()
        {
            if (++DisposeCount != 1)
            {
                throw new InvalidOperationException("Scope-owned service disposed more than once");
            }
            Dependency.Dispose();
        }
    }

    public sealed class Dependency : TUnit.Core.Interfaces.IAsyncInitializer, IDisposable
    {
        public bool Initialized { get; private set; }
        public int DisposeCount;
        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            if (++DisposeCount != 1)
            {
                throw new InvalidOperationException("Scope-owned dependency disposed more than once");
            }
        }
    }

    public sealed class ScopeDataAttribute : DependencyInjectionDataSourceAttribute<Scope>
    {
        public override Scope CreateScope(DataGeneratorMetadata metadata) => new();
        public override object Create(Scope scope, Type type) => scope.Service = new Service(scope);
    }

    [Test, ScopeData, Retry(1), EngineTest(ExpectedResult.Pass)]
    public async Task ScopeSurvivesRetry(Service service)
    {
        var context = TestContext.Current!;
        context.StateBag.Items["scope"] = service.Scope;
        await Assert.That(service.Scope.DisposeCount).IsEqualTo(0);
        await Assert.That(service.DisposeCount).IsEqualTo(0);
        await Assert.That(service.Dependency.Initialized).IsTrue();
        await Assert.That(service.Dependency.DisposeCount).IsEqualTo(0);
        if (context.StateBag.Items.TryAdd("firstAttempt", true))
        {
            throw new InvalidOperationException("Exercise scope lifetime across retries");
        }
    }

    [After(Class)]
    public static async Task Verify(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            await Assert.That(((Scope)test.StateBag.Items["scope"]!).DisposeCount).IsEqualTo(1);
            await Assert.That(((Scope)test.StateBag.Items["scope"]!).Service!.DisposeCount).IsEqualTo(1);
            await Assert.That(((Scope)test.StateBag.Items["scope"]!).Service!.Dependency.DisposeCount).IsEqualTo(1);
        }
    }
}

public class NonDisposableDataScopeLifecycleTests
{
    public sealed class Scope;

    public sealed class Service : IDisposable
    {
        public int DisposeCount;
        public void Dispose() => DisposeCount++;
    }

    public sealed class ScopeDataAttribute : DependencyInjectionDataSourceAttribute<Scope>
    {
        public override Scope CreateScope(DataGeneratorMetadata metadata) => new();
        public override object Create(Scope scope, Type type) => new Service();
    }

    [Test, ScopeData, EngineTest(ExpectedResult.Pass)]
    public async Task TrackerOwnsServicesWithoutDisposableScope(Service service)
    {
        TestContext.Current!.StateBag.Items["service"] = service;
        await Assert.That(service.DisposeCount).IsEqualTo(0);
    }

    [After(Class)]
    public static async Task Verify(ClassHookContext context)
    {
        foreach (var test in context.Tests)
        {
            await Assert.That(((Service)test.StateBag.Items["service"]!).DisposeCount).IsEqualTo(1);
        }
    }
}

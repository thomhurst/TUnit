using TUnit.Core;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

public class TestRunnerTests
{
    [Test]
    public async Task ExecuteTestAsync_WithNoDependencies_DoesNotUseExecutingTestsLedger()
    {
        var runner = CreateRunner(out var coordinator);
        var test = CreateTest("no-dependencies");

        await runner.ExecuteTestAsync(test, CancellationToken.None);

        await Assert.That(runner.ExecutingTestsCount).IsEqualTo(0);
        await Assert.That(coordinator.GetCallCount(test)).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteTestAsync_WithNoDependenciesAndExistingExecutionTask_AwaitsExistingTask()
    {
        var runner = CreateRunner(out var coordinator);
        var test = CreateTest("existing-execution-task");
        var existingExecution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        test.ExecutionTask = existingExecution.Task;

        var execution = runner.ExecuteTestAsync(test, CancellationToken.None);

        await Assert.That(execution.IsCompleted).IsFalse();

        existingExecution.SetResult();
        await execution;

        await Assert.That(runner.ExecutingTestsCount).IsEqualTo(0);
        await Assert.That(coordinator.GetCallCount(test)).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteTestWithoutExecutionTaskAsync_WithNoDependenciesAndExistingExecutionTask_ExecutesTest()
    {
        var runner = CreateRunner(out var coordinator);
        var test = CreateTest("scheduler-wrapper");
        var existingExecution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        test.ExecutionTask = existingExecution.Task;

        await runner.ExecuteTestWithoutExecutionTaskAsync(test, CancellationToken.None);

        await Assert.That(runner.ExecutingTestsCount).IsEqualTo(0);
        await Assert.That(coordinator.GetCallCount(test)).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteTestAsync_WithDependencies_UsesExecutingTestsLedger()
    {
        var runner = CreateRunner(out _);
        var dependency = CreateTest("dependency");
        var test = CreateTest("with-dependencies", [dependency]);
        TestScheduler.MarkDependencyRelatedTestsForExecutionDedup([test]);

        await runner.ExecuteTestAsync(test, CancellationToken.None);

        await Assert.That(runner.ExecutingTestsCount).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteTestAsync_WithNoDependenciesButDependencyTarget_UsesExecutingTestsLedger()
    {
        var runner = CreateRunner(out var coordinator);
        var test = CreateTest("dependency-target");
        test.RequiresExecutionDedup = true;

        await runner.ExecuteTestAsync(test, CancellationToken.None);

        await Assert.That(runner.ExecutingTestsCount).IsEqualTo(1);
        await Assert.That(coordinator.GetCallCount(test)).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteTestAsync_WithDependencies_DeduplicatesConcurrentAttempts()
    {
        var runner = CreateRunner(out var coordinator);
        var dependency = CreateTest("dependency");
        var test = CreateTest("with-dependencies", [dependency]);
        TestScheduler.MarkDependencyRelatedTestsForExecutionDedup([test]);
        var releaseExecution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.SetExecutionTask(test, releaseExecution.Task);

        var first = runner.ExecuteTestAsync(test, CancellationToken.None);
        var second = runner.ExecuteTestAsync(test, CancellationToken.None);

        releaseExecution.SetResult();
        await Task.WhenAll(first.AsTask(), second.AsTask());

        await Assert.That(coordinator.GetCallCount(test)).IsEqualTo(1);
        await Assert.That(runner.ExecutingTestsCount).IsEqualTo(2);
    }

    [Test]
    public async Task MarkDependencyRelatedTestsForExecutionDedup_MarksTransitiveDependencyTargetsOutsideBatch()
    {
        var leaf = CreateTest("leaf");
        var middle = CreateTest("middle", [leaf]);
        var root = CreateTest("root", [middle]);

        TestScheduler.MarkDependencyRelatedTestsForExecutionDedup([root]);

        await Assert.That(root.RequiresExecutionDedup).IsTrue();
        await Assert.That(middle.RequiresExecutionDedup).IsTrue();
        await Assert.That(leaf.RequiresExecutionDedup).IsTrue();
    }

    private static TestRunner CreateRunner(out FakeTestCoordinator coordinator)
    {
        coordinator = new FakeTestCoordinator();

        return new TestRunner(
            coordinator,
            new FakeMessageBus(),
            isFailFastEnabled: false,
            new CancellationTokenSource(),
            logger: null!,
            new TestStateManager(),
            new ParallelLimitLockProvider(),
            new NotInParallelLock());
    }

    private static AbstractExecutableTest CreateTest(
        string testId,
        AbstractExecutableTest[]? dependencies = null)
    {
        var metadata = CreateMetadata(testId);
        var beforeDiscoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
        var discoveryContext = new TestDiscoveryContext(beforeDiscoveryContext) { TestFilter = null };
        var sessionContext = new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = null
        };
        var assemblyContext = new AssemblyHookContext(sessionContext)
        {
            Assembly = typeof(TestRunnerTests).Assembly
        };
        var classContext = new ClassHookContext(assemblyContext)
        {
            ClassType = typeof(TestRunnerTests)
        };
        var builderContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata
        };
        var context = new TestContext(testId, new FakeServiceProvider(), classContext, builderContext, CancellationToken.None);

        var test = new StubExecutableTest
        {
            TestId = testId,
            Metadata = metadata,
            Arguments = [],
            Context = context,
            Dependencies = dependencies?.Select(dependency => new ResolvedDependency
            {
                Test = dependency,
                Metadata = TestDependency.FromMethodName(dependency.Metadata.TestMethodName)
            }).ToArray() ?? []
        };

        return test;
    }

    private static TestMetadata<TestRunnerTests> CreateMetadata(string testId)
    {
        var classMetadata = new ClassMetadata
        {
            Type = typeof(TestRunnerTests),
            TypeInfo = new ConcreteType(typeof(TestRunnerTests)),
            Name = nameof(TestRunnerTests),
            Namespace = typeof(TestRunnerTests).Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = typeof(TestRunnerTests).Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = [],
            Properties = []
        };

        return new TestMetadata<TestRunnerTests>
        {
            TestClassType = typeof(TestRunnerTests),
            TestMethodName = testId,
            TestName = testId,
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [],
            MethodMetadata = new MethodMetadata
            {
                Type = typeof(TestRunnerTests),
                TypeInfo = new ConcreteType(typeof(TestRunnerTests)),
                Name = testId,
                GenericTypeCount = 0,
                ReturnType = typeof(void),
                ReturnTypeInfo = new ConcreteType(typeof(void)),
                Parameters = [],
                Class = classMetadata
            },
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    private sealed class StubExecutableTest : AbstractExecutableTest
    {
        public override Task<object> CreateInstanceAsync() => Task.FromResult<object>(new object());

        public override Task InvokeTestAsync(object instance, CancellationToken cancellationToken)
        {
            _ = instance;
            _ = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTestCoordinator : ITestCoordinator
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<string, int> _callCounts = [];
        private readonly Dictionary<string, Task> _executionTasks = [];

        public int GetCallCount(AbstractExecutableTest test)
        {
            lock (_lock)
            {
                return _callCounts.GetValueOrDefault(test.TestId);
            }
        }

        public void SetExecutionTask(AbstractExecutableTest test, Task executionTask)
        {
            lock (_lock)
            {
                _executionTasks[test.TestId] = executionTask;
            }
        }

        public ValueTask ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
        {
            _ = cancellationToken;

            Task? executionTask;
            lock (_lock)
            {
                _callCounts[test.TestId] = _callCounts.GetValueOrDefault(test.TestId) + 1;
                _executionTasks.TryGetValue(test.TestId, out executionTask);
            }

            if (executionTask is not null)
            {
                return CompleteAfterAsync(test, executionTask);
            }

            test.SetResult(TestState.Passed);
            return default;
        }

        private static async ValueTask CompleteAfterAsync(AbstractExecutableTest test, Task executionTask)
        {
            await executionTask.ConfigureAwait(false);
            test.SetResult(TestState.Passed);
        }
    }

    private sealed class FakeMessageBus : ITUnitMessageBus
    {
        public ValueTask Discovered(TestContext testContext)
        {
            _ = testContext;
            return default;
        }

        public ValueTask InProgress(TestContext testContext)
        {
            _ = testContext;
            return default;
        }

        public ValueTask Passed(TestContext testContext, DateTimeOffset start)
        {
            _ = testContext;
            _ = start;
            return default;
        }

        public ValueTask Failed(TestContext testContext, Exception exception, DateTimeOffset start)
        {
            _ = testContext;
            _ = exception;
            _ = start;
            return default;
        }

        public ValueTask Skipped(TestContext testContext, string reason)
        {
            _ = testContext;
            _ = reason;
            return default;
        }

        public ValueTask Cancelled(TestContext testContext, DateTimeOffset start)
        {
            _ = testContext;
            _ = start;
            return default;
        }

        public ValueTask SessionArtifact(Artifact artifact)
        {
            _ = artifact;
            return default;
        }
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            _ = serviceType;
            return null;
        }
    }
}

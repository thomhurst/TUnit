using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7525;

public class RegistrationFailureTests
{
    public sealed class ThrowingReceiverAttribute : Attribute, ITestRegisteredEventReceiver
    {
        public int Order => 0;

        public ValueTask OnTestRegistered(TestRegisteredContext context)
            {
            context.SetSkipped("skip before registration failure");
            throw new InvalidOperationException("Registration receiver failed (#7525)");
        }
    }

    public sealed class ThrowingDelegateAttribute : DataSourceGeneratorAttribute<int>
    {
        protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            yield return () =>
            {
                metadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, _) =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("Registration delegate failed (#7525)");
                };
                return 1;
            };
        }
    }

    [Test, ThrowingReceiver, EngineTest(ExpectedResult.Failure)]
    public void ReceiverFailure() => throw new Exception("Test body must not execute");

    [Test, ThrowingDelegate, EngineTest(ExpectedResult.Failure)]
    public void DelegateFailure(int value) => throw new Exception("Test body must not execute");

    [Test, EngineTest(ExpectedResult.Pass)]
    public void UnrelatedTest() { }
}

// Eight or more tests take the parallel registration path.
[InheritsTests]
public class ParallelRegistrationFailureTests : RegistrationFailureTests
{
    [Test, Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5), EngineTest(ExpectedResult.Pass)]
    public void OtherTests(int value) { }
}

public sealed class RegistrationDisposable : IDisposable
{
    public static List<RegistrationDisposable> Created { get; } = [];
    public bool IsDisposed { get; private set; }
    public RegistrationDisposable() => Created.Add(this);
    public IAsyncInitializer Child => throw new InvalidOperationException("tracking getter failed");
    public void Dispose() => IsDisposed = true;
}

[EngineTest(ExpectedResult.Failure)]
public class RegistrationDisposalTests
{
    public static IEnumerable<RegistrationDisposable> Values()
    {
        yield return new RegistrationDisposable();
    }

    [Test, RegistrationFailureTests.ThrowingReceiver, MethodDataSource(nameof(Values))]
    public void Fails(RegistrationDisposable value) => throw new Exception("Test body must not execute");

    [Test, DependsOn(nameof(Fails), ProceedOnFailure = true)]
    public async Task ArgumentWasDisposed()
    {
        await Assert.That(RegistrationDisposable.Created.Count).IsEqualTo(1);
        await Assert.That(RegistrationDisposable.Created[0].IsDisposed).IsTrue();
    }
}

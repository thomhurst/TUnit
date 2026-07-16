using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4049;

/// <summary>
/// Tests that exceptions thrown during nested IAsyncInitializer property access
/// are properly propagated and cause tests to fail rather than running with null properties.
/// See: https://github.com/thomhurst/TUnit/issues/4049
/// </summary>
[EngineTest(ExpectedResult.Failure)]
[ClassDataSource<FailingNestedInitializerFactory>(Shared = SharedType.None)]
public class NestedInitializerExceptionPropagationTests(
    NestedInitializerExceptionPropagationTests.FailingNestedInitializerFactory factory)
{
    [Test]
    public void Test_Should_Fail_Due_To_Nested_Initializer_Exception()
    {
        // This test should never run - it should fail during discovery/initialization
        // because the nested IAsyncInitializer throws during property access
        throw new InvalidOperationException("This test should not have executed - nested initializer should have thrown");
    }

    public class FailingNestedInitializerFactory : IAsyncInitializer
    {
        // This property throws when accessed, simulating a container that fails to start
        public FailingNestedInitializer NestedInitializer =>
            throw new InvalidOperationException("Simulated container startup failure");

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class FailingNestedInitializer : IAsyncInitializer
    {
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}

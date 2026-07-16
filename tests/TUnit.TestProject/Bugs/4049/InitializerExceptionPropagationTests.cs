using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4049;

/// <summary>
/// Tests that exceptions thrown during IAsyncInitializer.InitializeAsync
/// are properly propagated and cause tests to fail.
/// See: https://github.com/thomhurst/TUnit/issues/4049
/// </summary>
[EngineTest(ExpectedResult.Failure)]
[ClassDataSource<FailingInitializerFactory>(Shared = SharedType.None)]
public class InitializerExceptionPropagationTests(
    InitializerExceptionPropagationTests.FailingInitializerFactory factory)
{
    [Test]
    public void Test_Should_Fail_Due_To_Initializer_Exception()
    {
        // This test should never run - it should fail during initialization
        // because the IAsyncInitializer.InitializeAsync throws
        throw new InvalidOperationException("This test should not have executed - initializer should have thrown");
    }

    public class FailingInitializerFactory : IAsyncInitializer
    {
        public Task InitializeAsync()
        {
            throw new InvalidOperationException("Simulated initialization failure (e.g., Docker container failed to start)");
        }
    }
}

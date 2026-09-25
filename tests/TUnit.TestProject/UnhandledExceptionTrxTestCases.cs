namespace TUnit.TestProject;

// Invoked in a child process by UnhandledExceptionTrxTests: the test results must survive
// a failure disposing a static data source after all tests have finished.
public class UnhandledExceptionTrxTestCases
{
    [ClassDataSource<ThrowingResource>(Shared = SharedType.PerTestSession)]
    public static ThrowingResource Resource { get; set; } = null!;

    [Test]
    public void PassingTest()
    {
        Resource.ArmFailure();
    }

    [Test]
    public void FailingTest()
    {
        Resource.ArmFailure();
        throw new InvalidOperationException("Test body failure");
    }

    public class ThrowingResource : IAsyncDisposable
    {
        private bool _used;

        public void ArmFailure() => _used = true;

        public ValueTask DisposeAsync()
        {
            // Static data sources can be initialized even when this class is filtered out.
            // Only the regression's child process opts in to the deliberate cleanup failure.
            if (_used && Environment.GetEnvironmentVariable("TUNIT_TEST_THROW_ON_STATIC_RESOURCE_DISPOSAL") == "1")
            {
                throw new InvalidOperationException("Shared resource disposal failure");
            }

            return default;
        }
    }
}

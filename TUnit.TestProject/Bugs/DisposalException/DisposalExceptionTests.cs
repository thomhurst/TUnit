using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs.DisposalException;

/// <summary>
/// Regression test for issue where disposal exceptions during cleanup
/// would override passing test results, causing tests to fail incorrectly.
/// 
/// This simulates a WebApplicationFactory scenario where the factory fails
/// to build (host throws exception), but the test correctly asserts the expected
/// behavior. During cleanup, the factory's disposal fails because the host is
/// in an invalid state, but this should not override the passing test result.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DisposalExceptionTests
{
    /// <summary>
    /// Simulates a fixture that fails during disposal, like a WebApplicationFactory
    /// with a failed host that throws when StopAsync is called.
    /// </summary>
    public class FixtureWithFailingDisposal : IAsyncDisposable
    {
        public bool DisposalShouldFail { get; set; }

        public async ValueTask DisposeAsync()
        {
            // Simulate the disposal failure that occurs when WebApplicationFactory
            // tries to stop a host that's in an invalid state
            if (DisposalShouldFail)
            {
                await Task.Yield();
                throw new InvalidOperationException("Disposal failed - simulating Host.StopAsync with null reference");
            }
        }
    }

    [Test]
    [ClassDataSource<FixtureWithFailingDisposal>]
    public async Task PassingTest_WithDisposalException_ShouldRemainPassed(FixtureWithFailingDisposal fixture)
    {
        // Simulate the scenario: the fixture is in a state that will cause disposal to fail
        fixture.DisposalShouldFail = true;

        // The test itself passes - it correctly validates the expected behavior
        await Assert.That(fixture).IsNotNull();
        await Assert.That(fixture.DisposalShouldFail).IsTrue();

        // After this test completes successfully, disposal will fail.
        // The fix ensures the disposal exception doesn't override the passing result.
    }

    /// <summary>
    /// Verify that passing tests with successful disposal remain passed.
    /// This is the baseline case - everything works as expected.
    /// </summary>
    public class FixtureWithSuccessfulDisposal : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
            // Successful disposal
        }
    }

    [Test]
    [ClassDataSource<FixtureWithSuccessfulDisposal>]
    public async Task PassingTest_WithSuccessfulDisposal_ShouldPass(FixtureWithSuccessfulDisposal fixture)
    {
        // Simple passing test with successful disposal
        await Assert.That(fixture).IsNotNull();
    }
}

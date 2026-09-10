using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3997;

/// <summary>
/// Simulates a data source (like a Docker container) that needs initialization during test discovery
/// so that InstanceMethodDataSource can access its data when generating test cases.
/// </summary>
public class SimulatedContainer : IAsyncDiscoveryInitializer
{
    private readonly List<string> _data = [];
    public bool IsInitialized { get; private set; }

    public IReadOnlyList<string> Data => _data;

    public Task InitializeAsync()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Container already initialized! InitializeAsync should only be called once.");
        }

        // Simulate container startup and data population
        _data.AddRange(["TestCase1", "TestCase2", "TestCase3"]);
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tests that IAsyncDiscoveryInitializer is called during test discovery,
/// allowing InstanceMethodDataSource to access initialized data.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class DiscoveryInitializerTests
{
    [ClassDataSource<SimulatedContainer>(Shared = SharedType.PerClass)]
    public required SimulatedContainer Container { get; init; }

    /// <summary>
    /// This property provides test data from the initialized container.
    /// The container MUST be initialized during discovery before this is evaluated.
    /// </summary>
    public IEnumerable<string> TestCases => Container.Data;

    [Test]
    [InstanceMethodDataSource(nameof(TestCases))]
    public async Task TestWithContainerData(string testCase)
    {
        // Container should be initialized
        await Assert.That(Container.IsInitialized).IsTrue();

        // testCase should be one of the container's data items
        await Assert.That(Container.Data).Contains(testCase);
    }
}

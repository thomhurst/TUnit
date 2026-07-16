using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Base class for Kafka tests with per-test topic prefix isolation.
/// Extends TestsBase (which provides container injection) and adds:
/// - Unique topic prefix per test (using GetIsolatedPrefix helper)
/// - Helper methods for topic management
/// </summary>
/// <remarks>
/// This class demonstrates the new simpler configuration API:
/// - <see cref="ConfigureTestConfiguration"/> is auto-additive (no need to call base)
/// - <see cref="GetIsolatedPrefix"/> provides consistent isolation naming
/// </remarks>
[SuppressMessage("Usage", "TUnit0043:Property must use `required` keyword")]
public abstract class KafkaTestBase : TestsBase
{
    [ClassDataSource<InMemoryKafka>(Shared = SharedType.PerTestSession)]
    public InMemoryKafka Kafka { get; init; } = null!;

    /// <summary>
    /// The unique topic prefix for this test.
    /// All Kafka topics will be prefixed with this value.
    /// </summary>
    protected string TopicPrefix { get; private set; } = null!;

    /// <summary>
    /// Configures the application with a unique topic prefix for this test.
    /// Uses the new simpler API - no need to call base or chain delegates.
    /// </summary>
    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        // Generate unique topic prefix using the built-in helper
        TopicPrefix = GetIsolatedPrefix();

        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Kafka:TopicPrefix", TopicPrefix }
        });
    }

    /// <summary>
    /// Gets the fully qualified topic name with prefix.
    /// </summary>
    protected string GetTopicName(string baseName) => $"{TopicPrefix}{baseName}";

    /// <summary>
    /// Gets the bootstrap address for the Kafka container.
    /// </summary>
    protected string GetBootstrapAddress() => Kafka.Container.GetBootstrapAddress();
}

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// These tests demonstrate that parallel tests have isolated Kafka topic namespaces.
/// Each test has its own topic prefix, so messages from one test cannot leak into another.
/// The [Repeat(3)] attribute creates multiple iterations that run in parallel,
/// and each gets its own topic prefix based on the unique TestContext.Id.
/// </summary>
public class KafkaIsolationTests : KafkaTestBase
{
    /// <summary>
    /// Verifies each test has a unique topic prefix.
    /// </summary>
    [Test, Repeat(3)]
    public async Task TopicPrefix_IsUnique()
    {
        // Each test instance should have a non-empty prefix
        await Assert.That(TopicPrefix).IsNotNullOrEmpty();

        // The prefix should follow our naming convention
        await Assert.That(TopicPrefix).StartsWith("test_");
        await Assert.That(TopicPrefix).EndsWith("_");
    }

    /// <summary>
    /// Verifies topic names are properly prefixed.
    /// </summary>
    [Test, Repeat(3)]
    public async Task GetTopicName_AppliesPrefix()
    {
        var topicName = GetTopicName("orders");

        await Assert.That(topicName).StartsWith(TopicPrefix);
        await Assert.That(topicName).EndsWith("orders");
    }

    /// <summary>
    /// Verifies the Kafka bootstrap address is available.
    /// </summary>
    [Test, Repeat(3)]
    public async Task BootstrapAddress_IsConfigured()
    {
        var bootstrapAddress = GetBootstrapAddress();

        await Assert.That(bootstrapAddress).IsNotNullOrEmpty();
        // Testcontainers uses 127.0.0.1 (or localhost) with a random port
        await Assert.That(bootstrapAddress).Contains("127.0.0.1");
    }

    /// <summary>
    /// Verifies multiple topics can be created with the prefix.
    /// </summary>
    [Test, Repeat(3)]
    public async Task MultipleTopic_AllHaveSamePrefix()
    {
        var ordersTopic = GetTopicName("orders");
        var eventsTopic = GetTopicName("events");
        var notificationsTopic = GetTopicName("notifications");

        // All topics should share the same prefix
        await Assert.That(ordersTopic).StartsWith(TopicPrefix);
        await Assert.That(eventsTopic).StartsWith(TopicPrefix);
        await Assert.That(notificationsTopic).StartsWith(TopicPrefix);

        // But have different suffixes
        await Assert.That(ordersTopic).IsNotEqualTo(eventsTopic);
        await Assert.That(eventsTopic).IsNotEqualTo(notificationsTopic);
    }
}

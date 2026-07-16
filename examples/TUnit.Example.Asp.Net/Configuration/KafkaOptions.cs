namespace TUnit.Example.Asp.Net.Configuration;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicPrefix { get; set; } = "";
}

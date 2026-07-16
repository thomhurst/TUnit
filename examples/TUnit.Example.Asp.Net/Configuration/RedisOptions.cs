namespace TUnit.Example.Asp.Net.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "";
}

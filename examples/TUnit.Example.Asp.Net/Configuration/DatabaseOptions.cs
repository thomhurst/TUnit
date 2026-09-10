namespace TUnit.Example.Asp.Net.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "todos";
}

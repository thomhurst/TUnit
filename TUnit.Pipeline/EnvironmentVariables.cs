namespace TUnit.Pipeline;

public class EnvironmentVariables
{
    public static readonly string? NetVersion = Environment.GetEnvironmentVariable("NET_VERSION");

    public static readonly bool IsNetFramework = NetVersion?.StartsWith("net4") == true;
}
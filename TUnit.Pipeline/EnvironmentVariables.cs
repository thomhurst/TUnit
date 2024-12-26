namespace TUnit.Pipeline;

public class EnvironmentVariables
{
    public static readonly string? NetVersion = Environment.GetEnvironmentVariable("NET_VERSION");

    public static readonly bool IsNet472 = NetVersion == "net472";
}
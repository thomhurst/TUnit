namespace TUnit.Engine.Tests.Attributes;

public class SkipNetFrameworkAttribute(string reason) : SkipAttribute(reason)
{
    private static readonly string NetVersion = Environment.GetEnvironmentVariable("NET_VERSION") ?? "net9.0";

    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        return Task.FromResult(NetVersion == "net472");
    }
}
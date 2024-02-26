using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class RetryCountProperty(int count) : IProperty
{
    public int Count { get; } = count;
}

public class TimeoutProperty(TimeSpan timeout) : IProperty
{
    public TimeSpan Timeout { get; } = timeout;
}
using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class RetryCountProperty(int count) : IProperty
{
    public int Count { get; } = count;
}

internal class TimeoutProperty(TimeSpan timeout) : IProperty
{
    public TimeSpan Timeout { get; } = timeout;
}
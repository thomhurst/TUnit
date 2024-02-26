using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class RepeatCountProperty(int count) : IProperty
{
    public int Count { get; } = count;
}
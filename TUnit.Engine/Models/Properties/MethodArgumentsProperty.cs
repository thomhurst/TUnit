using System.Text.Json.Serialization;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Engine.Json;

namespace TUnit.Engine.Models.Properties;

[JsonConverter(typeof(ObjectArrayConverter))]
public class MethodArgumentsProperty : IProperty
{
    public object?[]? Arguments { get; }

    public MethodArgumentsProperty(object?[]? arguments)
    {
        Arguments = arguments;
    }
}
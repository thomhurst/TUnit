using System.Text.Json.Serialization;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Engine.Json;

namespace TUnit.Engine.Models.Properties;

[JsonConverter(typeof(ObjectArrayConverter))]
internal class ClassArgumentsProperty : IProperty
{
    public object?[]? Arguments { get; }

    internal ClassArgumentsProperty(object?[]? arguments)
    {
        Arguments = arguments;
    }
}
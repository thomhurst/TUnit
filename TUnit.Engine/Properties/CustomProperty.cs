using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Properties;

public record CustomProperty(string Name, string Value) : IProperty;
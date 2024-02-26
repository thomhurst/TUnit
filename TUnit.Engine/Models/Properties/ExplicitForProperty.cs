using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class ExplicitForProperty(string explicitFor) : IProperty
{
    public string ExplicitFor { get; } = explicitFor;
}
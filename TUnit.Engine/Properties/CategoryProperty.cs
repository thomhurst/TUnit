using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Properties;

public record CategoryProperty : KeyValuePairStringProperty
{
    public CategoryProperty(string category) : base("Category", category)
    {
    }
}
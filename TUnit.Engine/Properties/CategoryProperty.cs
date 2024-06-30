using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Properties;

internal record CategoryProperty : KeyValuePairStringProperty
{
    public CategoryProperty(string category) : base("Category", category)
    {
    }
}
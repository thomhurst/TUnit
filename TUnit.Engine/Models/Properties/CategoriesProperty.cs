using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class CategoriesProperty(IReadOnlyList<string>? categories) : IProperty
{
    public IReadOnlyList<string>? Categories { get; } = categories;
}
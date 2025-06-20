namespace TUnit.Core.Extensions;

public static class MetadataExtensions
{
    public static T? GetAttribute<T>(this MethodMetadata method) where T : Attribute
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..method.Attributes,
            ..method.Class.Attributes,
            ..method.Class.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<T>()
            .FirstOrDefault();
    }
}

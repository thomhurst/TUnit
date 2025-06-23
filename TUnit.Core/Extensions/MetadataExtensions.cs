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

    public static IEnumerable<T> GetAttributes<T>(this MethodMetadata method) where T : Attribute
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..method.Attributes,
            ..method.Class.Attributes,
            ..method.Class.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<T>();
    }

    public static T? GetAttribute<T>(this ClassMetadata classMetadata) where T : Attribute
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..classMetadata.Attributes,
            ..classMetadata.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<T>()
            .FirstOrDefault();
    }

    public static IEnumerable<T> GetAttributes<T>(this ClassMetadata classMetadata) where T : Attribute
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..classMetadata.Attributes,
            ..classMetadata.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<T>();
    }

    public static Type DeclaringType(this MethodMetadata method) => method.Class.Type;

    public static string MethodName(this MethodMetadata method) => method.Name;

    public static string DisplayName(this MethodMetadata method) => method.Name;

    public static bool IsGenericMethodDefinition(this MethodMetadata method) => method.GenericTypeCount > 0;

    // Interface-specific methods that don't require Attribute constraint
    public static IEnumerable<IDataAttribute> GetDataAttributes(this MethodMetadata method)
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..method.Attributes,
            ..method.Class.Attributes,
            ..method.Class.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<IDataAttribute>();
    }

    public static IEnumerable<IDataAttribute> GetDataAttributes(this ClassMetadata classMetadata)
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..classMetadata.Attributes,
            ..classMetadata.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<IDataAttribute>();
    }

    public static IEnumerable<IAsyncDataSourceGeneratorAttribute> GetAsyncDataSourceGeneratorAttributes(this MethodMetadata method)
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..method.Attributes,
            ..method.Class.Attributes,
            ..method.Class.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<IAsyncDataSourceGeneratorAttribute>();
    }

    public static IEnumerable<IAsyncDataSourceGeneratorAttribute> GetAsyncDataSourceGeneratorAttributes(this ClassMetadata classMetadata)
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..classMetadata.Attributes,
            ..classMetadata.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<IAsyncDataSourceGeneratorAttribute>();
    }

    public static IEnumerable<MethodDataSourceAttribute> GetMethodDataSourceAttributes(this MethodMetadata method)
    {
        IEnumerable<AttributeMetadata> attributes =
        [
            ..method.Attributes,
            ..method.Class.Attributes,
            ..method.Class.Assembly.Attributes
        ];

        return attributes.Select(x => x.Instance)
            .OfType<MethodDataSourceAttribute>();
    }
}

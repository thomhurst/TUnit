using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive-only model for a class with properties that have data source attributes.
/// Used by PropertyInjectionSourceGeneratorV2 for proper incremental caching.
/// </summary>
internal sealed record ClassPropertyInjectionModel : IEquatable<ClassPropertyInjectionModel>
{
    /// <summary>
    /// Fully qualified class name (e.g., "global::MyNamespace.MyClass")
    /// </summary>
    public required string ClassFullyQualifiedName { get; init; }

    /// <summary>
    /// Safe class name for use in file names (dots/generics replaced with underscores)
    /// </summary>
    public required string SafeClassName { get; init; }

    /// <summary>
    /// Properties with data source attributes
    /// </summary>
    public required EquatableArray<PropertyDataSourceModel> Properties { get; init; }

    public bool Equals(ClassPropertyInjectionModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ClassFullyQualifiedName == other.ClassFullyQualifiedName
            && SafeClassName == other.SafeClassName
            && Properties.Equals(other.Properties);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = ClassFullyQualifiedName.GetHashCode();
            hash = (hash * 397) ^ SafeClassName.GetHashCode();
            hash = (hash * 397) ^ Properties.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Primitive-only model for a property with a data source attribute.
/// </summary>
internal sealed record PropertyDataSourceModel : IEquatable<PropertyDataSourceModel>
{
    /// <summary>
    /// Property name
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Fully qualified property type (e.g., "global::System.String")
    /// </summary>
    public required string PropertyTypeFullyQualified { get; init; }

    /// <summary>
    /// Property type for typeof() expression (non-nullable)
    /// </summary>
    public required string PropertyTypeForTypeof { get; init; }

    /// <summary>
    /// Fully qualified containing type (the type that declares this property, may differ from class if inherited)
    /// </summary>
    public required string ContainingTypeFullyQualified { get; init; }

    /// <summary>
    /// CLR type name format for UnsafeAccessorType attribute (e.g., "Namespace.GenericType`1[[Namespace.TypeArg, Assembly]]")
    /// Only populated for generic containing types.
    /// </summary>
    public required string? ContainingTypeClrName { get; init; }

    /// <summary>
    /// The open generic type definition with type parameters (e.g., "global::NS.GenericBase&lt;T&gt;")
    /// Only populated for generic containing types.
    /// </summary>
    public required string? ContainingTypeOpenGeneric { get; init; }

    /// <summary>
    /// Comma-separated list of type parameter names (e.g., "T" or "T1, T2")
    /// Only populated for generic containing types.
    /// </summary>
    public required string? GenericTypeParameters { get; init; }

    /// <summary>
    /// Comma-separated list of concrete type arguments (e.g., "global::NS.ProviderType")
    /// Only populated for generic containing types.
    /// </summary>
    public required string? GenericTypeArguments { get; init; }

    /// <summary>
    /// Type parameter constraints (e.g., "where T : class" or "where T1 : class where T2 : struct")
    /// Only populated for generic containing types that have constraints.
    /// </summary>
    public required string? GenericTypeConstraints { get; init; }

    /// <summary>
    /// Whether the property has an init-only setter
    /// </summary>
    public required bool IsInitOnly { get; init; }

    /// <summary>
    /// Whether the containing type (where the property is declared) is a generic type
    /// </summary>
    public required bool IsContainingTypeGeneric { get; init; }

    /// <summary>
    /// Whether the property is static
    /// </summary>
    public required bool IsStatic { get; init; }

    /// <summary>
    /// If the property type is a type parameter in the original definition (e.g., "T"),
    /// this contains the type parameter name. Otherwise null.
    /// Used for UnsafeAccessor generation on generic types.
    /// </summary>
    public required string? PropertyTypeAsTypeParameter { get; init; }

    /// <summary>
    /// Whether the property type is a value type
    /// </summary>
    public required bool IsValueType { get; init; }

    /// <summary>
    /// Whether the property type is a nullable value type (e.g., int?)
    /// </summary>
    public required bool IsNullableValueType { get; init; }

    /// <summary>
    /// Fully qualified attribute type name
    /// </summary>
    public required string AttributeTypeName { get; init; }

    /// <summary>
    /// Constructor arguments formatted as code (e.g., "\"value\"", "42", "typeof(Foo)")
    /// </summary>
    public required EquatableArray<string> ConstructorArgs { get; init; }

    /// <summary>
    /// Named arguments as key-value pairs with formatted values
    /// </summary>
    public required EquatableArray<NamedArgModel> NamedArgs { get; init; }

    public bool Equals(PropertyDataSourceModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return PropertyName == other.PropertyName
            && PropertyTypeFullyQualified == other.PropertyTypeFullyQualified
            && PropertyTypeForTypeof == other.PropertyTypeForTypeof
            && ContainingTypeFullyQualified == other.ContainingTypeFullyQualified
            && ContainingTypeClrName == other.ContainingTypeClrName
            && ContainingTypeOpenGeneric == other.ContainingTypeOpenGeneric
            && GenericTypeParameters == other.GenericTypeParameters
            && GenericTypeArguments == other.GenericTypeArguments
            && GenericTypeConstraints == other.GenericTypeConstraints
            && IsInitOnly == other.IsInitOnly
            && IsContainingTypeGeneric == other.IsContainingTypeGeneric
            && IsStatic == other.IsStatic
            && PropertyTypeAsTypeParameter == other.PropertyTypeAsTypeParameter
            && IsValueType == other.IsValueType
            && IsNullableValueType == other.IsNullableValueType
            && AttributeTypeName == other.AttributeTypeName
            && ConstructorArgs.Equals(other.ConstructorArgs)
            && NamedArgs.Equals(other.NamedArgs);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = PropertyName.GetHashCode();
            hash = (hash * 397) ^ PropertyTypeFullyQualified.GetHashCode();
            hash = (hash * 397) ^ PropertyTypeForTypeof.GetHashCode();
            hash = (hash * 397) ^ ContainingTypeFullyQualified.GetHashCode();
            hash = (hash * 397) ^ (ContainingTypeClrName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (ContainingTypeOpenGeneric?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (GenericTypeParameters?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (GenericTypeArguments?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (GenericTypeConstraints?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ IsInitOnly.GetHashCode();
            hash = (hash * 397) ^ IsContainingTypeGeneric.GetHashCode();
            hash = (hash * 397) ^ IsStatic.GetHashCode();
            hash = (hash * 397) ^ (PropertyTypeAsTypeParameter?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ IsValueType.GetHashCode();
            hash = (hash * 397) ^ IsNullableValueType.GetHashCode();
            hash = (hash * 397) ^ AttributeTypeName.GetHashCode();
            hash = (hash * 397) ^ ConstructorArgs.GetHashCode();
            hash = (hash * 397) ^ NamedArgs.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Primitive model for a named argument (key-value pair).
/// </summary>
internal sealed record NamedArgModel : IEquatable<NamedArgModel>
{
    public required string Name { get; init; }
    public required string FormattedValue { get; init; }

    public bool Equals(NamedArgModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && FormattedValue == other.FormattedValue;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name.GetHashCode() * 397) ^ FormattedValue.GetHashCode();
        }
    }
}

/// <summary>
/// Model for IAsyncInitializer types with properties that return other IAsyncInitializer types.
/// Used for AOT-compatible nested initializer discovery.
/// </summary>
internal sealed record AsyncInitializerModel : IEquatable<AsyncInitializerModel>
{
    public required string TypeFullyQualified { get; init; }
    public required string SafeTypeName { get; init; }
    public required EquatableArray<InitializerPropertyModel> Properties { get; init; }

    public bool Equals(AsyncInitializerModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TypeFullyQualified == other.TypeFullyQualified
            && SafeTypeName == other.SafeTypeName
            && Properties.Equals(other.Properties);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TypeFullyQualified.GetHashCode();
            hash = (hash * 397) ^ SafeTypeName.GetHashCode();
            hash = (hash * 397) ^ Properties.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Model for a property that returns an IAsyncInitializer type.
/// </summary>
internal sealed record InitializerPropertyModel : IEquatable<InitializerPropertyModel>
{
    public required string PropertyName { get; init; }
    public required string PropertyTypeFullyQualified { get; init; }

    public bool Equals(InitializerPropertyModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return PropertyName == other.PropertyName
            && PropertyTypeFullyQualified == other.PropertyTypeFullyQualified;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (PropertyName.GetHashCode() * 397) ^ PropertyTypeFullyQualified.GetHashCode();
        }
    }
}

/// <summary>
/// Model for a concrete instantiation of a generic type discovered at compile time.
/// Used for generating source metadata for generic types (e.g., CustomWebApplicationFactory&lt;Program&gt;).
/// </summary>
internal sealed record ConcreteGenericTypeModel : IEquatable<ConcreteGenericTypeModel>
{
    /// <summary>
    /// Fully qualified name of the concrete type (e.g., "global::MyNamespace.GenericClass&lt;System.String&gt;")
    /// </summary>
    public required string ConcreteTypeFullyQualified { get; init; }

    /// <summary>
    /// Safe type name for use in file names and class names
    /// </summary>
    public required string SafeTypeName { get; init; }

    /// <summary>
    /// Whether this type implements IAsyncInitializer
    /// </summary>
    public required bool ImplementsIAsyncInitializer { get; init; }

    /// <summary>
    /// Whether this type (or its base types) has properties with IDataSourceAttribute
    /// </summary>
    public required bool HasDataSourceProperties { get; init; }

    /// <summary>
    /// Properties with IDataSourceAttribute (from this type and base types)
    /// </summary>
    public required EquatableArray<PropertyDataSourceModel> DataSourceProperties { get; init; }

    /// <summary>
    /// Properties that return IAsyncInitializer types
    /// </summary>
    public required EquatableArray<InitializerPropertyModel> InitializerProperties { get; init; }

    public bool Equals(ConcreteGenericTypeModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ConcreteTypeFullyQualified == other.ConcreteTypeFullyQualified
            && SafeTypeName == other.SafeTypeName
            && ImplementsIAsyncInitializer == other.ImplementsIAsyncInitializer
            && HasDataSourceProperties == other.HasDataSourceProperties
            && DataSourceProperties.Equals(other.DataSourceProperties)
            && InitializerProperties.Equals(other.InitializerProperties);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = ConcreteTypeFullyQualified.GetHashCode();
            hash = (hash * 397) ^ SafeTypeName.GetHashCode();
            hash = (hash * 397) ^ ImplementsIAsyncInitializer.GetHashCode();
            hash = (hash * 397) ^ HasDataSourceProperties.GetHashCode();
            hash = (hash * 397) ^ DataSourceProperties.GetHashCode();
            hash = (hash * 397) ^ InitializerProperties.GetHashCode();
            return hash;
        }
    }
}

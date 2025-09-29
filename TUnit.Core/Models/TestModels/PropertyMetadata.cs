using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Type} {Name})")]
public record PropertyMetadata : MemberMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    public required PropertyInfo ReflectionInfo { get; init; }

    public required bool IsStatic { get; init; }
    public bool IsNullable { get; init; }
    public required Func<object?, object?> Getter { get; init; }
    public required ClassMetadata ClassMetadata { get; set; }
    
    /// <summary>
    /// Metadata about the class that contains this property
    /// </summary>
    public required ClassMetadata ContainingTypeMetadata { get; set; }

    // AOT-friendly properties added by source generator

    /// <summary>
    /// Setter delegate that works even for init-only properties.
    /// Uses backing field access when necessary.
    /// </summary>
    public Action<object, object?>? Setter { get; init; }

    /// <summary>
    /// Indicates if this is an init-only property.
    /// </summary>
    public bool IsInitOnly { get; init; }

    /// <summary>
    /// Indicates if this is a required property.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Factory to create data source attribute if one exists.
    /// Avoids GetCustomAttributes reflection call.
    /// </summary>
    public Func<IDataSourceAttribute>? CreateDataSource { get; init; }
}

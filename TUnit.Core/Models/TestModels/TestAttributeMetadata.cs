using System.Diagnostics;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{AttributeType.Name} on {TargetElement}")]
public record TestAttributeMetadata
{
    /// <summary>
    /// The actual attribute instance
    /// </summary>
    public required Attribute Instance { get; init; }

    /// <summary>
    /// The type of the attribute
    /// </summary>
    public Type AttributeType => Instance.GetType();

    /// <summary>
    /// The element this attribute is applied to (Assembly, Class, Method, Property, Parameter, etc.)
    /// </summary>
    public required TestAttributeTarget TargetElement { get; init; }

    /// <summary>
    /// The name of the member this attribute is applied to (if applicable)
    /// </summary>
    public string? TargetMemberName { get; init; }

    /// <summary>
    /// The type containing the member this attribute is applied to (if applicable)
    /// </summary>
    public Type? TargetType { get; init; }

    /// <summary>
    /// Constructor arguments used when the attribute was applied
    /// </summary>
    public object?[]? ConstructorArguments { get; init; }

    /// <summary>
    /// Named arguments (properties) set on the attribute
    /// </summary>
    public IDictionary<string, object?>? NamedArguments { get; init; }

    /// <summary>
    /// Whether this attribute allows multiple instances on the same target
    /// </summary>
    public bool AllowMultiple => AttributeType.GetCustomAttribute<AttributeUsageAttribute>()?.AllowMultiple ?? false;

    /// <summary>
    /// Whether this attribute is inherited by derived classes
    /// </summary>
    public bool Inherited => AttributeType.GetCustomAttribute<AttributeUsageAttribute>()?.Inherited ?? true;

    /// <summary>
    /// Valid targets for this attribute type
    /// </summary>
    public AttributeTargets ValidTargets => AttributeType.GetCustomAttribute<AttributeUsageAttribute>()?.ValidOn ?? AttributeTargets.All;

    public virtual bool Equals(TestAttributeMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Instance.Equals(other.Instance) && 
               TargetElement == other.TargetElement &&
               TargetMemberName == other.TargetMemberName &&
               TargetType == other.TargetType;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Instance.GetHashCode();
            hashCode = (hashCode * 397) ^ TargetElement.GetHashCode();
            hashCode = (hashCode * 397) ^ (TargetMemberName?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (TargetType?.GetHashCode() ?? 0);
            return hashCode;
        }
    }
}

public enum TestAttributeTarget
{
    Assembly,
    Module,
    Class,
    Struct,
    Enum,
    Interface,
    Delegate,
    Method,
    Constructor,
    Property,
    Field,
    Event,
    Parameter,
    ReturnValue,
    GenericParameter
}
using System;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// Equatable model for a static abstract interface member that needs an explicit
/// implementation stub in the generated mock class.
/// </summary>
internal sealed record MockStaticAbstractMemberModel : IEquatable<MockStaticAbstractMemberModel>
{
    /// <summary>
    /// The fully qualified name of the interface declaring this static abstract member.
    /// Used for explicit interface implementation syntax.
    /// </summary>
    public string InterfaceFullyQualifiedName { get; init; } = "";

    /// <summary>
    /// The member name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The fully qualified return type (for methods) or property type.
    /// </summary>
    public string ReturnType { get; init; } = "void";

    /// <summary>
    /// True for void methods.
    /// </summary>
    public bool IsVoid { get; init; }

    /// <summary>
    /// True if this is a property rather than a method.
    /// </summary>
    public bool IsProperty { get; init; }

    /// <summary>
    /// True if the property has a getter.
    /// </summary>
    public bool HasGetter { get; init; }

    /// <summary>
    /// True if the property has a setter.
    /// </summary>
    public bool HasSetter { get; init; }

    /// <summary>
    /// Method parameters.
    /// </summary>
    public EquatableArray<MockParameterModel> Parameters { get; init; } = EquatableArray<MockParameterModel>.Empty;

    /// <summary>
    /// Generic type parameters with constraints.
    /// </summary>
    public EquatableArray<MockTypeParameterModel> TypeParameters { get; init; } = EquatableArray<MockTypeParameterModel>.Empty;

    public bool Equals(MockStaticAbstractMemberModel? other)
    {
        if (other is null) return false;
        return InterfaceFullyQualifiedName == other.InterfaceFullyQualifiedName
            && Name == other.Name
            && ReturnType == other.ReturnType
            && IsVoid == other.IsVoid
            && IsProperty == other.IsProperty
            && HasGetter == other.HasGetter
            && HasSetter == other.HasSetter
            && Parameters.Equals(other.Parameters)
            && TypeParameters.Equals(other.TypeParameters);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + InterfaceFullyQualifiedName.GetHashCode();
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + ReturnType.GetHashCode();
            hash = hash * 31 + Parameters.GetHashCode();
            hash = hash * 31 + TypeParameters.GetHashCode();
            return hash;
        }
    }
}

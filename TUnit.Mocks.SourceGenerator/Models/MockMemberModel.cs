using System;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// Equatable model for a method or property member to be mocked.
/// </summary>
internal sealed record MockMemberModel : IEquatable<MockMemberModel>
{
    public string Name { get; init; } = "";
    public int MemberId { get; init; }
    public string ReturnType { get; init; } = "void";
    public string UnwrappedReturnType { get; init; } = "void";
    public bool IsVoid { get; init; }
    public bool IsAsync { get; init; }
    public bool IsValueTask { get; init; }
    public bool IsProperty { get; init; }
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public int SetterMemberId { get; init; }
    public bool IsIndexer { get; init; }
    public bool IsGenericMethod { get; init; }
    public EquatableArray<MockParameterModel> Parameters { get; init; } = EquatableArray<MockParameterModel>.Empty;
    public EquatableArray<MockTypeParameterModel> TypeParameters { get; init; } = EquatableArray<MockTypeParameterModel>.Empty;
    public string? ExplicitInterfaceName { get; init; }
    public string NullableAnnotation { get; init; } = "None";
    public string SmartDefault { get; init; } = "default!";
    public string UnwrappedSmartDefault { get; init; } = "default!";
    public bool IsAbstractMember { get; init; }
    public bool IsVirtualMember { get; init; }
    public bool IsProtected { get; init; }

    public bool Equals(MockMemberModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && MemberId == other.MemberId
            && ReturnType == other.ReturnType
            && UnwrappedReturnType == other.UnwrappedReturnType
            && IsVoid == other.IsVoid
            && IsAsync == other.IsAsync
            && IsValueTask == other.IsValueTask
            && IsProperty == other.IsProperty
            && HasGetter == other.HasGetter
            && HasSetter == other.HasSetter
            && SetterMemberId == other.SetterMemberId
            && IsIndexer == other.IsIndexer
            && IsGenericMethod == other.IsGenericMethod
            && Parameters.Equals(other.Parameters)
            && TypeParameters.Equals(other.TypeParameters)
            && ExplicitInterfaceName == other.ExplicitInterfaceName
            && NullableAnnotation == other.NullableAnnotation
            && SmartDefault == other.SmartDefault
            && UnwrappedSmartDefault == other.UnwrappedSmartDefault
            && IsAbstractMember == other.IsAbstractMember
            && IsVirtualMember == other.IsVirtualMember
            && IsProtected == other.IsProtected;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + MemberId;
            hash = hash * 31 + ReturnType.GetHashCode();
            hash = hash * 31 + Parameters.GetHashCode();
            return hash;
        }
    }
}

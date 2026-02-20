using System;

namespace TUnit.Mock.SourceGenerator.Models;

/// <summary>
/// Fully equatable value model for a mocked type. Extracted from ISymbol in the transform phase.
/// NEVER store ISymbol in pipeline - this record must be fully equatable for incremental caching.
/// </summary>
internal sealed record MockTypeModel : IEquatable<MockTypeModel>
{
    public string FullyQualifiedName { get; init; } = "";
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public bool IsInterface { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsPartial { get; init; }
    public bool IsPartialMock { get; init; }
    public EquatableArray<MockMemberModel> Methods { get; init; } = EquatableArray<MockMemberModel>.Empty;
    public EquatableArray<MockMemberModel> Properties { get; init; } = EquatableArray<MockMemberModel>.Empty;
    public EquatableArray<MockEventModel> Events { get; init; } = EquatableArray<MockEventModel>.Empty;
    public EquatableArray<string> AllInterfaces { get; init; } = EquatableArray<string>.Empty;
    public EquatableArray<MockConstructorModel> Constructors { get; init; } = EquatableArray<MockConstructorModel>.Empty;

    public bool Equals(MockTypeModel? other)
    {
        if (other is null) return false;
        return FullyQualifiedName == other.FullyQualifiedName
            && Name == other.Name
            && Namespace == other.Namespace
            && IsInterface == other.IsInterface
            && IsAbstract == other.IsAbstract
            && IsPartial == other.IsPartial
            && IsPartialMock == other.IsPartialMock
            && Methods.Equals(other.Methods)
            && Properties.Equals(other.Properties)
            && Events.Equals(other.Events)
            && AllInterfaces.Equals(other.AllInterfaces)
            && Constructors.Equals(other.Constructors);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + IsPartialMock.GetHashCode();
            hash = hash * 31 + Methods.GetHashCode();
            hash = hash * 31 + Properties.GetHashCode();
            hash = hash * 31 + Events.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Equatable model for a constructor of a mocked class.
/// </summary>
internal sealed record MockConstructorModel : IEquatable<MockConstructorModel>
{
    public EquatableArray<MockParameterModel> Parameters { get; init; } = EquatableArray<MockParameterModel>.Empty;

    public bool Equals(MockConstructorModel? other)
    {
        if (other is null) return false;
        return Parameters.Equals(other.Parameters);
    }

    public override int GetHashCode() => Parameters.GetHashCode();
}

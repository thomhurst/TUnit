using System;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// Fully equatable value model for a mocked type. Extracted from ISymbol in the transform phase.
/// NEVER store ISymbol in pipeline - this record must be fully equatable for incremental caching.
/// </summary>
internal sealed record MockTypeModel : IEquatable<MockTypeModel>
{
    public string FullyQualifiedName { get; init; } = "";
    public string OpenGenericTypeOfExpression { get; init; } = "";
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public bool IsInterface { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsPartialMock { get; init; }
    public bool IsDelegateType { get; init; }
    public bool IsWrapMock { get; init; }
    public EquatableArray<MockTypeParameterModel> TypeParameters { get; init; } = EquatableArray<MockTypeParameterModel>.Empty;
    public EquatableArray<MockMemberModel> Methods { get; init; } = EquatableArray<MockMemberModel>.Empty;
    public EquatableArray<MockMemberModel> Properties { get; init; } = EquatableArray<MockMemberModel>.Empty;
    public EquatableArray<MockEventModel> Events { get; init; } = EquatableArray<MockEventModel>.Empty;
    public EquatableArray<string> AllInterfaces { get; init; } = EquatableArray<string>.Empty;
    public EquatableArray<string> AdditionalInterfaceNames { get; init; } = EquatableArray<string>.Empty;
    public EquatableArray<MockConstructorModel> Constructors { get; init; } = EquatableArray<MockConstructorModel>.Empty;
    public bool HasStaticAbstractMembers { get; init; }

    /// <summary>
    /// True for the per-(primary, additional-interface) PAIR model that generates the setup/verify
    /// extension surface for one additional interface of a multi-type mock. Pair models carry the
    /// PRIMARY type's identity (<see cref="FullyQualifiedName"/> = T1, so extensions target
    /// <c>Mock&lt;T1&gt;</c>) but the ADDITIONAL interface's standalone members
    /// (<see cref="AdditionalInterfaceNames"/> holds that single interface), whose member IDs are
    /// local ordinals translated to the owning impl's union IDs at runtime via the map the factory
    /// registers on the engine. Equal pair models from different combos (Mock.Of&lt;T1,T2&gt; and
    /// Mock.Of&lt;T1,T2,T3&gt;) dedupe in the pipeline, so each pair surface is emitted exactly once.
    /// </summary>
    public bool IsSecondaryMemberSurface { get; init; }

    /// <summary>
    /// On multi-type models only: one entry per <see cref="AdditionalInterfaceNames"/> element,
    /// mapping that interface's standalone member IDs (the pair surface's local ordinals,
    /// indexed positionally) to this combo's union member IDs (-1 = unmapped). Emitted into the
    /// factory as <c>engine.RegisterSecondaryInterface(typeof(Tn), map)</c>.
    /// </summary>
    public EquatableArray<EquatableArray<int>> SecondaryMemberIdMaps { get; init; } = EquatableArray<EquatableArray<int>>.Empty;

    /// <summary>
    /// True if the mocked type's effective accessibility is public (the type itself and all
    /// containing types are public). When false, generated wrapper/extension types must be
    /// declared <c>internal</c> to avoid CS9338/CS0051 inconsistent accessibility errors.
    /// </summary>
    public bool IsPublic { get; init; } = true;

    /// <summary>
    /// True when generation must fall back to <c>TUnit.Mocks.Generated[.{Namespace}]</c>
    /// because the original namespace already declares a type whose name would collide
    /// with one of the generator's emitted public type names.
    /// </summary>
    public bool UseFallbackNamespace { get; init; }

    /// <summary>The C# visibility keyword to emit on generated wrapper/extension types.</summary>
    public string Visibility => IsPublic ? "public" : "internal";

    public bool Equals(MockTypeModel? other)
    {
        if (other is null) return false;
        return FullyQualifiedName == other.FullyQualifiedName
            && OpenGenericTypeOfExpression == other.OpenGenericTypeOfExpression
            && Name == other.Name
            && Namespace == other.Namespace
            && IsInterface == other.IsInterface
            && IsAbstract == other.IsAbstract
            && IsPartialMock == other.IsPartialMock
            && IsDelegateType == other.IsDelegateType
            && IsWrapMock == other.IsWrapMock
            && IsPublic == other.IsPublic
            && UseFallbackNamespace == other.UseFallbackNamespace
            && TypeParameters.Equals(other.TypeParameters)
            && Methods.Equals(other.Methods)
            && Properties.Equals(other.Properties)
            && Events.Equals(other.Events)
            && AllInterfaces.Equals(other.AllInterfaces)
            && AdditionalInterfaceNames.Equals(other.AdditionalInterfaceNames)
            && Constructors.Equals(other.Constructors)
            && HasStaticAbstractMembers == other.HasStaticAbstractMembers
            && IsSecondaryMemberSurface == other.IsSecondaryMemberSurface
            && SecondaryMemberIdMaps.Equals(other.SecondaryMemberIdMaps);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + OpenGenericTypeOfExpression.GetHashCode();
            hash = hash * 31 + IsPartialMock.GetHashCode();
            hash = hash * 31 + IsDelegateType.GetHashCode();
            hash = hash * 31 + IsWrapMock.GetHashCode();
            hash = hash * 31 + IsPublic.GetHashCode();
            hash = hash * 31 + UseFallbackNamespace.GetHashCode();
            hash = hash * 31 + TypeParameters.GetHashCode();
            hash = hash * 31 + Methods.GetHashCode();
            hash = hash * 31 + Properties.GetHashCode();
            hash = hash * 31 + Events.GetHashCode();
            hash = hash * 31 + AdditionalInterfaceNames.GetHashCode();
            hash = hash * 31 + HasStaticAbstractMembers.GetHashCode();
            hash = hash * 31 + IsSecondaryMemberSurface.GetHashCode();
            hash = hash * 31 + SecondaryMemberIdMaps.GetHashCode();
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

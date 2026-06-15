using System;
using System.Linq;

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
    /// <summary>
    /// When true, the explicit interface impl can safely delegate to the public method
    /// with the same name (return types are compatible, e.g. IEnumerable.GetEnumerator → IEnumerable&lt;T&gt;.GetEnumerator).
    /// When false, the explicit impl needs its own engine dispatch (incompatible return types).
    /// Only meaningful when <see cref="ExplicitInterfaceName"/> is not null.
    /// </summary>
    public bool ExplicitInterfaceCanDelegate { get; init; }
    /// <summary>
    /// The fully qualified name of the interface that declares this member.
    /// Used by the wrapper type builder for correct explicit interface forwarding.
    /// </summary>
    public string? DeclaringInterfaceName { get; init; }

    /// <summary>
    /// Extra interface FQNs whose identically-signed member slot this single member also
    /// satisfies, but which the generated wrapper type must implement with its own explicit
    /// interface forward. Populated when a base-interface member is hidden by a <c>new</c>
    /// member, or when identical members are inherited from multiple interfaces: the shared
    /// impl implements every slot implicitly, but the wrapper forwards explicitly, and an
    /// explicit impl satisfies only the one slot it names — so each shadowed base slot needs
    /// its own forward or the build fails with CS0535 (#6252). Empty in the common case and
    /// consumed only by <see cref="Builders.MockWrapperTypeBuilder"/> (single-interface mocks).
    /// </summary>
    public EquatableArray<string> AdditionalExplicitInterfaceNames { get; init; } = EquatableArray<string>.Empty;
    public string NullableAnnotation { get; init; } = "None";
    public string SmartDefault { get; init; } = "default!";
    public string UnwrappedSmartDefault { get; init; } = "default!";
    public bool IsAbstractMember { get; init; }
    public bool IsVirtualMember { get; init; }
    public string OverrideAccessModifier { get; init; } = "public";
    public string GetterAccessModifier { get; init; } = "";
    public string SetterAccessModifier { get; init; } = "";
    public bool IsRefStructReturn { get; init; }
    public bool IsStaticAbstract { get; init; }
    public string? AutoMockFactoryMethod { get; init; }

    /// <summary>
    /// Which type in a multi-type mock owns this member: 0 = the primary type,
    /// n = 1-based index into <see cref="MockTypeModel.AdditionalInterfaceNames"/>.
    /// Always 0 for single-type mocks. Members shared between the primary and an
    /// additional interface keep the first occurrence (0) — dedup is first-wins.
    /// </summary>
    public int OwnerTypeIndex { get; init; }

    /// <summary>
    /// True when the (unwrapped) return type is an interface that has static abstract members
    /// without a most specific implementation. Such types cannot be used as generic type arguments
    /// (CS8920), so the generator must fall back to the void wrapper path.
    /// </summary>
    public bool IsReturnTypeStaticAbstractInterface { get; init; }

    /// <summary>
    /// True when this method's <c>out</c> parameters must be kept in the generated extension
    /// method signature to avoid CS0111 collisions with a sibling overload that shares the same
    /// matchable-parameter signature (e.g. <c>BlobClient.GenerateSasUri(perms, expires)</c> vs
    /// <c>GenerateSasUri(perms, expires, out string stringToSign)</c>). When true, callers must
    /// pass <c>out _</c> at the call site for the disambiguated overload.
    /// Computed transiently inside <see cref="Builders.MockMembersBuilder.Build"/> by examining
    /// the full method set; never set on models flowing through the incremental pipeline.
    /// Excluded from <see cref="Equals(MockMemberModel?)"/> / <see cref="GetHashCode"/> because
    /// it is a derived per-build flag, not part of model identity.
    /// </summary>
    public bool KeepOutParamsInExtensionSignature { get; init; }

    /// <summary>
    /// For methods returning ReadOnlySpan&lt;T&gt; or Span&lt;T&gt;, the fully qualified element type.
    /// Null for non-span return types. Used to support configurable span return values via array conversion.
    /// </summary>
    public string? SpanReturnElementType { get; init; }

    /// <summary>
    /// The C# attribute syntax to copy onto generated overrides/forwards when the source member
    /// carries <see cref="System.ObsoleteAttribute"/>. Empty string when the member is not obsolete.
    /// Example: <c>[global::System.Obsolete("Use V2", false)]</c>. Carrying the attribute through
    /// silences CS0612/CS0618 warnings inside the generated body and resolves CS0672 on overrides.
    /// </summary>
    public string ObsoleteAttribute { get; init; } = "";

    /// <summary>Per-accessor [Obsolete] attribute, only set when the property symbol itself
    /// is not obsolete but the getter is. Lets the generator preserve asymmetric cases like
    /// <c>{ [Obsolete] get; set; }</c> without forcing the whole property to be marked.</summary>
    public string GetterObsoleteAttribute { get; init; } = "";

    /// <summary>Per-accessor [Obsolete] attribute, set when only the setter is obsolete.</summary>
    public string SetterObsoleteAttribute { get; init; } = "";

    /// <summary>
    /// Returns true if the method has any non-out ref struct parameters.
    /// Computed from <see cref="Parameters"/> — does not participate in equality.
    /// </summary>
    public bool HasRefStructParams => Parameters.Any(p => p.IsRefStruct && p.Direction != ParameterDirection.Out);

    /// <summary>
    /// True when this property can appear as an extension property on the generated
    /// <c>Mock&lt;T&gt;</c> setup surface (ref-struct and static-abstract-interface returns can't
    /// flow through <c>PropertyMockCall&lt;T&gt;</c>; indexers get Item/SetItem methods instead).
    /// Shared between the members builder and the secondary-surface rename logic so the two
    /// definitions of "exposed property" can't drift. Derived — not part of equality.
    /// </summary>
    public bool IsConfigurableSurfaceProperty
        => !IsIndexer && !IsRefStructReturn && !IsReturnTypeStaticAbstractInterface && (HasGetter || HasSetter);

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
            && ExplicitInterfaceCanDelegate == other.ExplicitInterfaceCanDelegate
            && DeclaringInterfaceName == other.DeclaringInterfaceName
            && AdditionalExplicitInterfaceNames.Equals(other.AdditionalExplicitInterfaceNames)
            && NullableAnnotation == other.NullableAnnotation
            && SmartDefault == other.SmartDefault
            && UnwrappedSmartDefault == other.UnwrappedSmartDefault
            && IsAbstractMember == other.IsAbstractMember
            && IsVirtualMember == other.IsVirtualMember
            && OverrideAccessModifier == other.OverrideAccessModifier
            && GetterAccessModifier == other.GetterAccessModifier
            && SetterAccessModifier == other.SetterAccessModifier
            && IsRefStructReturn == other.IsRefStructReturn
            && IsStaticAbstract == other.IsStaticAbstract
            && AutoMockFactoryMethod == other.AutoMockFactoryMethod
            && OwnerTypeIndex == other.OwnerTypeIndex
            && IsReturnTypeStaticAbstractInterface == other.IsReturnTypeStaticAbstractInterface
            && SpanReturnElementType == other.SpanReturnElementType
            && ObsoleteAttribute == other.ObsoleteAttribute
            && GetterObsoleteAttribute == other.GetterObsoleteAttribute
            && SetterObsoleteAttribute == other.SetterObsoleteAttribute;
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
            hash = hash * 31 + IsStaticAbstract.GetHashCode();
            hash = hash * 31 + OverrideAccessModifier.GetHashCode();
            hash = hash * 31 + GetterAccessModifier.GetHashCode();
            hash = hash * 31 + SetterAccessModifier.GetHashCode();
            hash = hash * 31 + (AutoMockFactoryMethod?.GetHashCode() ?? 0);
            hash = hash * 31 + OwnerTypeIndex;
            hash = hash * 31 + IsReturnTypeStaticAbstractInterface.GetHashCode();
            hash = hash * 31 + (ExplicitInterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + ExplicitInterfaceCanDelegate.GetHashCode();
            hash = hash * 31 + (DeclaringInterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + AdditionalExplicitInterfaceNames.GetHashCode();
            hash = hash * 31 + ObsoleteAttribute.GetHashCode();
            hash = hash * 31 + GetterObsoleteAttribute.GetHashCode();
            hash = hash * 31 + SetterObsoleteAttribute.GetHashCode();
            return hash;
        }
    }
}

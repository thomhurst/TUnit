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
    public string NullableAnnotation { get; init; } = "None";
    public string SmartDefault { get; init; } = "default!";
    public string UnwrappedSmartDefault { get; init; } = "default!";
    public bool IsAbstractMember { get; init; }
    public bool IsVirtualMember { get; init; }
    public bool IsProtected { get; init; }
    public bool IsRefStructReturn { get; init; }
    public bool IsStaticAbstract { get; init; }

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
    /// Returns true if the method has any non-out ref struct parameters.
    /// Computed from <see cref="Parameters"/> — does not participate in equality.
    /// </summary>
    public bool HasRefStructParams => Parameters.Any(p => p.IsRefStruct && p.Direction != ParameterDirection.Out);

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
            && NullableAnnotation == other.NullableAnnotation
            && SmartDefault == other.SmartDefault
            && UnwrappedSmartDefault == other.UnwrappedSmartDefault
            && IsAbstractMember == other.IsAbstractMember
            && IsVirtualMember == other.IsVirtualMember
            && IsProtected == other.IsProtected
            && IsRefStructReturn == other.IsRefStructReturn
            && IsStaticAbstract == other.IsStaticAbstract
            && IsReturnTypeStaticAbstractInterface == other.IsReturnTypeStaticAbstractInterface
            && SpanReturnElementType == other.SpanReturnElementType;
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
            hash = hash * 31 + IsReturnTypeStaticAbstractInterface.GetHashCode();
            hash = hash * 31 + (ExplicitInterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + ExplicitInterfaceCanDelegate.GetHashCode();
            hash = hash * 31 + (DeclaringInterfaceName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

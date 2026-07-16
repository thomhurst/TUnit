using System;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// A distinct interface slot that a single shared member also satisfies, but which the generated
/// wrapper type must implement with its own explicit interface forward (#6252). Carries the slot's
/// own accessor presence so the forward emits only the accessors that <i>this</i> interface declares:
/// when a base property is hidden by a <c>new</c> member with a different accessor set (e.g. base
/// <c>{ get; }</c>, derived <c>new { get; set; }</c>), the shared model holds the <em>merged</em>
/// accessors for the implicit impl, but each explicit forward must match its own slot exactly or the
/// build fails with CS0550 (#6263). For methods these flags are unused.
/// </summary>
internal sealed record MockExplicitInterfaceSlot : IEquatable<MockExplicitInterfaceSlot>
{
    public string InterfaceName { get; init; } = "";
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }

    public bool Equals(MockExplicitInterfaceSlot? other)
    {
        if (other is null) return false;
        return InterfaceName == other.InterfaceName
            && HasGetter == other.HasGetter
            && HasSetter == other.HasSetter;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + InterfaceName.GetHashCode();
            hash = hash * 31 + HasGetter.GetHashCode();
            hash = hash * 31 + HasSetter.GetHashCode();
            return hash;
        }
    }
}

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
internal sealed record MockExplicitInterfaceSlot
{
    public string InterfaceName { get; init; } = "";
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
}

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for issue #5841.
/// IsEquivalentTo must not invoke property/field getters whose return type is a ref struct
/// (e.g. ReadOnlySpan&lt;T&gt;, reachable via ReadOnlyMemory&lt;T&gt;.Span). Ref structs cannot
/// be boxed, so RuntimeMethodInfo.Invoke throws NotSupportedException.
/// </summary>
public class Tests5841
{
    public class HasMemoryProperty
    {
        public ReadOnlyMemory<byte> Memory { get; init; }
    }

    public class HasSpanProperty
    {
        public ReadOnlySpan<byte> Span => default;
        public int Value { get; init; }
    }

    [Test]
    public async Task IsEquivalentTo_with_ReadOnlyMemory_property_does_not_invoke_Span_getter()
    {
        var a = new HasMemoryProperty { Memory = new byte[] { 1, 2, 3 } };
        var b = new HasMemoryProperty { Memory = new byte[] { 1, 2, 3 } };

        await Assert.That(a).IsEquivalentTo(b);
    }

    [Test]
    public async Task IsEquivalentTo_skips_ref_struct_returning_property()
    {
        var a = new HasSpanProperty { Value = 7 };
        var b = new HasSpanProperty { Value = 7 };

        await Assert.That(a).IsEquivalentTo(b);
    }
}

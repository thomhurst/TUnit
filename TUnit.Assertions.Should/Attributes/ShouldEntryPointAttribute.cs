using System;

namespace TUnit.Assertions.Should.Attributes;

/// <summary>
/// Assembly-level toggle to suppress the <c>Should()</c> entry extension methods
/// when consumers also reference FluentAssertions (which provides its own
/// <c>Should()</c> on <c>T</c>) and want to avoid the ambiguity. Should-flavored
/// extension methods (<c>BeEqualTo</c>, <c>HaveCount</c>, etc.) remain available
/// off any <see cref="Core.IShouldSource{T}"/> the consumer constructs manually.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ShouldEntryPointAttribute : Attribute
{
    public ShouldEntryPointAttribute(bool enabled) => Enabled = enabled;

    public bool Enabled { get; }
}

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Holds all pre-generated C# code for one test method within a per-class TestSource.
/// All fields are primitives/strings for incremental caching (no ISymbol references).
/// </summary>
public sealed class TestMethodSourceCode : IEquatable<TestMethodSourceCode>
{
    /// <summary>
    /// Safe unique identifier within the class (handles overloads via parameter types).
    /// e.g. "MyMethod" or "MyMethod__Int32_String"
    /// </summary>
    public required string MethodId { get; init; }

    /// <summary>
    /// Pre-generated metadata block for GetTests().
    /// Contains: var metadata_{id} = new TestMetadata&lt;T&gt; { ... }; metadata_{id}.UseRuntimeDataGeneration(...);
    /// </summary>
    public required string MetadataCode { get; init; }

    /// <summary>
    /// Pre-generated descriptor block for EnumerateTestDescriptors().
    /// Contains: yield return new TestDescriptor { ..., Materializer = __Materialize_{id} };
    /// </summary>
    public required string DescriptorCode { get; init; }

    /// <summary>
    /// Pre-generated full __InvokeTest_{id} static method (signature + body).
    /// </summary>
    public required string InvokeTestMethod { get; init; }

    /// <summary>
    /// Pre-generated full __Materialize_{id} static method (signature + body).
    /// Only called when --treenode-filter is used, so never JIT'd in normal runs.
    /// </summary>
    public required string MaterializerMethod { get; init; }

    /// <summary>
    /// Index into the shared AttributeGroups array in ClassTestGroup.
    /// Methods with identical attribute factory bodies share the same index.
    /// </summary>
    public required int AttributeGroupIndex { get; init; }

    public bool Equals(TestMethodSourceCode? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return MethodId == other.MethodId &&
               MetadataCode == other.MetadataCode &&
               DescriptorCode == other.DescriptorCode &&
               InvokeTestMethod == other.InvokeTestMethod &&
               MaterializerMethod == other.MaterializerMethod &&
               AttributeGroupIndex == other.AttributeGroupIndex;
    }

    public override bool Equals(object? obj) => Equals(obj as TestMethodSourceCode);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + MethodId.GetHashCode();
            hash = hash * 31 + MetadataCode.GetHashCode();
            hash = hash * 31 + DescriptorCode.GetHashCode();
            hash = hash * 31 + InvokeTestMethod.GetHashCode();
            hash = hash * 31 + MaterializerMethod.GetHashCode();
            hash = hash * 31 + AttributeGroupIndex;
            return hash;
        }
    }
}

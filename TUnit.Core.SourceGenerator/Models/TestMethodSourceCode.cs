namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Holds all pre-generated C# code for one test method within a per-class TestSource.
/// All fields are primitives/strings for incremental caching (no ISymbol references).
/// </summary>
public sealed record TestMethodSourceCode
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
    /// The metadata code here intentionally duplicates MetadataCode rather than extracting a shared
    /// BuildMetadata_{id} helper, because such helpers would each be JIT-compiled (adding 10,000 JITs
    /// in the repro scenario), negating the optimization. Materializers are never JIT'd in normal runs
    /// (only when --treenode-filter is used), so the duplication has zero runtime cost.
    /// </summary>
    public required string MaterializerMethod { get; init; }

    /// <summary>
    /// Index into the shared AttributeGroups array in ClassTestGroup.
    /// Methods with identical attribute factory bodies share the same index.
    /// </summary>
    public required int AttributeGroupIndex { get; init; }

    /// <summary>
    /// Sequential 0-based index of this method within its class, used for
    /// switch-based dispatch in the Materialize method and for MethodIndex
    /// in TestRegistrationEntry.
    /// </summary>
    public required int MethodIndex { get; init; }

    /// <summary>
    /// Pre-generated TestRegistrationEntry struct initializer for the Entries array.
    /// Contains pure data (no delegates or method references).
    /// </summary>
    public required string RegistrationEntryCode { get; init; }

    /// <summary>
    /// Pre-generated switch case body for the Materialize method.
    /// Creates TestMetadata for this specific method index.
    /// </summary>
    public required string MaterializeSwitchCaseCode { get; init; }

    /// <summary>
    /// Pre-generated switch case body for the consolidated __InvokeByIndex method.
    /// Contains the actual test method invocation for this method index.
    /// </summary>
    public required string InvokeSwitchCaseCode { get; init; }
}

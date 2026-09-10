using Microsoft.CodeAnalysis;
using Microsoft.Kiota.Abstractions;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression tests for #6456: mocking Microsoft.Kiota.Abstractions.IRequestAdapter, whose
/// generic methods carry an interface constraint (where ModelType : IParsable). Explicit
/// interface implementations inherit constraints, but T? in them is parsed as Nullable&lt;T&gt;
/// unless a 'where T : default' clause is emitted — which is required whenever T is not known
/// to be a reference or value type, not just when T is fully unconstrained.
/// </summary>
public class KiotaMockTests : SnapshotTestBase
{
    private const string Source = """
        using Microsoft.Kiota.Abstractions;
        using TUnit.Mocks;

        [assembly: GenerateMock(typeof(IRequestAdapter))]
        """;

    private static IEnumerable<MetadataReference> KiotaReferences() =>
        [MetadataReference.CreateFromFile(typeof(IRequestAdapter).Assembly.Location)];

    [Test]
    public void KiotaIRequestAdapter_ConstrainedGenericMethod_EmitsDefaultConstraint()
    {
        var output = string.Join("\n", RunGenerator(Source, KiotaReferences()));

        // SendAsync<ModelType> has 'where ModelType : IParsable' and returns Task<ModelType?>;
        // its explicit implementation must carry 'where ModelType : default'.
        AssertContains(output,
            "global::Microsoft.Kiota.Abstractions.IRequestAdapter.SendAsync<ModelType>");
        AssertContains(output, "where ModelType : default");
    }

    [Test]
    public void KiotaIRequestAdapter_GeneratedMock_HasNoConstraintErrors()
    {
        var errors = GetGeneratedCompilationErrors(Source, KiotaReferences());

        // The exact errors reported in #6456. Other diagnostics (e.g. CS1520/CS1513 from C# 14
        // extension() blocks that the test-pinned Roslyn cannot parse) are infra limitations
        // and intentionally not asserted here — see SnapshotTestBase.
        foreach (var id in (string[])["CS0539", "CS0535", "CS0453", "CS0314"])
        {
            var match = errors.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal));
            if (match is not null)
            {
                throw new InvalidOperationException($"Generated code produced {id}: {match}");
            }
        }
    }

    private static void AssertContains(string text, string expected)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected generated output to contain: {expected}");
        }
    }
}

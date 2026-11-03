using TUnit.Inference;

namespace TUnit.SourceGenerator.IncrementalTests;

internal class GenericClassInferencePolicyTests
{
    [Test]
    [Arguments(true, true, GenericClassInferencePolicy.RowFallback.None)]
    [Arguments(true, false, GenericClassInferencePolicy.RowFallback.None)]
    [Arguments(false, true, GenericClassInferencePolicy.RowFallback.NoClassRow)]
    [Arguments(false, false, GenericClassInferencePolicy.RowFallback.AllRows)]
    public async Task Constructor_rows_use_the_matching_partition_before_fallback(
        bool matchingRow, bool literalRows, GenericClassInferencePolicy.RowFallback expected)
    {
        await Assert.That(GenericClassInferencePolicy.SelectRowFallback(matchingRow, literalRows)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(true, false, true)]
    [Arguments(false, true, false)]
    [Arguments(null, true, true)]
    [Arguments(null, false, false)]
    public async Task Uninferred_rows_join_only_when_the_scope_allows_them(
        bool? matching, bool includeUninferred, bool expected)
    {
        await Assert.That(GenericClassInferencePolicy.IncludesRow(matching, includeUninferred)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, 1, 1, false)]
    [Arguments(1, 1, 2, true)]
    [Arguments(2, 1, 2, false)]
    [Arguments(2, 1, 3, true)]
    public async Task Open_class_or_method_types_cannot_form_a_combined_instantiation(
        int classArity, int methodArity, int supplied, bool expected)
    {
        await Assert.That(GenericClassInferencePolicy.IsCompletePair(classArity, methodArity, supplied)).IsEqualTo(expected);
    }

    [Test]
    public async Task Class_types_precede_method_types_when_substituting_constraints()
    {
        var pair = GenericClassInferencePolicy.Pair(new[] { "class1", "class2" }, new[] { "method" });
        await Assert.That(pair.SequenceEqual(new[] { "class1", "class2", "method" })).IsTrue();
    }

}

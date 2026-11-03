using TUnit.Inference;

namespace TUnit.SourceGenerator.IncrementalTests;

public class GenericMethodInferencePolicyTests
{
    [Test]
    public async Task Source_precedence_and_flags_are_explicit()
    {
        var actual = GenericMethodInferencePolicy.Rules
            .Select(rule => (rule.Source, rule.ScopesRows, rule.FallsBackToExplicitTypes))
            .ToArray();
        var expected = new[]
        {
            (GenericMethodInferencePolicy.Source.Arguments, true, false),
            (GenericMethodInferencePolicy.Source.TypedDataSource, true, true),
            (GenericMethodInferencePolicy.Source.ParameterAttribute, false, false),
            (GenericMethodInferencePolicy.Source.MethodDataSource, true, true),
            (GenericMethodInferencePolicy.Source.GenerateGenericTest, false, false),
        };
        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }
}

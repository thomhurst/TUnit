using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class CollectionShapeAssertionGeneratorTests : TestsBase<CollectionShapeAssertionGenerator>
{
    [Test]
    public Task Emits_Full_Per_Shape_Assertion_Surface() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "CollectionShapeAssertionSource.cs"),
        async generatedFiles =>
        {
            // Only Emitter A fires (no Satisfies / CountSpecialised triggers in the input).
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var file = generatedFiles[0];

            // Shared pre-work-preserving upcast helper is emitted once.
            await Assert.That(file).Contains("AssertionContext<TTo> Upcast<TFrom, TTo>");
            await Assert.That(file).Contains("context.PendingPreWork is { } preWork");

            // Per-shape signature fidelity reflected from the real symbols: ReadOnlyList.HasItemAt
            // carries an IEqualityComparer the IList overload does not.
            await Assert.That(file).Contains(
                "ReadOnlyListAssertion<TItem>(source.Context).HasItemAt(index, expected, comparer, indexExpression, expectedExpression)");
            await Assert.That(file).Contains(
                "ListAssertion<TItem>(source.Context).HasItemAt(index, expected, indexExpression, expectedExpression)");

            // Set shapes seed via the internal FromContext factory.
            await Assert.That(file).Contains("SetAssertion<TItem>.FromContext(source.Context)");

            // Concrete shapes upcast their context (List<T> -> IList<T>) so the wrapper's pre-work survives.
            await Assert.That(file).Contains(
                "Upcast<global::System.Collections.Generic.List<TItem>, global::System.Collections.Generic.IList<TItem>>(source.Context, x => x)");

            // Dictionary value shapes preserve dictionary-specific methods and the notnull key constraint.
            await Assert.That(file).Contains("DictionaryAssertion<TKey, TValue>(source.Context).ContainsKey");
            await Assert.That(file).Contains("where TKey : notnull");
        });
}

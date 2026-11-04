namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7224 : TestsBase
{
    [Test]
    public Task Method_sources_infer_class_types() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7224", "MethodSourceClassInferenceTests.cs"),
        async files =>
        {
            var generated = string.Join(Environment.NewLine, files);
            await Assert.That(generated).Contains("MethodSourceClassInferenceTests<int>");
            await Assert.That(generated).Contains("NullableMethodSourceClassInferenceTests<int>");
            await Assert.That(generated).Contains("instance.NullableValue(");
            await Assert.That(generated).Contains("instance.WrappedDelegateArray<global::System.Func<int>>(");
            await Assert.That(generated).Contains("instance.WrappedDelegateArrayWithFirst<global::System.Func<int>>(");
            await Assert.That(generated).Contains("instance.ExternalWrappedArray<global::System.Uri>(");
            foreach (var method in new[] { "NestedWrappedTuple", "Single", "Tuple", "Array", "Field", "TaskSource", "ValueTaskSource", "AsyncSource", "Overloaded", "External", "WrappedArray", "WrappedArrayPair", "WrappedArrayLaterParameter", "WrappedArrayOptional", "WrappedArraySuppliedOptional", "WrappedArrayEmptyParams", "WrappedArrayParams", "WrappedArrayLaterParams", "OptionalFillsMissingType", "ReferenceTuple", "Interface", "BaseType", "MixedSources" })
            {
                await Assert.That(generated).Contains($"instance.{method}(");
            }
        });

    [Test]
    public Task Uninferable_sources_report_failure() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7224", "UninferableMethodSourceClassTests.cs"),
        async files =>
        {
            var generated = string.Join(Environment.NewLine, files);
            await Assert.That(generated).Contains("Cannot infer generic class type arguments from method data source 'Rows'");
            await Assert.That(generated).Contains("InvalidClassType");
            await Assert.That(generated).Contains("CannotInferReversed");
            await Assert.That(generated).Contains("CannotInferUnusedType");
            await Assert.That(generated).DoesNotContain("AmbiguousMethodSourceClassTests<int>");
            await Assert.That(generated).DoesNotContain("AmbiguousMethodSourceClassTests<string>");
            await Assert.That(generated).Contains("ExplicitMethodSourceClassTests<int>");
            await Assert.That(generated).Contains("instance.ExplicitTypes(");
        });
}

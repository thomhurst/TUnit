using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InstanceMethodTypeInferenceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7222", "GenericInstanceSourceTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = [Path.Combine(Git.TestsDirectory.FullName,
                "TUnit.TestProject", "Bugs", "_7222", "InstanceSourceConstraintViolationTests.cs")]
        },
        async files =>
        {
            var source = string.Join("\n", files);
            await Assert.That(source).Contains("Could not infer the type arguments of generic test method 'NullableElement'");
            await Assert.That(string.Join("\n", files.Where(file =>
                !file.Contains("ClosedClassUninferredInstanceSourceTests")
                && !file.Contains("ClosedClassRowUninferredInstanceSourceTests"))))
                .DoesNotContain("Could not infer the type arguments");
            await Assert.That(source).Contains("Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'");
            await Assert.That(source).Contains("GenericInstanceSourceTests<int>");
            await Assert.That(source).Contains("NonGenericInstanceSourceTests");
            await Assert.That(source).DoesNotContain("WholeRow<int>");
            await Assert.That(source).Contains("WholeRow<global::System.Tuple<int, string>>");
            await Assert.That(source).Contains("BothSources<string>");
            await Assert.That(source).Contains("Optional<int>");
            await Assert.That(source).Contains("ValueOptional<int>");
            await Assert.That(source).Contains("ValueParams<int>");
            await Assert.That(source).Contains("ReferenceOptional<int>");
            await Assert.That(source).Contains("ReferenceParams<int>");
            await Assert.That(source).Contains("Params<int>");
            await Assert.That(source).Contains("InstanceElement<int>");
            await Assert.That(source).Contains("StaticElement<int>");
            await Assert.That(source).DoesNotContain("InstanceElement<global::System.Tuple<int>>");
            await Assert.That(source).Contains("ClosedNested<int>");
            await Assert.That(source).Contains("RepeatedValue<string>");
            await Assert.That(source).Contains("RepeatedReference<int>");
            await Assert.That(source).Contains("RepeatedWhole<int>");
            await Assert.That(source).Contains("ValueAssignable<int>");
            await Assert.That(source).Contains("ReferenceAssignable<int>");
            await Assert.That(source).Contains("CrossFamily<int>");
            await Assert.That(source).Contains("NullableElement<int>");
            await Assert.That(source).Contains("NumericElement<int>");
            await Assert.That(source).Contains("EnumElement<int>");
            await Assert.That(source).Contains("EnumReferenceElement<int>");
            await Assert.That(source).Contains("StringElement<int>");
            await Assert.That(source).Contains("StringReferenceElement<int>");
            await Assert.That(source).Contains("OperatorElement<int>");
            await Assert.That(source).Contains("OperatorReferenceElement<int>");
            await Assert.That(source).Contains("NarrowNumericElement<int>");
            await Assert.That(source).Contains("NarrowNumericReferenceElement<int>");
            await Assert.That(source).Contains("OpenNullableElement<int>");
            await Assert.That(source).Contains("OpenNullableReferenceElement<int>");
            await Assert.That(source).Contains("BaseElement<int>");
            await Assert.That(source).Contains("BaseReferenceElement<int>");
            await Assert.That(source).Contains("InterfaceElement<int>");
            await Assert.That(source).Contains("InterfaceReferenceElement<int>");
            await Assert.That(source).Contains("NullableClass<int>");
            await Assert.That(source).Contains("NullableReferenceClass<int>");
            await Assert.That(source).Contains("ClosedClass<string>");
            await Assert.That(source).Contains("Valid<int>");
            await Assert.That(source).Contains("Valid<string>");
        });
}

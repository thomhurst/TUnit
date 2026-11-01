using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// A method-level [GenerateGenericTest] with no type arguments, or the wrong number of them, is emitted as a
/// failing entry with reflection mode's message instead of being dropped (#7222).
/// </summary>
internal class Tests7222 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
            Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7222",
            "GenerateGenericTestArityTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                // A pointer is not a valid type argument: M<int*>() would not compile
                """
                namespace TUnit.TestProject.Bugs._7222;

                public unsafe class PointerGenerateGenericTestTests
                {
                    [Test]
                    [GenerateGenericTest(typeof(int*))]
                    public void PointerTypeArgument<T>()
                    {
                    }
                }
                """
            ]
        },
        async generatedFiles =>
        {
            const string violation = "global::TUnit.Core.GenericConstraintViolationDataSource.Create(";

            string SourceFor(string className, string methodName) => generatedFiles.Single(f =>
                f.Contains($"class TUnit_TestProject_Bugs__7222_{className}")
                && (f.Contains($"methodName: \"{methodName}<") || f.Contains($"methodName: \"{methodName}\"")));

            await Assert.That(SourceFor("GenerateGenericTestArityTests", "TooManyTypeArguments"))
                .Contains(violation + "\"[GenerateGenericTest] on method 'TooManyTypeArguments' provides 2 type argument(s) but method requires 1. Provided: [Int32, String]\")");

            await Assert.That(SourceFor("GenerateGenericTestArityTests", "NoTypeArguments"))
                .Contains(violation + "\"[GenerateGenericTest] on method 'NoTypeArguments' has no type arguments\")");

            await Assert.That(SourceFor("GenerateGenericTestArityTests", "TooFewTypeArguments"))
                .Contains(violation + "\"[GenerateGenericTest] on method 'TooFewTypeArguments' provides 1 type argument(s) but method requires 2. Provided: [Int32]\")");

            // The valid ones still run beside the failing one.
            var mixed = SourceFor("GenerateGenericTestArityTests", "MixedArity");
            await Assert.That(mixed).Contains("instance.MixedArity<int>(");
            await Assert.That(mixed).Contains("instance.MixedArity<string>(");
            await Assert.That(mixed).Contains(violation + "\"[GenerateGenericTest] on method 'MixedArity' provides 2 type argument(s)");

            // An array type argument is a type argument like any other.
            var array = SourceFor("GenerateGenericTestArityTests", "ArrayTypeArgument");
            await Assert.That(array).DoesNotContain(violation);
            await Assert.That(array).Contains("instance.ArrayTypeArgument<int[]>(");

            // A generic method of a generic class fails the wrongly sized one on each concrete class.
            var genericClass = SourceFor("GenericClassGenerateGenericTestArityTests", "MixedArity");
            await Assert.That(genericClass.Split([violation + "\"[GenerateGenericTest] on method 'MixedArity'"], StringSplitOptions.None).Length - 1)
                .IsEqualTo(2);

            // With no valid method-level [GenerateGenericTest], the concrete class still gets the failure.
            await Assert.That(SourceFor("OnlyWronglySizedOnGenericClassTests", "NoTypeArguments"))
                .Contains(violation + "\"[GenerateGenericTest] on method 'NoTypeArguments' has no type arguments\")");

            // Declaration order: the arity failure first, then the constraint failure, as in reflection mode.
            var order = SourceFor("ArityAndConstraintOrderTests", "ArityThenConstraint");
            await Assert.That(order.IndexOf(violation + "\"[GenerateGenericTest] on method 'ArityThenConstraint' provides 2", StringComparison.Ordinal))
                .IsLessThan(order.IndexOf(violation + "\"Type 'Int32' does not satisfy", StringComparison.Ordinal));

            // The failing entry has no class rows, so it fails once rather than once per class row.
            var classRows = SourceFor("ClassRowsGenerateGenericTestArityTests", "MixedArity");
            var classRowsViolationEntry = classRows.Substring(classRows.IndexOf(violation, StringComparison.Ordinal));
            classRowsViolationEntry = classRowsViolationEntry.Substring(0, classRowsViolationEntry.IndexOf("TestEntryFactory", StringComparison.Ordinal) is var end and > 0 ? end : classRowsViolationEntry.Length);
            await Assert.That(classRowsViolationEntry).DoesNotContain("classDataSources:");

            // A non-generic class passes its constructor argument rather than calling a parameterless constructor.
            await Assert.That(SourceFor("ConstructorArgumentGenerateGenericTestArityTests", "MixedArity"))
                .DoesNotContain("new global::TUnit.TestProject.Bugs._7222.ConstructorArgumentGenerateGenericTestArityTests()");

            var pointer = SourceFor("PointerGenerateGenericTestTests", "PointerTypeArgument");
            await Assert.That(pointer).DoesNotContain("PointerTypeArgument<int*>");
            await Assert.That(pointer)
                .Contains(violation + "\"[GenerateGenericTest] on method 'PointerTypeArgument' provides type argument 'Int32*', which cannot be a generic type argument\")");
        });
}

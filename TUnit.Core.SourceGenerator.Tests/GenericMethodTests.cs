using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class GenericMethodTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "GenericMethodTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await AssertFileContains(generatedFiles[0], "var methodData in global::TUnit.TestProject.GenericMethodTests.AggregateBy_Numeric_TestData()");
            
            await AssertFileContains(generatedFiles[0], "var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Collections.Generic.IEnumerable<global::System.Int32>, global::System.Func<global::System.Int32, global::System.Int32>, global::System.Func<global::System.Int32, global::System.Int32>, global::System.Func<global::System.Int32, global::System.Int32, global::System.Int32>, global::System.Collections.Generic.IEqualityComparer<global::System.Int32>, global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<global::System.Int32, global::System.Int32>>>(methodData);");
            await AssertFileContains(generatedFiles[0], "global::System.Collections.Generic.IEnumerable<global::System.Int32> methodArg = methodArgTuples.Item1;");
            await AssertFileContains(generatedFiles[0], "global::System.Func<global::System.Int32, global::System.Int32> methodArg1 = methodArgTuples.Item2;");
            await AssertFileContains(generatedFiles[0], "global::System.Func<global::System.Int32, global::System.Int32> methodArg2 = methodArgTuples.Item3;");
            await AssertFileContains(generatedFiles[0], "global::System.Func<global::System.Int32, global::System.Int32, global::System.Int32> methodArg3 = methodArgTuples.Item4;");
            await AssertFileContains(generatedFiles[0], "global::System.Collections.Generic.IEqualityComparer<global::System.Int32> methodArg4 = methodArgTuples.Item5;");
            await AssertFileContains(generatedFiles[0], "global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<global::System.Int32, global::System.Int32>> methodArg5 = methodArgTuples.Item6;");
            
            await AssertFileContains(generatedFiles[0], "var methodData in global::TUnit.TestProject.GenericMethodTests.AggregateBy_String_TestData()");

            await AssertFileContains(generatedFiles[0], "var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.Func<global::System.String, global::System.String>, global::System.Func<global::System.String, global::System.String>, global::System.Func<global::System.String, global::System.String, global::System.String>, global::System.Collections.Generic.IEqualityComparer<global::System.String>, global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<global::System.String, global::System.String>>>(methodData);");
            await AssertFileContains(generatedFiles[0], "global::System.Collections.Generic.IEnumerable<global::System.String> methodArg = methodArgTuples.Item1;");
            await AssertFileContains(generatedFiles[0], "global::System.Func<global::System.String, global::System.String> methodArg1 = methodArgTuples.Item2;");
            await AssertFileContains(generatedFiles[0], "global::System.Func<global::System.String, global::System.String> methodArg2 = methodArgTuples.Item3;");
            await AssertFileContains(generatedFiles[0], "global::System.Func<global::System.String, global::System.String, global::System.String> methodArg3 = methodArgTuples.Item4;");
            await AssertFileContains(generatedFiles[0], "global::System.Collections.Generic.IEqualityComparer<global::System.String> methodArg4 = methodArgTuples.Item5;");
            await AssertFileContains(generatedFiles[0], "global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<global::System.String, global::System.String>> methodArg5 = methodArgTuples.Item6;");
        });
}
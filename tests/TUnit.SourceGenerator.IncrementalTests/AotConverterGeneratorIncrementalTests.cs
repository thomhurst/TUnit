using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class AotConverterGeneratorIncrementalTests
{
    private const string DefaultConverter =
        """
        using global::TUnit.Core;

        #nullable enabled
        public record Foo
        {
            public static implicit operator Foo((int Value1, int Value2) tuple) => new();
        }

        public class Tests
        {
            [Test]
            [MethodDataSource(nameof(Data))]
            public void Test1(Foo data)
            {
            }

            public static IEnumerable<Foo> Data() => [new()];
        }
        """;

    private const string SecondConverter =
        """
        using global::TUnit.Core;

        #nullable enabled
        public record FooBar
        {
            public static implicit operator FooBar((int Value1, int Value2) tuple) => new();
        }

        public class Tests1
        {
            [Test]
            [MethodDataSource(nameof(Data))]
            public void Test1(FooBar data)
            {
            }

            public static IEnumerable<FooBar> Data() => [new()];
        }
        """;

    [Fact]
    public void AddUnrelatedType_MethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<AotConverterGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New, 1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 1);
    }

    [Fact]
    public void AddNewConverterShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<AotConverterGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New, 1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SecondConverter));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified, 2);
    }

    [Fact]
    public void ModifyOperatorShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<AotConverterGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New, 1);

        var compilation2 = TestHelper.ReplaceTypeDeclaration(compilation1, "Foo",
            """
            public record Foo
            {
                public static explicit operator Foo((int Value1, int Value2) tuple) => new();
            }
            """
            );

        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified, 1);
    }

    private static void AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int conversionMetadataLength,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];
        var runValue = runResult.TrackedSteps[AotConverterGenerator.ParseAotConverter][0].Outputs[0].Value;
        var runState = (ValueTuple<EquatableArray<AotConverterGenerator.ConversionMetadata>, bool>)runValue;
        Xunit.Assert.Equal(conversionMetadataLength, runState.Item1.Length);

        TestHelper.AssertRunReason(runResult, AotConverterGenerator.ParseAotConverter, reasons.BuildStep, outputIndex);
    }
}

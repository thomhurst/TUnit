using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using TUnit.Analyzers.Tests.Verifiers;

namespace TUnit.Analyzers.Tests;

/// <summary>
/// Tests for the AOT compatibility analyzer
/// </summary>
public class AotCompatibilityAnalyzerTests
{
    [Test]
    public async Task GenericTestClass_WithoutExplicitInstantiation_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class {|TUnit0058:GenericTestClass|}<T>
            {
                [Test]
                public void TestMethod()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericTestClass_WithExplicitInstantiation_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            [GenerateGenericTest(typeof(int))]
            [GenerateGenericTest(typeof(string))]
            public class GenericTestClass<T>
            {
                [Test]
                public void TestMethod()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericTestMethod_WithoutExplicitInstantiation_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void {|TUnit0058:GenericTestMethod|}<T>()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericTestMethod_WithExplicitInstantiation_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [GenerateGenericTest(typeof(int))]
                [GenerateGenericTest(typeof(string))]
                [Test]
                public void GenericTestMethod<T>()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }





    [Test]
    public async Task ClassDataSourceAttribute_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [{|TUnit0059:ClassDataSource(typeof(DataProvider))|}, Test]
                public void TestMethod(int value)
                {
                }
            }

            public class DataProvider
            {
                public static IEnumerable<int> GetData() => [1, 2, 3];
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task MethodDataSourceAttribute_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [{|TUnit0059:MethodDataSource(nameof(GetData))|}, Test]
                public void TestMethod(int value)
                {
                }

                public static IEnumerable<int> GetData() => [1, 2, 3];
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ArgumentsAttribute_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Arguments(1, 2, 3), Test]
                public void TestMethod(int value)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task NonGenericTestClass_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class SimpleTestClass
            {
                [Test]
                public void TestMethod()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
using TUnit.Analyzers.Tests.Verifiers;

namespace TUnit.Analyzers.Tests;

public class TestConstructorDataSourceAnalyzerTests
{
    [Test]
    public async Task Unmarked_Constructors_Keep_First_Constructor_Behavior()
    {
        await CSharpAnalyzerVerifier<TestDataAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            [Arguments(1)]
            public class C
            {
                public C(int value) { }
                public C(string value) { }
                [Test] public void Test() { }
            }
            """);
    }

    [Test]
    public async Task Method_Rows_Match_Marked_Generic_Constructor()
    {
        await CSharpAnalyzerVerifier<TestDataAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Collections.Generic;

            [GenerateGenericTest(typeof(int))]
            [MethodDataSource(nameof(Rows))]
            public class C<T> where T : struct
            {
                public C(T value) { }
                [TestConstructor]
                public C(T value, string label) { }
                public static IEnumerable<(T, string)> Rows() => new[] { (default(T), "b") };
                [Test] public void Test() { }
            }
            """);
    }

    [Test]
    public async Task Arguments_Must_Match_Marked_Constructor()
    {
        await CSharpAnalyzerVerifier<TestDataAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            [{|#0:Arguments(1)|}]
            public class C
            {
                public C(int value) { }
                [TestConstructor]
                public C(string value) { }
                [Test] public void Test() { }
            }
            """,
            CSharpAnalyzerVerifier<TestDataAnalyzer>.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(0).WithArguments("int", "string"));
    }

    [Test]
    public async Task Matrix_Uses_Marked_Constructor()
    {
        await CSharpAnalyzerVerifier<MatrixAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            [MatrixDataSource]
            public class C
            {
                public C([Matrix("wrong")] int ignored) { }
                [TestConstructor]
                public C([Matrix(1)] int value, [Matrix("a")] string label) { }
                [Test] public void Test() { }
            }
            """);
    }

    [Test]
    public async Task Matrix_On_Marked_Constructor_Requires_Class_Attribute()
    {
        await CSharpAnalyzerVerifier<MatrixAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            public class {|#0:C|}
            {
                public C() { }
                [TestConstructor]
                public C([Matrix(1)] int value) { }
                [Test] public void Test() { }
            }
            """,
            CSharpAnalyzerVerifier<MatrixAnalyzer>.Diagnostic(Rules.MatrixDataSourceAttributeRequired)
                .WithLocation(0));
    }

    [Test]
    public async Task Combined_Data_Uses_Marked_Constructor()
    {
        await CSharpAnalyzerVerifier<CombinedDataSourceAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            [CombinedDataSources]
            public class C
            {
                public C(int ignored) { }
                [TestConstructor]
                public C([Arguments(1)] int value, [Arguments("a")] string label) { }
                [Test] public void Test() { }
            }
            """);
    }

    [Test]
    public async Task Combined_Data_On_Marked_Constructor_Requires_Class_Attribute()
    {
        await CSharpAnalyzerVerifier<CombinedDataSourceAnalyzer>.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            public class {|#0:C|}
            {
                public C() { }
                [TestConstructor]
                public C([Arguments(1)] int value) { }
                [Test] public void Test() { }
            }
            """,
            CSharpAnalyzerVerifier<CombinedDataSourceAnalyzer>.Diagnostic(Rules.CombinedDataSourceAttributeRequired)
                .WithLocation(0));
    }
}

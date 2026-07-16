using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.Analyzers.MatrixAnalyzer,
    TUnit.Analyzers.CodeFixers.MatrixDataSourceCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class MatrixDataSourceCodeFixProviderTests
{
    [Test]
    public async Task Adds_MatrixDataSource_On_Method()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;

            public class MyClass
            {
                [Test]
                public void {|#0:MyTest|}(
                    [Matrix(1, 2, 3)] int value,
                    [Matrix(true, false)] bool flag)
                {
                }
            }
            """,
            Verifier.Diagnostic(Rules.MatrixDataSourceAttributeRequired).WithLocation(0),
            """
            using TUnit.Core;

            public class MyClass
            {
                [Test]
                [MatrixDataSource]
                public void MyTest(
                    [Matrix(1, 2, 3)] int value,
                    [Matrix(true, false)] bool flag)
                {
                }
            }
            """
        );
    }

    [Test]
    public async Task Adds_MatrixDataSource_On_Class()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;

            public class {|#0:MyClass|}
            {
                public MyClass(
                    [Matrix(1, 2)] int value,
                    [Matrix(true, false)] bool flag)
                {
                }

                [Test]
                public void MyTest()
                {
                }
            }
            """,
            Verifier.Diagnostic(Rules.MatrixDataSourceAttributeRequired).WithLocation(0),
            """
            using TUnit.Core;

            [MatrixDataSource]
            public class MyClass
            {
                public MyClass(
                    [Matrix(1, 2)] int value,
                    [Matrix(true, false)] bool flag)
                {
                }

                [Test]
                public void MyTest()
                {
                }
            }
            """
        );
    }
}

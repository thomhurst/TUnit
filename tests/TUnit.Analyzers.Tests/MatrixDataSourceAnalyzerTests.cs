using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MatrixAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MatrixDataSourceAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [MatrixDataSource]
                public class MyClass
                {
                    public MyClass(
                        [Matrix(1, 2, 3)] int value,
                        [Matrix(true, false)] bool value2
                        )
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

    [Test]
    public async Task Class_Missing_Attribute_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class {|#0:MyClass|}
                {
                    public MyClass(
                        [Matrix(1, 2, 3)] int value,
                        [Matrix(true, false)] bool value2
                        )
                    {
                    }
                            
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.MatrixDataSourceAttributeRequired)
                    .WithLocation(0)
            );
    }


    [Test]
    public async Task Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                
                public class MyClass
                {
                    [MatrixDataSource]
                    [Test]
                    public void MyTest(
                        [Matrix(1, 2, 3)] int value,
                        [Matrix(true, false)] bool value2
                        )
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Missing_Attribute_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                
                public class MyClass
                {
                    [Test]
                    public void {|#0:MyTest|}(
                        [Matrix(1, 2, 3)] int value,
                        [Matrix(true, false)] bool value2
                        )
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.MatrixDataSourceAttributeRequired)
                    .WithLocation(0)
            );
    }
}

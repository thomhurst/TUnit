using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.CombinedDataSourceAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class CombinedDataSourceAnalyzerTests
{
    [Test]
    public async Task Method_With_CombinedDataSource_And_ParameterDataSources_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [CombinedDataSource]
                    [Test]
                    public void MyTest(
                        [Arguments(1, 2, 3)] int value,
                        [Arguments("a", "b")] string text
                        )
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_With_ParameterDataSources_Missing_CombinedDataSource_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void {|#0:MyTest|}(
                        [Arguments(1, 2, 3)] int value,
                        [Arguments("a", "b")] string text
                        )
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CombinedDataSourceAttributeRequired)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Method_With_CombinedDataSource_Missing_ParameterDataSource_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [CombinedDataSource]
                    [Test]
                    public void MyTest(
                        [Arguments(1, 2, 3)] int value,
                        string {|#0:text|}
                        )
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CombinedDataSourceMissingParameterDataSource)
                    .WithLocation(0)
                    .WithArguments("text")
            );
    }

    [Test]
    public async Task Method_With_CombinedDataSource_And_MatrixDataSource_Warning()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [CombinedDataSource]
                    [MatrixDataSource]
                    [Test]
                    public void {|#0:MyTest|}(
                        [Arguments(1, 2, 3)] int value,
                        [Arguments("a", "b")] string text
                        )
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CombinedDataSourceConflictWithMatrix)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Class_With_CombinedDataSource_And_ParameterDataSources_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [CombinedDataSource]
                public class MyClass
                {
                    public MyClass(
                        [Arguments(1, 2, 3)] int value,
                        [Arguments("a", "b")] string text
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
    public async Task Class_With_ParameterDataSources_Missing_CombinedDataSource_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class {|#0:MyClass|}
                {
                    public MyClass(
                        [Arguments(1, 2, 3)] int value,
                        [Arguments("a", "b")] string text
                        )
                    {
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CombinedDataSourceAttributeRequired)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Method_With_CancellationToken_No_DataSource_Required()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading;
                using TUnit.Core;

                public class MyClass
                {
                    [CombinedDataSource]
                    [Test]
                    public void MyTest(
                        [Arguments(1, 2, 3)] int value,
                        CancellationToken cancellationToken
                        )
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_With_MethodDataSource_And_Arguments_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public class MyClass
                {
                    public static IEnumerable<int> GetNumbers() => [1, 2, 3];

                    [CombinedDataSource]
                    [Test]
                    public void MyTest(
                        [MethodDataSource(nameof(GetNumbers))] int number,
                        [Arguments("a", "b")] string text
                        )
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_With_ClassDataSource_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;
                using System;

                public class TestData : DataSourceGeneratorAttribute<int>
                {
                    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
                    {
                        yield return () => 1;
                        yield return () => 2;
                    }
                }

                public class MyClass
                {
                    [CombinedDataSource]
                    [Test]
                    public void MyTest(
                        [ClassDataSource<TestData>] int number,
                        [Arguments("a", "b")] string text
                        )
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Multiple_Parameters_Missing_DataSource_Multiple_Errors()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [CombinedDataSource]
                    [Test]
                    public void MyTest(
                        [Arguments(1, 2, 3)] int value,
                        string {|#0:text|},
                        bool {|#1:flag|}
                        )
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CombinedDataSourceMissingParameterDataSource)
                    .WithLocation(0)
                    .WithArguments("text"),
                Verifier.Diagnostic(Rules.CombinedDataSourceMissingParameterDataSource)
                    .WithLocation(1)
                    .WithArguments("flag")
            );
    }

    [Test]
    public async Task Method_Without_CombinedDataSource_No_ParameterDataSources_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, "a", true)]
                    public void MyTest(int value, string text, bool flag)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Class_With_CombinedDataSource_Missing_ParameterDataSource_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [CombinedDataSource]
                public class MyClass
                {
                    public MyClass(
                        [Arguments(1, 2, 3)] int value,
                        string {|#0:text|}
                        )
                    {
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CombinedDataSourceMissingParameterDataSource)
                    .WithLocation(0)
                    .WithArguments("text")
            );
    }
}

using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestMethodParametersAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class TestMethodParametersAnalyzerTests
{
    [Test]
    [Arguments("ReadOnlySpan")]
    [Arguments("Span")]
    public async Task Span_Params_Reports_Unsupported_Parameter(string spanType)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System;
            using System.Collections.Generic;
            using TUnit.Core;

            public class MyClass
            {
                [Test, Rows]
                public void MyTest<T>(T[] values, {|#1:params|} {{spanType}}<string> {|#0:rest|}) { }
            }

            public class RowsAttribute : TypedDataSourceAttribute<(int[], string)>
            {
                public override async IAsyncEnumerable<Func<System.Threading.Tasks.Task<(int[], string)>>> GetTypedDataRowsAsync(DataGeneratorMetadata metadata)
                {
                    yield return () => System.Threading.Tasks.Task.FromResult((new[] { 1 }, "hello"));
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """,
            // The verifier uses Roslyn 4.8, before C# 13 params collections.
            Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS0225").WithLocation(1),
            Verifier.Diagnostic(Rules.UnsupportedSpanParams).WithLocation(0).WithArguments("rest"));
    }

    [Test]
    public async Task Array_And_Interface_Params_And_NonTest_Spans_Are_Allowed()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;

            public class MyClass
            {
                [Test, Arguments("hello")]
                public void ArrayTest(params string[] rest) { }

                [Test, Arguments("hello")]
                public void InterfaceTest({|#0:params|} IEnumerable<string> rest) { }

                public void Helper({|#1:params|} ReadOnlySpan<string> rest) { }
            }
            """,
            Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS0225").WithLocation(0),
            Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS0225").WithLocation(1));
    }

    [Test]
    public async Task Test_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Test_Missing_Parameter_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest(int {|#0:value|})
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.NoDataSourceProvided).WithLocation(0)
            );
    }

    [Test]
    public async Task Abstract_Test_Missing_Parameter__NoError()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public abstract class MyClass
                {
                    [Test]
                    public abstract void MyTest(int value);
                }
                """
            );
    }

    [Test]
    public async Task DataSourceGenerator()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                public class DataSourceGeneratorTests
                {
                    [Test]
                    [AutoFixtureGenerator<int>]
                    public void GeneratedData_Method(int value)
                    {
                        // Dummy method
                    }

                    [Test]
                    [AutoFixtureGenerator<int, string, bool>]
                    public void GeneratedData_Method2(int value, string value2, bool value3)
                    {
                        // Dummy method
                    }

                    public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
                    {
                        protected override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
                        {
                            return [() => default!];
                        }
                    }

                    public class AutoFixtureGeneratorAttribute<T1, T2, T3> : DataSourceGeneratorAttribute<T1, T2, T3>
                    {
                        protected override IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
                        {
                            return [() => default!];
                        }
                    }
                }
                """
            );
    }
}

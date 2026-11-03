using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ArgumentsAnalyzerTests
{
    [Test]
    public async Task Typed_Array_Row_Infers_Method_Element_Type()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            public class MyClass
            {
                [Test]
                [Arguments<int[]>(new[] { 1, 2 })]
                public void Run<T>(T[] values) { }
            }
            """);
    }

    [Test]
    public async Task Typed_Array_Row_Allows_Omitted_Generic_Optional_Parameter()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;

            public class MyClass
            {
                [Test]
                [Arguments<int[]>(new[] { 1, 2 })]
                public void Run<T>(T[] values, T fallback = default!) { }
            }
            """);
    }

    [Test]
    public async Task Typed_Array_Source_Allows_Omitted_Optional_Parameter()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;

            public class ArrayRowAttribute : TypedDataSourceAttribute<(int[], int)>
            {
                public override async IAsyncEnumerable<Func<System.Threading.Tasks.Task<(int[], int)>>> GetTypedDataRowsAsync(DataGeneratorMetadata metadata)
                {
                    await System.Threading.Tasks.Task.Yield();
                    yield return () => System.Threading.Tasks.Task.FromResult((new[] { 1 }, 2));
                }
            }

            public class MyClass
            {
                [Test]
                [ArrayRow]
                public void Run<T>(T[] values, T value, int count = 0) { }
            }
            """);
    }

    [Test]
    public async Task Typed_Array_Source_Preserves_Declared_Type_Constraint_Diagnostic()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;

            public class ObjectRowAttribute : DataSourceGeneratorAttribute<object, object[]>
            {
                protected override IEnumerable<Func<(object, object[])>> GenerateDataSources(DataGeneratorMetadata metadata)
                {
                    yield return () => (new object(), new object[0]);
                }
            }

            public class MyClass
            {
                [Test]
                [{|#0:ObjectRow|}]
                public void Run<T>(T value, T[] values) where T : IDisposable { }
            }
            """,
            Verifier.Diagnostic(Rules.GenericTypeArgumentInferredFromDeclaredType)
                .WithLocation(0).WithArguments("T", "object"));
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
                    [Arguments(0, 1L)] // this is ok
                    [Arguments(0, 1)] // Error TUnit0001 : Attribute argument types 'int' don't match method parameter types 'long[]'
                    public void Test(int a, params long[] arr)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_DisplayName_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("preferLocal", "AutoResolvePreferLocal", DisplayName = "Auto-resolve as prefer local (branch)")]
                    [Arguments("preferRemote", "AutoResolvePreferRemote", DisplayName = "Auto-resolve as prefer remote (org)")]
                    public void MergeAutoResolveIsCorrect(string conflictsArg, string expectedMetadataDirectoryName)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_Skip_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("value1", "value2", Skip = "Skipped for testing")]
                    public void MyTest(string arg1, string arg2)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_Categories_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("value1", "value2", Categories = new[] { "smoke", "integration" })]
                    public void MyTest(string arg1, string arg2)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_Multiple_Named_Properties_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("value1", "value2", DisplayName = "Custom name", Categories = new[] { "smoke" })]
                    public void MyTest(string arg1, string arg2)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Arguments_With_DisplayName_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments<string>("value", DisplayName = "Test with string")]
                    public void MyTest(string arg)
                    {
                    }
                }
                """
            );
    }

    [Test]
    [Arguments("[Arguments<string[]>(new[] { \"x\" })]", "T[] values")]
    [Arguments("[Arguments<string[]>(null!)]", "T[]? values")]
    public async Task Generic_Class_Row_Matching_Constructor_After_Inferring_T_No_Error(string row, string parameter)
    {
        // #7223: the row infers T through the constructor parameter (T[] gives its element type) as both execution modes
        // infer it, so its declared type matches.
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;

                {{row}}
                public class MyClass<T>({{parameter}})
                {
                    [Test]
                    [Arguments("a")]
                    public void MyTest(T arg)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Class_Row_Not_Matching_Constructor_Shape_Error()
    {
        // #7223: an int row cannot bind to T[] for any T
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [{|#0:Arguments<int>(1)|}]
                public class MyClass<T>(T[] values)
                {
                    [Test]
                    [Arguments("a")]
                    public void MyTest(T arg)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "T[]")
            );
    }

    [Test]
    [Arguments("Outer<int>.Inner<string>", "Outer<int>.Inner<string>")]
    [Arguments("Outer<int>.Inner<string>", "Outer<T>.Inner<string>")]
    public async Task Generic_Class_Row_With_Nested_Generic_Parameter_No_Error(string declared, string parameter)
    {
        // A nested type keeps its containing type's arguments when the class's inferred type arguments are substituted
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class Outer<TOuter>
                {
                    public class Inner<TInner>;
                }

                public class NestedRowAttribute : DataSourceGeneratorAttribute<int, {{declared}}>
                {
                    protected override IEnumerable<Func<(int, {{declared}})>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
                    {
                        yield return () => (1, new {{declared}}());
                    }
                }

                [NestedRow]
                public class MyClass<T>(T value, {{parameter}} other)
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }
}

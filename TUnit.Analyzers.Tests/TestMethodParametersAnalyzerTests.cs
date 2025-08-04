using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestMethodParametersAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class TestMethodParametersAnalyzerTests
{
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
                    public void {|#0:MyTest|}(int value)
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
    public async Task Arguments_Attribute_Should_Not_Trigger_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments(-1)]
                    [Arguments(6)]
                    public void GivenInvalidRating_WhenCreatingFeedback_ThenShouldThrowDomainException(int invalidRating)
                    {
                        // Test logic here
                    }
                }
                """
            );
    }

    [Test]
    public async Task MethodDataSource_Attribute_Should_Not_Trigger_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public class MyClass
                {
                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void TestMethod(int value)
                    {
                        // Test logic here
                    }

                    public static IEnumerable<int> GetValues()
                    {
                        yield return 1;
                        yield return 2;
                    }
                }
                """
            );
    }

    [Test]
    public async Task ClassDataSource_Attribute_Should_Not_Trigger_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public class MyClass
                {
                    [Test]
                    [ClassDataSource<MyDataSource>]
                    public void TestMethod(int value)
                    {
                        // Test logic here
                    }
                }

                public class MyDataSource : IEnumerable<object[]>
                {
                    public IEnumerator<object[]> GetEnumerator()
                    {
                        yield return new object[] { 1 };
                        yield return new object[] { 2 };
                    }

                    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                }
                """
            );
    }

    [Test]
    public async Task Multiple_Arguments_Attributes_Should_Not_Trigger_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, "test")]
                    [Arguments(2, "test2")]
                    [Arguments(3, "test3")]
                    public void TestMethodWithMultipleArgs(int number, string text)
                    {
                        // Test logic here
                    }
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

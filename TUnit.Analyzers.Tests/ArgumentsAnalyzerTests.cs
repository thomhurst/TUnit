using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ArgumentsAnalyzerTests
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
                        public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
                        {
                            return [() => default!];
                        }
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T1, T2, T3> : DataSourceGeneratorAttribute<T1, T2, T3>
                    {
                        public override IEnumerable<Func<(T1, T2, T3)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
                        {
                            return [() => default!];
                        }
                    }
                }
                """
            );
    }
}
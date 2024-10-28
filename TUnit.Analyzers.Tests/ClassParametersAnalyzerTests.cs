using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ClassParametersAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ClassParametersAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
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
    public async Task Class_Missing_Parameter_Error()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using TUnit.Core;
                            
                public class {|#0:MyClass|}
                {
                    public MyClass(int value)
                    {
                    }
                            
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.NoDataSourceProvided)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task Abstract_Class_Missing_Parameter__NoError()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using TUnit.Core;

                public abstract class MyClass
                {
                    public MyClass(int value)
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
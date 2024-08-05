using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ClassParametersAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ClassParametersAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void MyTest()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Class_Missing_Parameter_Error()
    {
        const string text = """
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
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoDataSourceProvided).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}
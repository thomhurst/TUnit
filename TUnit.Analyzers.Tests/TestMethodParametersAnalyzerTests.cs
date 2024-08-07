using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestMethodParametersAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class TestMethodParametersAnalyzerTests
{
    [Test]
    public async Task Test_No_Error()
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
    public async Task Test_Missing_Parameter_Error()
    {
        const string text = """
                            using TUnit.Core;
                            
                            public class MyClass
                            {
                                [Test]
                                public void {|#0:MyTest|}(int value)
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoDataSourceProvided).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Abstract_Test_Missing_Parameter__NoError()
    {
        const string text = """
                            using TUnit.Core;

                            public abstract class MyClass
                            {
                                [Test]
                                public abstract void MyTest(int value);
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}
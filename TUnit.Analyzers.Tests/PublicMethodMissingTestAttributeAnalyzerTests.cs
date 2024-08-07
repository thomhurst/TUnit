using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.PublicMethodMissingTestAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class PublicMethodMissingTestAttributeAnalyzerTests
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
                                    Helper();
                                }
                                
                                private void Helper()
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
                            
                            public class MyClass
                            {
                                [Test]
                                public void MyTest()
                                {
                                    Helper();
                                }
                                
                                public void {|#0:Helper|}()
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.PublicMethodMissingTestAttribute).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Before_Hook_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void MyTest()
                                {
                                }
                                
                                [BeforeEachTest]
                                public void SetUp()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task After_Hook_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void MyTest()
                                {
                                }
                                
                                [AfterEachTest]
                                public void SetUp()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}
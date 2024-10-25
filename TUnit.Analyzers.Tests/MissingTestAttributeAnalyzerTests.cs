using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MissingTestAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MissingTestAttributeAnalyzerTests
{
    [Test]
    public async Task Not_Flagged_When_Test_Attribute()
    {
        const string text = """
                            using TUnit.Core;

                            public class BaseClass
                            {
                                [Test]
                                [Arguments(1)]
                                public void Test(int value)
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Flagged_Error_For_Arguments()
    {
        const string text = """
                            using TUnit.Core;
                            
                            public class BaseClass
                            {
                                [Arguments(1)]
                                public void {|#0:Test|}(int value)
                                {
                                }
                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.MissingTestAttribute).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Flagged_Error_For_MethodDataSource()
    {
        const string text = """
                            using TUnit.Core;

                            public class BaseClass
                            {
                                [MethodDataSource(nameof(Method))]
                                public void {|#0:Test|}(int value)
                                {
                                }
                                
                                public static int Method() => 1;
                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.MissingTestAttribute).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Flagged_Error_For_ClassDataSource()
    {
        const string text = """
                            using TUnit.Core;

                            public class BaseClass
                            {
                                [ClassDataSource<MyClass>]
                                public void {|#0:Test|}(MyClass value)
                                {
                                }
                            }
                            
                            public class MyClass;
                            """;

        var expected = Verifier.Diagnostic(Rules.MissingTestAttribute).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}
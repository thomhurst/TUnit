using System.Threading.Tasks;
using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DataDrivenTestArgumentsAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DataDrivenTestArgumentsAnalyzerTests
{
    [Test]
    public async Task DataDriven_Argument_Is_Flagged_When_Does_Not_Match_Parameter_Type()
    {
        const string text = """
                            using TUnit.Assertions;
                            using TUnit.Core;
                            
                            public class MyClass
                            {

                                [{|#0:DataDrivenTest(1)|}]
                                public void MyTest(string value)
                                {
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic().WithLocation(0)
            .WithArguments("int", "string");
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task DataDriven_Argument_Is_Not_Flagged_When_Matches_Parameter_Type()
    {
        const string text = """
                            public class MyClass
                            {
                            
                                [DataDrivenTest(1)]
                                public void MyTest(int value)
                                {
                                }
                            
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}
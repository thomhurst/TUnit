using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DataDrivenTestArgumentsAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DataDrivenTestArgumentsAnalyzerTests
{
    [Test]
    public async Task DataDriven_Argument_Is_Flagged_When_No_Parameters_Passed()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [{|#0:Arguments|}]
                                public void MyTest(string value)
                                {
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.NoTestDataProvided).WithLocation(0)
            .WithArguments("int", "string");

        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task DataDriven_Argument_Is_Flagged_When_Does_Not_Match_Parameter_Type()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [{|#0:Arguments(1)|}]
                                public void MyTest(string value)
                                {
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
            .WithArguments("int", "string");
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task DataDriven_Argument_Is_Not_Flagged_When_Matches_Parameter_Type()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [Arguments(1)]
                                public void MyTest(int value)
                                {
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Optional_Argument_Is_Not_Flagged()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [Arguments(1)]
                                public void MyTest(int value, bool flag = true)
                                {
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}
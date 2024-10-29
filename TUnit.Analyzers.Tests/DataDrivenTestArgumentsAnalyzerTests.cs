using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DataDrivenTestArgumentsAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DataDrivenTestArgumentsAnalyzerTests
{
    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_No_Parameters_Passed()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using TUnit.Core;

                public class MyClass
                {
                            
                    [Test]
                    [{|#0:Arguments|}]
                    public void MyTest(string value)
                    {
                    }

                }
                """,
                
                Verifier.Diagnostic(Rules.NoTestDataProvided)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Does_Not_Match_Parameter_Type()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using TUnit.Core;

                public class MyClass
                {
                            
                    [Test]
                    [{|#0:Arguments(1)|}]
                    public void MyTest(string value)
                    {
                    }

                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Matches_Parameter_Type()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using TUnit.Core;

                public class MyClass
                {
                            
                    [Test]
                    [Arguments(1)]
                    public void MyTest(int value)
                    {
                    }

                }
                """
			);
    }
    
    [Test]
    public async Task Optional_Argument_Is_Not_Flagged()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using TUnit.Core;

                public class MyClass
                {
                            
                    [Test]
                    [Arguments(1)]
                    public void MyTest(int value, bool flag = true)
                    {
                    }

                }
                """
			);
    }
}
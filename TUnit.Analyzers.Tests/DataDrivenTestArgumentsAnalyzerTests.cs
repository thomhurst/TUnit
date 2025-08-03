using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

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

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("<null>", "string")
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

    [Test]
    public async Task Argument_Not_Flagged_When_Matching_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                            
                    [Test]
                    [Arguments("Hello")]
                    public void MyTest(string value)
                    {
                    }

                }
                """
            );
    }

    [Test]
    public async Task Argument_Not_Flagged_When_Matching_Type_Enum()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                            
                    [Test]
                    [Arguments(MyEnum.Value1)]
                    public void MyTest(MyEnum value)
                    {
                    }

                }
                
                public enum MyEnum
                {
                    Value1,
                    Value2
                }
                """
            );
    }

    [Test]
    public async Task Argument_Not_Flagged_When_Matching_ExternalType_Package_Enum()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using Polly.CircuitBreaker;

                public class MyClass
                {
                            
                    [Test]
                    [Arguments(CircuitState.Closed)]
                    public void MyTest(CircuitState value)
                    {
                    }

                }
                """
            );
    }

    [Test]
    public async Task Argument_Not_Flagged_When_Matching_ExternalType_Project_Enum()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using TUnit.TestProject.Library;

                public class MyClass
                {
                    [Test]
                    [Arguments(ProjectReferenceEnum.Value1)]
                    public void MyTest(ProjectReferenceEnum value)
                    {
                    }

                }
                """
            );
    }

    [Test]
    public async Task Convertible_Type_Does_Not_Flag()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments(-123)]
                    public void Int_To_Short(short value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Error_When_Too_Many_Arguments()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [{|#0:Arguments(null, "")|}]
                    public void Create_Unit_Test(string? something)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.TooManyArguments)
                    .WithLocation(0)
            );
    }
}

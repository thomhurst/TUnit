using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MissingTestAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MissingTestAttributeAnalyzerTests
{
    [Test]
    public async Task Not_Flagged_When_Test_Attribute()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class BaseClass
                {
                    [Test]
                    [Arguments(1)]
                    public void Test(int value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Flagged_Error_For_Arguments()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                            
                public class BaseClass
                {
                    [Arguments(1)]
                    public void {|#0:Test|}(int value)
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.MissingTestAttribute)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Flagged_Error_For_MethodDataSource()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class BaseClass
                {
                    [MethodDataSource(nameof(Method))]
                    public void {|#0:Test|}(int value)
                    {
                    }
                                
                    public static int Method() => 1;
                }
                """,

                Verifier.Diagnostic(Rules.MissingTestAttribute)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Flagged_Error_For_ClassDataSource()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class BaseClass
                {
                    [ClassDataSource<MyClass>]
                    public void {|#0:Test|}(MyClass value)
                    {
                    }
                }
                            
                public class MyClass;
                """,

                Verifier.Diagnostic(Rules.MissingTestAttribute)
                    .WithLocation(0)
            );
    }
}
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ConsoleOutAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ConsoleOutAnalyzerTests
{
    [Test]
    public async Task No_Error()
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
    [Arguments("SetOut")]
    [Arguments("SetError")]
    public async Task Static_Error(string method)
    {

        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public static void MyTest()
                    {
                        {|#0:System.Console.{{method}}(new System.IO.StringWriter())|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.OverwriteConsole).WithLocation(0)
            );
    }
}

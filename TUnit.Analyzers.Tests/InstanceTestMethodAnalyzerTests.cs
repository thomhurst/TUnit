using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.InstanceTestMethodAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class InstanceTestMethodAnalyzerTests
{
    [Test]
    public async Task Instance_No_Error()
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
    public async Task Static_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public static void {|#0:MyTest|}()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.InstanceTestMethod).WithLocation(0)
            );
    }
}

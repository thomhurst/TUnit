using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.InheritsTestsAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class InheritsTestsAnalyzerTests
{
    [Test]
    public async Task No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [InheritsTests]
                public class Tests : BaseClass
                {
                }
                            
                public class BaseClass
                {
                    [Test]
                    public void Test()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error2()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class Tests
                {
                }
                """
            );
    }

    [Test]
    public async Task Warning_Test()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class {|#0:Tests|} : BaseClass
                {
                }

                public class BaseClass
                {
                    [Test]
                    public void Test()
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.DoesNotInheritTestsWarning)
                    .WithLocation(0)
            );
    }
}
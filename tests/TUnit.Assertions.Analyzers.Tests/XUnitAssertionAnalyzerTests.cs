using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.XUnitAssertionAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class XUnitAssertionAnalyzerTests
{
    [Test]
    public async Task Xunit_Assert_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Equal(1, 1)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Xunit_Assert_Is_Flagged_When_Only_Referenced_Via_Extern_Alias()
    {
        await Verifier
            .VerifyAnalyzerWithAliasedReferencesAsync(
                """
                extern alias XunitAssert;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:XunitAssert::Xunit.Assert.Equal(1, 1)|};
                    }
                }
                """,
                "XunitAssert",
                ["xunit.v3.assert.dll"],

                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0)
            );
    }
}
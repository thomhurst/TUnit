using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        TUnit.Analyzers.AwaitAssertionAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class AwaitAssertionAnalyzerTests
{
    [Fact]
    public async Task ClassWithMyCompanyTitle_AlertDiagnostic()
    {
        const string text = """
                            public class MyClass
                            {

                                public async Task MyTest()
                                {
                                    var one = 1;
                                    {|#0:Assert.That(one).Is.EqualTo(1)|};
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic().WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}
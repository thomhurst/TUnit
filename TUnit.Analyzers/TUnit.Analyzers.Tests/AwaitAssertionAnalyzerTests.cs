using System.Threading.Tasks;
using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.AwaitAssertionAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class AwaitAssertionAnalyzerTests
{
    [Test]
    public async Task ClassWithMyCompanyTitle_AlertDiagnostic()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Core;
                            
                            public class MyClass
                            {

                                public async Task MyTest()
                                {
                                    var one = 1;
                                    {|#0:Assert.That(one).Is.EqualTo(1);|}
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic().WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}
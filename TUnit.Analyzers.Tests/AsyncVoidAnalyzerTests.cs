using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.AsyncVoidAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class AsyncVoidAnalyzerTests
{
    [Test]
    public async Task Async_Void_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;
                            
                            public class MyClass
                            {
                                [Test]
                                public async void {|#0:Test|}()
                                {
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.AsyncVoidMethod)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}
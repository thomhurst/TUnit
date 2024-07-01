using System.Threading.Tasks;
using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DependsOnNotInParallelConflictAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DependsOnNotInParallelConflictAnalyzerTests
{
    [Test]
    public async Task Conflict_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;
                            
                            public class MyClass
                            {
                                [Test]
                                public void Test()
                                {
                                }
                                
                                [Test, NotInParallel, DependsOn(nameof(Test))]
                                public void {|#0:Test2|}()
                                {
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.DependsOnNotInParallelConflict)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}
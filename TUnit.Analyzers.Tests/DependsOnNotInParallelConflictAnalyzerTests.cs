using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DependsOnNotInParallelConflictAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DependsOnNotInParallelConflictAnalyzerTests
{
    [Test]
    public async Task Conflict_Raises_Error()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
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
                """,

                Verifier
                    .Diagnostic(Rules.DependsOnNotInParallelConflict)
                    .WithLocation(0)
            );
    }
}
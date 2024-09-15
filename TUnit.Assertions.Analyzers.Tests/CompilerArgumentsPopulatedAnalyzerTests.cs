using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.CompilerArgumentsPopulatedAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class CompilerArgumentsPopulatedAnalyzerTests
{
    [Test]
    public async Task Expression_Argument_Is_Flagged_When_Populated()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                public async Task MyTest()
                                {
                                    await Assert.That(1, {|#0:"expression"|}).IsEqualTo(1);
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic().WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Not_Flagged_When_Not_Populated()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                public async Task MyTest()
                                {
                                    await Assert.That(1).IsEqualTo(1);
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}
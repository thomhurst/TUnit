using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.ConstantInAssertThatAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class ConstantInAssertThatAnalyzerTests
{
    [Test]
    public async Task Assert_That_Is_Flagged_When_Using_Constant()
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
                                    await {|#0:Assert.That(1)|}.IsEqualTo(1);
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.ConstantValueInAssertThat).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task No_Error_When_Not_Constant()
    {
        const string text = """
                            using System;
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                public async Task MyTest()
                                {
                                    var one = 1;
                                    await Assert.That(one).IsEqualTo(1);
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}
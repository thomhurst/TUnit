using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.MixAndOrOperatorsAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class MixAndOrOperatorsAnalyzerTests
{
    [Test]
    public async Task Flag_When_Mixing_And_With_Or()
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
                                    {|#0:await Assert.That(1).IsEqualTo(1).And.IsNotEqualTo(2).Or.IsEqualTo(3)|};
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.MixAndOrConditionsAssertion).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task No_Error_When_Not_Mixing()
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
                                    await Assert.That(one).IsEqualTo(1).And.IsNotEqualTo(2);
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}
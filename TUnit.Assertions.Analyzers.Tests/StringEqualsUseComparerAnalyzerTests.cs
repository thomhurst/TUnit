using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.StringEqualsUseComparerAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class StringEqualsUseComparerAnalyzerTests
{
    [Test]
    public async Task EqualTo_String_Is_Flagged_When_Not_Passing_Comparer()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Core;
                            
                            public class MyClass
                            {

                                public async Task MyTest()
                                {
                                    var one = "1";
                                    Assert.That(one).Is.{|#0:EqualTo|}("1");
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic().WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
        
        NUnit.Framework.Assert.That("1", Is.EqualTo("1"));
    }
    
    [Test]
    public async Task EqualTo_String_Is_Not_Flagged_When_Passing_Comparer()
    {
        const string text = """
                            using System;
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                public async Task MyTest()
                                {
                                    var one = "1";
                                    Assert.That(one).Is.EqualTo("1", StringComparison.Ordinal);
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}
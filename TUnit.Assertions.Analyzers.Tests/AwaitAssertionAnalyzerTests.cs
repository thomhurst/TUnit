using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.AwaitAssertionAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class AwaitAssertionAnalyzerTests
{
    [Test]
    public async Task Assert_That_Is_Flagged_When_Not_Awaited()
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
                                    var one = 1;
                                    {|#0:AssertAsync.That(one)|}.IsEqualTo(1);
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.AwaitAssertion).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Assert_That_Is_Flagged_When_Generic_Type_Parameters_And_Not_Awaited()
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
                                    var one = 1;
                                    {|#0:AssertAsync.That<long>(one)|}.IsEqualTo(1);
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.AwaitAssertion).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Assert_Multiple_Is_Flagged_When_Not_Await_Using()
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
                                    var one = 1;
                                    {|#0:AssertAsync.Multiple()|};
                                }

                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.DisposableUsingMultiple).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Assert_Multiple_Is_Not_Flagged_When_Await_Using()
    {
        const string text = """
                            using System;
                            using System.Collections.Generic;
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                public async Task MyTest()
                                {
                                    var list = new List<int> { 1, 2, 3 };
                            
                                    await using (AssertAsync.Multiple())
                                    {
                                        await AssertAsync.That(list).IsEquivalentTo(new[] { 1, 2, 3, 4, 5 });
                                        await AssertAsync.That(list).HasCount().EqualTo(5);
                                    }
                                }
                                
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}
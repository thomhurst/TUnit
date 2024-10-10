using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.ObjectBaseEqualsMethodAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class ObjectBaseEqualsMethodAnalyzerTests
{
    
    [Test]
    public async Task No_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public async Task Test()
                                {
                                    await Assert.That(1).IsEqualTo(1);
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
    
    [Test]
    public async Task No_Error2()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public async Task Test()
                                {
                                    await Assert.That(1).IsPositive().And.IsEqualTo(1);
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
        
    [Test]
    public async Task No_Error3()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public async Task Test()
                                {
                                    await Assert.That(1).IsPositive().Or.IsEqualTo(1);
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Assert_With_ObjectEquals_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Core;
                            
                            public class MyClass
                            {
                                [Test]
                                [System.Obsolete("Obsolete")]
                                public void Test()
                                {
                                    {|#0:Assert.That(1).Equals(1)|};
                                }
                            }
                            """;
        
        var expected = Verifier
            .Diagnostic(Rules.ObjectEqualsBaseMethod)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task And_With_ObjectEquals_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void Test()
                                {
                                    {|#0:Assert.That(1).IsPositive().And.Equals(1)|};
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.ObjectEqualsBaseMethod)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Or_With_ObjectEquals_Raises_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Assertions;
                            using TUnit.Assertions.Extensions;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void Test()
                                {
                                    {|#0:Assert.That(1).IsPositive().Or.Equals(1)|};
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.ObjectEqualsBaseMethod)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}